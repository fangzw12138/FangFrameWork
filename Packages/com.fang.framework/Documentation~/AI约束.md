# AI 约束

本文件是 AI 参与本框架开发时的硬约束清单。与通用编码习惯冲突时，以本文件为准。

## 一、硬约束

1. **零第三方依赖。** 核心包 `package.json` 的 `dependencies` 为空对象。不引入 VContainer / UniTask / Addressables / Newtonsoft 等任何外部包。
2. **核心包不引用扩展包。** 依赖只有单向：扩展包 → 核心包。
3. **核心层不含序列化 / 反序列化 / 存档。** 不出现 `Serialize`、`Deserialize`、`GetSaveData`、`Load`、`Save` 命名的公开成员。这些能力全部归扩展包。
4. **`.cs` 文件里不写解释性注释。** 理由、决策依据写进 `Documentation~/` 与计划正文。代码要保持干净。唯一例外是一句话的职责 / 非标准写法备注（如「这里是工厂职责，示例不拆出独立服务」「示例直接 `Resources.Load` 拿配置，正式项目归扩展包解析层」）；理由型长注释仍然禁止。
5. **零推测性 API。** 只暴露当前确实存在调用方的成员。新增 API 时必须能指出当前调用点。不加「先留着以后可能有人用」的 setter / 重载 / 扩展点 / 接口 / 基类。
6. **配置只读。** `ConfigDataSo` 及所有配置资产对外零写入口。Editor 侧写配置走 `SerializedObject` / `SerializedProperty` + `EditorUtility.SetDirty`。
7. **`Data` 只有一个泛型基类。** 不存在非泛型 `Data`，不存在 `IData` 接口，不存在无参构造，不存在中间态 Data。
8. **依赖方向单向：`Controller → Data`。** `Controller` 类型上不得出现任何指向表现层（视觉脚本 `XxxVisual`、视图句柄 `XxxIWO`）的成员。表现只读 `Data` / `Config`，不反向持有 `Controller` 以外的领域对象。
9. **语言级别锁定 C# 9。** 禁用文件级 namespace、`global using`、`record struct`、`required` 成员、原始字符串字面量。
10. **不使用 Unity 6 独有 API。**
11. **零反射。** 不缓存构造函数、不按类型动态构造、不做反射注入。服务一律 `AddComponent<T>()`（`where T : Service`），依赖一律显式 `Scope.GetService<T>()`。
12. **MonoBehaviour 的归属是固定的。** `Scope` / `Service` / `Controller` 是 MonoBehaviour；`Data<TConfig>` 是纯 C# 类，`ConfigDataSo` 是 ScriptableObject。不得把 `Data` 改成 MonoBehaviour，也不得把 `Scope` / `Service` / `Controller` 改回纯 C#。核心包**不提供表现层基类**（`WorldObject` 已删除，见 `表现层规范.md`）。
13. **`Scope` / `Service` / `Controller` 不写 Unity 消息方法。** 不出现 `Awake` / `Start` / `Update` / `FixedUpdate` / `LateUpdate` / `OnEnable` / `OnDisable` / `OnDestroy`。生命周期只走 `ILifecycle` / `ITickable` / `IFixedTickable`，由使用方显式调 `Scope.OnInit` / `Tick` / `FixedTick` / `OnDispose`。框架不抢 Unity 回调，也不提供宿主 MonoBehaviour。**Unity 接触面（`[SerializeField]` / `Instantiate` / 协程 / 音效）落在视觉脚本 `XxxVisual` 与使用方自己写的宿主上**，见 `表现层规范.md`。
14. **框架不销毁 GameObject。** `RemoveService<T>()` 与 `OnDispose()` 只做注销与钩子回调；服务物体与子 Scope 物体留给使用方或 Unity 层级回收。不写 `DestroyObject` 这类辅助方法。
15. **`Scope` 有 `IsInitialized` 状态，但没有就绪门槛。** 不重新引入 `Build()` / `Register()` / `Resolve()` / 自定义异常类型。`IsInitialized` 只作状态查询，`AddService` / `RemoveService` / `GetService` 都不检查它。

