using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Fang.Framework.Async.Tests
{
    internal sealed class ProbeConfigDataSo : ConfigDataSo
    {
    }

    internal sealed class ProbeData : Data<ProbeConfigDataSo>
    {
        public ProbeData(ProbeConfigDataSo config) : base(config)
        {
        }
    }

    internal sealed class ProbeAsyncService : AsyncService
    {
        public int InitCount;
        public int DisposeCount;

        public AsyncScope InjectedScope => Scope;

        protected override UniTask OnInitAsync()
        {
            InitCount++;
            return UniTask.CompletedTask;
        }

        protected override UniTask OnDisposeAsync()
        {
            DisposeCount++;
            return UniTask.CompletedTask;
        }
    }

    internal sealed class ProbeAsyncController : AsyncController<ProbeConfigDataSo, ProbeData>
    {
        public int InitCount;
        public int DisposeCount;

        public AsyncScope InjectedScope => Scope;

        protected override UniTask OnInitAsync()
        {
            InitCount++;
            return UniTask.CompletedTask;
        }

        protected override UniTask OnDisposeAsync()
        {
            DisposeCount++;
            return UniTask.CompletedTask;
        }
    }

    public class AsyncContractTests
    {
        private const BindingFlags DeclaredAll =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        private const BindingFlags DeclaredInstance =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        [Test]
        public void Interfaces_are_interfaces_and_types_are_abstract_mono_behaviours()
        {
            Assert.IsTrue(typeof(IAsyncLifecycle).IsInterface);
            Assert.IsTrue(typeof(IAsyncInjectable).IsInterface);

            foreach (var type in new[]
                     {
                         typeof(AsyncLifecycleBehaviour), typeof(AsyncScope), typeof(AsyncService), typeof(AsyncController<,>)
                     })
            {
                Assert.IsTrue(type.IsAbstract, $"{type.Name} must be abstract.");
                Assert.IsTrue(typeof(MonoBehaviour).IsAssignableFrom(type), $"{type.Name} must derive from MonoBehaviour.");
            }
        }

        [Test]
        public void IAsyncLifecycle_declares_the_state_and_the_two_entries()
        {
            AssertProperty(typeof(IAsyncLifecycle), "IsInitialized", typeof(bool));
            AssertProperty(typeof(IAsyncLifecycle), "IsInitializing", typeof(bool));
            AssertProperty(typeof(IAsyncLifecycle), "InitProgress", typeof(float));

            AssertMethod(typeof(IAsyncLifecycle), "InitAsync", typeof(UniTask));
            AssertMethod(typeof(IAsyncLifecycle), "DisposeAsync", typeof(UniTask));
        }

        [Test]
        public void IAsyncInjectable_declares_Inject_against_AsyncScope()
        {
            var method = typeof(IAsyncInjectable).GetMethod("Inject", BindingFlags.Public | BindingFlags.Instance);

            Assert.IsNotNull(method);
            Assert.AreEqual(typeof(void), method.ReturnType);
            Assert.AreEqual(new[] { typeof(AsyncScope) }, method.GetParameters().Select(parameter => parameter.ParameterType).ToArray());
        }

        [Test]
        public void Every_type_derives_from_the_shared_lifecycle_base()
        {
            foreach (var type in new[] { typeof(AsyncScope), typeof(AsyncService), typeof(AsyncController<,>) })
            {
                Assert.AreEqual(typeof(AsyncLifecycleBehaviour), type.BaseType, $"{type.Name} must derive from AsyncLifecycleBehaviour.");
                Assert.IsTrue(typeof(IAsyncLifecycle).IsAssignableFrom(type), $"{type.Name} must implement IAsyncLifecycle.");
            }
        }

        [Test]
        public void Service_and_Controller_implement_IAsyncInjectable()
        {
            foreach (var type in new[] { typeof(AsyncService), typeof(AsyncController<,>) })
            {
                Assert.IsTrue(typeof(IAsyncInjectable).IsAssignableFrom(type), $"{type.Name} must implement IAsyncInjectable.");
            }
        }

        [Test]
        public void AsyncScope_is_not_async_injectable()
        {
            Assert.IsFalse(typeof(IAsyncInjectable).IsAssignableFrom(typeof(AsyncScope)));
        }

        [Test]
        public void The_base_declares_the_protocol_the_hooks_and_the_progress_writer()
        {
            foreach (var name in new[] { "InitAsync", "DisposeAsync" })
            {
                var protocol = typeof(AsyncLifecycleBehaviour).GetMethod(name, DeclaredInstance);

                Assert.IsNotNull(protocol, $"AsyncLifecycleBehaviour.{name} is missing.");
                Assert.IsTrue(protocol.IsPublic, $"AsyncLifecycleBehaviour.{name} must be public.");
                Assert.AreEqual(typeof(UniTask), protocol.ReturnType);
                Assert.AreEqual(0, protocol.GetParameters().Length);
            }

            Assert.IsTrue(typeof(AsyncLifecycleBehaviour).GetMethod("InitAsync", DeclaredInstance).IsVirtual);

            foreach (var name in new[] { "OnInitAsync", "OnDisposeAsync" })
            {
                var hook = typeof(AsyncLifecycleBehaviour).GetMethod(name, DeclaredInstance);

                Assert.IsNotNull(hook, $"AsyncLifecycleBehaviour.{name} is missing.");
                Assert.IsTrue(hook.IsFamily, $"AsyncLifecycleBehaviour.{name} must be protected.");
                Assert.IsTrue(hook.IsVirtual, $"AsyncLifecycleBehaviour.{name} must be virtual.");
                Assert.AreEqual(typeof(UniTask), hook.ReturnType);
            }

            var progress = typeof(AsyncLifecycleBehaviour).GetMethod("ReportInitProgress", DeclaredInstance);

            Assert.IsNotNull(progress);
            Assert.IsTrue(progress.IsPublic);
            Assert.AreEqual(typeof(void), progress.ReturnType);
            Assert.AreEqual(new[] { typeof(float) }, progress.GetParameters().Select(parameter => parameter.ParameterType).ToArray());

            foreach (var name in new[] { "IsInitialized", "IsInitializing", "InitProgress" })
            {
                Assert.IsNotNull(
                    typeof(AsyncLifecycleBehaviour).GetProperty(name, DeclaredInstance),
                    $"AsyncLifecycleBehaviour.{name} is missing.");
            }
        }

        [Test]
        public void The_state_machine_lives_in_exactly_one_place()
        {
            foreach (var type in new[] { typeof(AsyncScope), typeof(AsyncService) })
            {
                foreach (var name in new[] { "InitAsync", "DisposeAsync", "OnInitAsync", "OnDisposeAsync", "ReportInitProgress" })
                {
                    Assert.IsNull(
                        type.GetMethod(name, DeclaredInstance),
                        $"{type.Name} must not redeclare '{name}'; it belongs to AsyncLifecycleBehaviour.");
                }

                foreach (var name in new[] { "IsInitialized", "IsInitializing", "InitProgress" })
                {
                    Assert.IsNull(
                        type.GetProperty(name, DeclaredInstance),
                        $"{type.Name} must not redeclare '{name}'; it belongs to AsyncLifecycleBehaviour.");
                }
            }
        }

        [Test]
        public void Controller_takes_data_through_its_own_entry_and_guards_the_parameterless_one()
        {
            var entries = typeof(AsyncController<,>).GetMethods(DeclaredInstance)
                .Where(method => method.Name == "InitAsync")
                .ToList();

            Assert.AreEqual(2, entries.Count, "AsyncController must declare the data entry and the parameterless guard.");

            var withData = entries.Single(method => method.GetParameters().Length == 1);

            Assert.IsTrue(withData.IsPublic);
            Assert.AreEqual(typeof(UniTask), withData.ReturnType);
            Assert.IsTrue(withData.GetParameters()[0].ParameterType.IsGenericParameter);
            Assert.AreEqual("TData", withData.GetParameters()[0].ParameterType.Name);

            var guard = entries.Single(method => method.GetParameters().Length == 0);

            Assert.IsTrue(guard.IsPublic);
            Assert.IsTrue(guard.IsVirtual);
            Assert.IsTrue(guard.GetBaseDefinition().DeclaringType == typeof(AsyncLifecycleBehaviour));
        }

        [Test]
        public void Controller_exposes_a_readonly_Data_property()
        {
            var property = typeof(AsyncController<,>).GetProperty("Data", DeclaredInstance);

            Assert.IsNotNull(property);
            Assert.IsTrue(property.CanRead);
            Assert.IsTrue(property.SetMethod == null || !property.SetMethod.IsPublic);
        }

        [Test]
        public void Service_and_Controller_expose_a_protected_AsyncScope()
        {
            foreach (var type in new[] { typeof(AsyncService), typeof(AsyncController<,>) })
            {
                var property = type.GetProperty("Scope", DeclaredInstance);

                Assert.IsNotNull(property, $"{type.Name}.Scope is missing.");
                Assert.AreEqual(typeof(AsyncScope), property.PropertyType);
                Assert.IsTrue(property.GetMethod.IsFamily, $"{type.Name}.Scope must be protected.");
                Assert.IsNotNull(property.SetMethod);
                Assert.IsFalse(property.SetMethod.IsPublic, $"{type.Name}.Scope must not be publicly writable.");
            }
        }

        [Test]
        public void Scope_exposes_the_agreed_registration_surface()
        {
            var expected = new[]
            {
                "Name", "Parent", "Services", "Children",
                "Tick", "FixedTick", "GetService",
                "CreateChildScope", "RemoveChild", "AddService", "RemoveService"
            };

            foreach (var name in expected)
            {
                Assert.IsTrue(typeof(AsyncScope).GetMember(name, DeclaredAll).Length > 0, $"AsyncScope.{name} is missing.");
            }

            Assert.AreEqual(typeof(IReadOnlyList<IAsyncLifecycle>), typeof(AsyncScope).GetProperty("Services").PropertyType);
            Assert.AreEqual(typeof(IReadOnlyList<AsyncScope>), typeof(AsyncScope).GetProperty("Children").PropertyType);
        }

        [Test]
        public void Scope_registration_entry_points_are_protected_or_public_as_agreed()
        {
            AssertMethodAccessibility(typeof(AsyncScope), "Tick", true);
            AssertMethodAccessibility(typeof(AsyncScope), "FixedTick", true);
            AssertMethodAccessibility(typeof(AsyncScope), "GetService", true);
            AssertMethodAccessibility(typeof(AsyncScope), "CreateChildScope", false);
            AssertMethodAccessibility(typeof(AsyncScope), "RemoveChild", false);
            AssertMethodAccessibility(typeof(AsyncScope), "AddService", false);
            AssertMethodAccessibility(typeof(AsyncScope), "RemoveService", false);
        }

        [Test]
        public void Controller_InitAsync_rejects_null_data()
        {
            var host = new GameObject("AsyncControllerNullDataProbe");

            try
            {
                var controller = host.AddComponent<ProbeAsyncController>();

                Assert.Throws<ArgumentNullException>(() => { controller.InitAsync(null); });
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void Controller_needs_data_before_it_can_be_initialized()
        {
            var host = new GameObject("AsyncControllerNoDataProbe");

            try
            {
                var controller = host.AddComponent<ProbeAsyncController>();

                Assert.Throws<InvalidOperationException>(() => { controller.InitAsync(); });
                Assert.Throws<InvalidOperationException>(() => { ((IAsyncLifecycle)controller).InitAsync(); });
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void Controller_binds_data_only_once()
        {
            var host = new GameObject("AsyncControllerRebindProbe");
            var config = ScriptableObject.CreateInstance<ProbeConfigDataSo>();

            try
            {
                var controller = host.AddComponent<ProbeAsyncController>();

                controller.InitAsync(new ProbeData(config)).GetAwaiter().GetResult();

                Assert.Throws<InvalidOperationException>(() => { controller.InitAsync(new ProbeData(config)); });
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void Inject_stores_the_scope()
        {
            var scopeHost = new GameObject("AsyncScopeProbe");
            var serviceHost = new GameObject("AsyncServiceProbe");

            try
            {
                var scope = scopeHost.AddComponent<ProbeAsyncScope>();
                var service = serviceHost.AddComponent<ProbeAsyncService>();

                service.Inject(scope);

                Assert.AreSame(scope, service.InjectedScope);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(serviceHost);
                UnityEngine.Object.DestroyImmediate(scopeHost);
            }
        }

        [UnityTest]
        public IEnumerator Service_InitAsync_and_DisposeAsync_run_the_hooks_once()
        {
            var host = new GameObject("AsyncServiceIdempotenceProbe");

            try
            {
                var service = host.AddComponent<ProbeAsyncService>();

                yield return service.InitAsync().ToCoroutine(null);
                yield return service.InitAsync().ToCoroutine(null);

                Assert.AreEqual(1, service.InitCount);

                yield return service.DisposeAsync().ToCoroutine(null);
                yield return service.DisposeAsync().ToCoroutine(null);

                Assert.AreEqual(1, service.DisposeCount);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [UnityTest]
        public IEnumerator Controller_InitAsync_binds_data_and_runs_the_hook_once()
        {
            var host = new GameObject("AsyncControllerInitProbe");
            var config = ScriptableObject.CreateInstance<ProbeConfigDataSo>();

            try
            {
                var controller = host.AddComponent<ProbeAsyncController>();
                var data = new ProbeData(config);

                yield return controller.InitAsync(data).ToCoroutine(null);

                Assert.AreSame(data, controller.Data);
                Assert.AreEqual(1, controller.InitCount);
                Assert.IsTrue(controller.IsInitialized);

                yield return controller.InitAsync().ToCoroutine(null);

                Assert.AreEqual(1, controller.InitCount);

                yield return controller.DisposeAsync().ToCoroutine(null);
                yield return controller.DisposeAsync().ToCoroutine(null);

                Assert.AreEqual(1, controller.DisposeCount);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void Bases_declare_no_unity_message_methods()
        {
            var messages = new[] { "Awake", "Start", "Update", "FixedUpdate", "LateUpdate", "OnEnable", "OnDisable", "OnDestroy" };

            foreach (var type in new[]
                     {
                         typeof(AsyncLifecycleBehaviour), typeof(AsyncScope), typeof(AsyncService), typeof(AsyncController<,>)
                     })
            {
                var declared = type.GetMembers(DeclaredAll).Select(member => member.Name).ToList();

                foreach (var message in messages)
                {
                    Assert.IsFalse(declared.Contains(message), $"{type.Name} must not declare Unity message '{message}'.");
                }
            }
        }

        [Test]
        public void Async_types_declare_no_sync_lifecycle_members()
        {
            foreach (var type in new[]
                     {
                         typeof(AsyncLifecycleBehaviour), typeof(AsyncScope), typeof(AsyncService), typeof(AsyncController<,>)
                     })
            {
                var declared = type.GetMembers(DeclaredAll).Select(member => member.Name).ToList();

                Assert.IsFalse(declared.Contains("OnInit"), $"{type.Name} must not declare 'OnInit'.");
                Assert.IsFalse(declared.Contains("OnDispose"), $"{type.Name} must not declare 'OnDispose'.");
                Assert.IsFalse(declared.Contains("Initialize"), $"{type.Name} must not declare 'Initialize'.");
            }

            Assert.IsFalse(typeof(ILifecycle).IsAssignableFrom(typeof(AsyncLifecycleBehaviour)));
            Assert.IsFalse(typeof(ILifecycle).IsAssignableFrom(typeof(AsyncScope)));
            Assert.IsFalse(typeof(ILifecycle).IsAssignableFrom(typeof(AsyncService)));
            Assert.IsFalse(typeof(IInjectable).IsAssignableFrom(typeof(AsyncService)));
        }

        [Test]
        public void Package_declares_no_second_Data_type()
        {
            foreach (var type in GetLoadableTypes(typeof(AsyncService).Assembly))
            {
                Assert.AreNotEqual("Data", type.Name, $"Unexpected non-generic Data type: {type.FullName}");
                Assert.IsFalse(type.IsInterface && type.Name == "IData", $"Unexpected IData interface: {type.FullName}");
            }
        }

        private static void AssertMethod(Type type, string name, Type returnType, params Type[] parameters)
        {
            var method = type.GetMethod(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, parameters, null);

            Assert.IsNotNull(method, $"{type.Name}.{name} is missing.");
            Assert.AreEqual(returnType, method.ReturnType);
        }

        private static void AssertProperty(Type type, string name, Type propertyType)
        {
            var property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            Assert.IsNotNull(property, $"{type.Name}.{name} is missing.");
            Assert.AreEqual(propertyType, property.PropertyType);
            Assert.IsTrue(property.CanRead);
            Assert.IsFalse(property.CanWrite, $"{type.Name}.{name} must be read-only.");
        }

        private static void AssertMethodAccessibility(Type type, string name, bool isPublic)
        {
            var method = type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            Assert.IsNotNull(method, $"{type.Name}.{name} is missing.");
            Assert.AreEqual(isPublic, method.IsPublic, $"{type.Name}.{name} accessibility is wrong.");
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                return exception.Types.Where(type => type != null);
            }
        }
    }
}
