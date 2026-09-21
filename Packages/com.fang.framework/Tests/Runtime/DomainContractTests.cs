using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Fang.Framework.Tests
{
    internal sealed class TestConfigDataSo : ConfigDataSo
    {
    }

    internal sealed class TestData : Data<TestConfigDataSo>
    {
        public TestData(TestConfigDataSo config) : base(config)
        {
        }
    }

    internal sealed class TestController : Controller<TestConfigDataSo, TestData>
    {
        public Scope InjectedScope => Scope;
    }

    public class DomainContractTests
    {
        private const BindingFlags DeclaredAll =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        [TearDown]
        public void TearDown()
        {
            ProbeScope.DestroyAll();
        }

        [Test]
        public void Domain_types_service_and_scope_are_abstract()
        {
            Assert.IsTrue(typeof(ConfigDataSo).IsAbstract);
            Assert.IsTrue(typeof(Data<>).IsAbstract);
            Assert.IsTrue(typeof(Controller<,>).IsAbstract);
            Assert.IsTrue(typeof(Service).IsAbstract);
            Assert.IsTrue(typeof(Scope).IsAbstract);
        }

        [Test]
        public void Mono_types_and_pure_types_are_split_as_agreed()
        {
            foreach (var type in new[] { typeof(Scope), typeof(Service), typeof(Controller<,>) })
            {
                Assert.IsTrue(typeof(MonoBehaviour).IsAssignableFrom(type), $"{type.Name} must derive from MonoBehaviour.");
            }

            Assert.AreEqual(typeof(ScriptableObject), typeof(ConfigDataSo).BaseType);
            Assert.IsFalse(typeof(UnityEngine.Object).IsAssignableFrom(typeof(Data<>)));
            Assert.AreEqual(typeof(object), typeof(Data<>).BaseType);
        }

        [Test]
        public void Scope_implements_ILifecycle_but_not_IInjectable()
        {
            Assert.IsTrue(typeof(ILifecycle).IsAssignableFrom(typeof(Scope)));
            Assert.IsFalse(typeof(IInjectable).IsAssignableFrom(typeof(Scope)));
        }

        [Test]
        public void IInjectable_declares_Inject_against_Scope()
        {
            var method = typeof(IInjectable).GetMethod("Inject", BindingFlags.Public | BindingFlags.Instance);

            Assert.IsNotNull(method);
            Assert.AreEqual(typeof(void), method.ReturnType);
            Assert.AreEqual(new[] { typeof(Scope) }, method.GetParameters().Select(parameter => parameter.ParameterType).ToArray());
        }

        [Test]
        public void Lifecycle_interfaces_declare_the_expected_members()
        {
            AssertMethod(typeof(ILifecycle), "OnInit", typeof(void));
            AssertMethod(typeof(ILifecycle), "OnDispose", typeof(void));
            AssertMethod(typeof(ITickable), "OnTick", typeof(void), typeof(float));
            AssertMethod(typeof(IFixedTickable), "OnFixedTick", typeof(void), typeof(float));
        }

        [Test]
        public void Controller_and_Service_implement_IInjectable_and_ILifecycle()
        {
            foreach (var type in new[] { typeof(Controller<,>), typeof(Service) })
            {
                Assert.IsTrue(typeof(IInjectable).IsAssignableFrom(type), $"{type.Name} must implement IInjectable.");
                Assert.IsTrue(typeof(ILifecycle).IsAssignableFrom(type), $"{type.Name} must implement ILifecycle.");
            }
        }

        [Test]
        public void Controller_and_Service_expose_a_protected_Scope()
        {
            foreach (var type in new[] { typeof(Controller<,>), typeof(Service) })
            {
                var property = type.GetProperty("Scope", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

                Assert.IsNotNull(property, $"{type.Name}.Scope is missing.");
                Assert.AreEqual(typeof(Scope), property.PropertyType);
                Assert.IsTrue(property.GetMethod.IsFamily, $"{type.Name}.Scope must be protected.");
                Assert.IsNotNull(property.SetMethod);
                Assert.IsFalse(property.SetMethod.IsPublic, $"{type.Name}.Scope must not be publicly writable.");
            }
        }

        [Test]
        public void Package_no_longer_declares_IInitializable()
        {
            Assert.IsFalse(
                GetLoadableTypes(typeof(Scope).Assembly).Any(type => type.Name == "IInitializable"),
                "IInitializable must be gone.");
        }

        [Test]
        public void Scope_exposes_only_the_agreed_member_surface()
        {
            var expected = new[]
            {
                "Name", "Parent", "Services", "Children", "IsInitialized",
                "OnInit", "OnDispose", "Tick", "FixedTick",
                "CreateChildScope", "AddService", "GetService", "RemoveService"
            };

            foreach (var name in expected)
            {
                Assert.IsTrue(typeof(Scope).GetMember(name, DeclaredAll).Length > 0, $"Scope.{name} is missing.");
            }

            var forbidden = new[]
            {
                "Build", "Register", "RegisterInstance", "Resolve", "TryResolve", "TryGet",
                "Inject", "InjectHierarchy", "CreateChild", "IsBuilt", "Initialize",
                "Dispose", "IsDisposed"
            };

            var declared = typeof(Scope).GetMembers(DeclaredAll).Select(member => member.Name).ToList();

            foreach (var name in forbidden)
            {
                Assert.IsFalse(declared.Contains(name), $"Scope must not declare '{name}'.");
            }
        }

        [Test]
        public void Scope_registration_and_lifecycle_entry_points_have_the_agreed_accessibility()
        {
            AssertMethodAccessibility(typeof(Scope), "OnInit", true);
            AssertMethodAccessibility(typeof(Scope), "OnDispose", true);
            AssertMethodAccessibility(typeof(Scope), "Tick", true);
            AssertMethodAccessibility(typeof(Scope), "FixedTick", true);
            AssertMethodAccessibility(typeof(Scope), "CreateChildScope", false);
            AssertMethodAccessibility(typeof(Scope), "AddService", false);
            AssertMethodAccessibility(typeof(Scope), "RemoveService", false);
        }

        [Test]
        public void Scope_Service_and_Controller_declare_no_unity_message_methods()
        {
            var messages = new[] { "Awake", "Start", "Update", "FixedUpdate", "LateUpdate", "OnEnable", "OnDisable", "OnDestroy" };

            foreach (var type in new[] { typeof(Scope), typeof(Service), typeof(Controller<,>) })
            {
                var declared = type.GetMembers(DeclaredAll).Select(member => member.Name).ToList();

                foreach (var message in messages)
                {
                    Assert.IsFalse(declared.Contains(message), $"{type.Name} must not declare Unity message '{message}'.");
                }
            }
        }

        [Test]
        public void Data_constraint_is_ConfigDataSo()
        {
            var parameters = typeof(Data<>).GetGenericArguments();
            Assert.AreEqual(1, parameters.Length);
            Assert.AreEqual(typeof(ConfigDataSo), parameters[0].BaseType);
        }

        [Test]
        public void Controller_constraints_are_correct()
        {
            var type = typeof(Controller<,>);
            var parameters = type.GetGenericArguments();

            Assert.AreEqual(2, parameters.Length);
            Assert.AreEqual(typeof(ConfigDataSo), parameters[0].BaseType);

            var dataConstraint = parameters[1].BaseType;
            Assert.AreEqual(typeof(Data<>), dataConstraint.GetGenericTypeDefinition());
            Assert.AreEqual(parameters[0].Name, dataConstraint.GetGenericArguments()[0].Name);
        }

        [Test]
        public void Package_has_no_non_generic_Data_type_and_no_IData_interface()
        {
            foreach (var type in GetLoadableTypes(typeof(ConfigDataSo).Assembly))
            {
                Assert.AreNotEqual("Data", type.Name, $"Unexpected non-generic Data type: {type.FullName}");
                Assert.IsFalse(type.IsInterface && type.Name == "IData", $"Unexpected IData interface: {type.FullName}");
            }
        }

        [Test]
        public void Data_has_no_parameterless_constructor()
        {
            var constructors = typeof(Data<>).GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsFalse(constructors.Any(candidate => candidate.GetParameters().Length == 0));

            var derived = typeof(TestData).GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsFalse(derived.Any(candidate => candidate.GetParameters().Length == 0));
        }

        [Test]
        public void Data_properties_are_readonly()
        {
            foreach (var name in new[] { "RuntimeId", "Config" })
            {
                var property = typeof(Data<>).GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                Assert.IsNotNull(property, $"Data<TConfig>.{name} is missing.");
                Assert.IsTrue(property.CanRead);
                Assert.IsFalse(property.CanWrite, $"Data<TConfig>.{name} must be read-only.");
            }
        }

        [Test]
        public void Data_has_no_serialization_or_persistence_members()
        {
            var forbidden = new[] { "Load", "Save", "ToJson", "FromJson", "ResolveConfig", "ConfigKey" };
            var members = typeof(Data<>).GetMembers(DeclaredAll).Select(member => member.Name).ToList();

            foreach (var name in forbidden)
            {
                Assert.IsFalse(members.Contains(name), $"Data<TConfig> must not declare '{name}'.");
            }
        }

        [Test]
        public void Data_runtime_fields_are_not_serialized()
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

            var runtimeId = typeof(Data<>).GetField("_runtimeId", flags);
            var config = typeof(Data<>).GetField("_config", flags);

            Assert.IsNotNull(runtimeId);
            Assert.IsNotNull(config);
            Assert.IsTrue(runtimeId.IsNotSerialized);
            Assert.IsTrue(config.IsNotSerialized);
        }

        [Test]
        public void Data_assigns_unique_runtime_id_and_keeps_config()
        {
            var first = ScriptableObject.CreateInstance<TestConfigDataSo>();
            var second = ScriptableObject.CreateInstance<TestConfigDataSo>();

            try
            {
                var a = new TestData(first);
                var b = new TestData(second);

                Assert.IsFalse(string.IsNullOrEmpty(a.RuntimeId));
                Assert.IsFalse(string.IsNullOrEmpty(b.RuntimeId));
                Assert.AreNotEqual(a.RuntimeId, b.RuntimeId);
                Assert.AreSame(first, a.Config);
                Assert.AreSame(second, b.Config);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(first);
                UnityEngine.Object.DestroyImmediate(second);
            }
        }

        [Test]
        public void Controller_exposes_readonly_Data_property()
        {
            var property = typeof(Controller<,>).GetProperty("Data", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            Assert.IsNotNull(property);
            Assert.IsTrue(property.CanRead);
            Assert.IsNotNull(property.SetMethod);
            Assert.IsFalse(property.SetMethod.IsPublic);
        }

        [Test]
        public void Controller_Initialize_rejects_null()
        {
            var host = new GameObject("ControllerInitializeHost");
            try
            {
                var controller = host.AddComponent<TestController>();

                Assert.Throws<ArgumentNullException>(() => { controller.Initialize(null); });
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void Controller_Inject_stores_the_scope()
        {
            var scope = ProbeScope.Create<ProbeScope>();
            var host = new GameObject("ControllerInjectHost");

            try
            {
                var controller = host.AddComponent<TestController>();

                controller.Inject(scope);

                Assert.AreSame(scope, controller.InjectedScope);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void ConfigDataSo_declares_no_member_named_Name()
        {
            var members = typeof(ConfigDataSo).GetMembers(DeclaredAll);
            Assert.IsFalse(members.Any(member => member.Name == "Name"), "ConfigDataSo must not declare a member named 'Name'.");
        }

        [Test]
        public void ConfigDataSo_has_no_public_write_entry()
        {
            var type = typeof(ConfigDataSo);

            foreach (var property in type.GetProperties(DeclaredAll))
            {
                Assert.IsFalse(
                    property.SetMethod != null && property.SetMethod.IsPublic,
                    $"Property '{property.Name}' must not be publicly writable.");
            }

            foreach (var field in type.GetFields(DeclaredAll))
            {
                Assert.IsFalse(field.IsPublic, $"Field '{field.Name}' must not be public.");
            }

            foreach (var method in type.GetMethods(DeclaredAll))
            {
                Assert.IsFalse(method.Name.StartsWith("Set", StringComparison.Ordinal), $"Method '{method.Name}' must not be a public setter.");
            }
        }

        [Test]
        public void Package_exposes_no_serialization_or_save_member()
        {
            var forbidden = new[] { "Load", "Save", "GetSaveData" };

            foreach (var type in GetLoadableTypes(typeof(ConfigDataSo).Assembly))
            {
                foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    Assert.IsFalse(
                        member.Name.IndexOf("Serializ", StringComparison.Ordinal) >= 0,
                        $"'{type.Name}.{member.Name}' must not expose serialization.");
                    Assert.IsFalse(forbidden.Contains(member.Name), $"'{type.Name}.{member.Name}' must not expose persistence.");
                }
            }
        }

        private static void AssertMethod(Type type, string name, Type returnType, params Type[] parameters)
        {
            var method = type.GetMethod(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, parameters, null);

            Assert.IsNotNull(method, $"{type.Name}.{name} is missing.");
            Assert.AreEqual(returnType, method.ReturnType);
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