以上第 3、7、12 条由 `Tests/Runtime/DomainContractTests.cs` 反射守住；第 13、15 条同样有反射契约（不声明 Unity 消息方法、成员面不含 `Dispose` / `IsDisposed`）；改动核心类型后必须重跑测试。第 8 条的表现侧无法用类型反射守住（视觉脚本是普通 `MonoBehaviour`，没有基类可断言），靠评审检查点。

## 二、Data 写入规则

### 2.1 唯一真相来源

**`Data<TConfig>` 是所有数据的来源与落脚点** —— 加载保存、状态运算、修改方法全部在 Data 上。

这是「唯一真相来源」的具体含义：它不只是**存**数据的地方，也是**改**数据的地方。

### 2.2 写入路径

```text
视觉脚本收到输入 / 回调
  → 调 Controller 的意图方法（不碰 Data）
  → Controller 走完整流程：校验 → 改 Data → 副作用 → 发 event
  → 视觉脚本订阅 event（或读 Data）刷新表现
```

### 2.3 权限表

| 角色 | 对 Data 的权限 |
| --- | --- |
| `Controller` | **读写**，修改方法的唯一调用者 |
| 视觉脚本 `XxxVisual` | **只读**，永不直接调 Data 的修改方法 |
| `Service` | 跨领域场景可写，同样走 Data 的修改方法 |

### 2.4 为什么不让视觉脚本直接写

1. **中间流程** —— 改一个数值往往牵动一串副作用。真实例子：扣血 → 血量归 0 → 死亡判定 → 掉落 / 播放死亡动画 / 发事件 / 存档脏标记。视觉脚本直接调 `data.ApplyDamage(10)`，数值变了但这串流程没人走，世界不会反应。
2. **不变量** —— Data 自己守约束（血量不为负、数量不超上限）。绕过 Data 的方法直接写字段，不变量就破了。
3. **单一写路径** —— 两个写入口时，出 bug 无法判断是谁改的；存档脏标记、撤销、回放也都依赖单一写入口。

视觉脚本只读不会被卡住：表现刷新由 Controller 的 `event` 驱动，不靠视觉脚本自己写状态。

### 2.5 框架能强制到什么程度

Data 的修改方法必然是 `public`，视觉脚本拿到 Data 实例就能调，**类型上无法禁止**。框架做到的是三件事：

1. 表现层没有基类可以借道（`WorldObject` 已删除），视觉脚本拿到的 `Data` 只能来自 `Controller.Data` 这个只读属性
2. 视觉脚本必须持有它所属的 `Controller` —— 要做事只能调 Controller 的意图方法（单向依赖正好支撑这条）
3. 本规则写进 `编码规范.md` 与 `AI约束.md`，并作为评审检查点

**代价（认下）**：视觉脚本直接调 `data.ApplyDamage(...)` 编译器拦不住，绕过流程的 bug 只能靠评审发现。若将来要真正强制，唯一手段是把修改方法移到 Controller，但那样 Data 退化成纯数据袋、不变量没人守 —— 不采用。

## 三、写代码前必须确认的事实

1. 目标类型是否已存在于 `Runtime/`；存在则改，不存在才加。
2. 新增成员是否有当前调用方；没有则不加。
3. 是否触碰核心层的序列化 / 存档边界；触碰则说明归属错了，应落在扩展包。
4. 是否引入第三方 `using`；引入则说明依赖错了。
5. 新加的类型是 MonoBehaviour 还是纯 C#；若与第 12 条的归属表冲突，说明设计错了。

## 四、改动后的验证

1. `unity_editor.refresh`，确认零编译错误、零警告。
2. 运行 EditMode 测试，确认全绿。
3. 文档中的 API 签名与代码逐条比对一致。

## 五、不要做的事

- 不要因为「更通用」而给基类加泛型参数、重载或扩展点。
- 不要把领域业务概念（Item / Buff / Character 等）写进核心包。
- 不要在核心包里加 Editor 工具；Editor 工具走 `Fang.Framework.Editor` 程序集。
- 不要修改 `Documentation~/` 之外的地方去记录设计理由。
- 不要在测试程序集里引用 `Assets/` 下的内容（沙盒不进包，引用会导致拆包后测试编译失败）。
- 不要在 `Scope` / `Service` / `Controller` 里写 Unity 消息方法，也不要让框架代劳销毁物体。
