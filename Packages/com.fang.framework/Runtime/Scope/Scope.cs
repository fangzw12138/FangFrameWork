using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Fang.Framework
{
    public interface IInitializable
    {
        void Initialize();
    }

    public interface IInjectable
    {
        void Inject(Scope scope);
    }

    public sealed class Scope : IDisposable
    {
        private static readonly Dictionary<Type, ConstructorInfo> ConstructorCache = new Dictionary<Type, ConstructorInfo>();

        private readonly Dictionary<Type, Registration> _registrations = new Dictionary<Type, Registration>();
        private readonly List<Registration> _registrationOrder = new List<Registration>();
        private readonly Dictionary<Type, object> _instances = new Dictionary<Type, object>();
        private readonly List<object> _creationOrder = new List<object>();

        public Scope(string name = null)
        {
            Name = string.IsNullOrEmpty(name) ? "Scope" : name;
        }

        public Scope(Scope parent, string name = null) : this(name)
        {
            Parent = parent ?? throw new ArgumentNullException(nameof(parent));
        }

        public string Name { get; }

        public Scope Parent { get; }

        public bool IsBuilt { get; private set; }

        public bool IsDisposed { get; private set; }

        public Scope Register<TService, TImplementation>() where TImplementation : class, TService
        {
            ThrowIfNotBuildable();
            AddRegistration(new Registration(typeof(TService), typeof(TImplementation), null, null));
            return this;
        }

        public Scope Register<TService>(Func<Scope, TService> factory)
        {
            ThrowIfNotBuildable();
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            AddRegistration(new Registration(typeof(TService), null, scope => (object)factory(scope), null));
            return this;
        }

        public Scope RegisterInstance<TService>(TService instance)
        {
            ThrowIfNotBuildable();
            AddRegistration(new Registration(typeof(TService), null, null, instance));
            return this;
        }

        public void Build()
        {
            ThrowIfDisposed();
            if (IsBuilt)
            {
                throw new ScopeStateException($"Scope '{Name}' has already been built.");
            }

            IsBuilt = true;

            var stack = new List<Type>();
            for (int i = 0; i < _registrationOrder.Count; i++)
            {
                ResolveInternal(_registrationOrder[i].ServiceType, stack);
            }

            var created = _creationOrder.ToArray();
            for (int i = 0; i < created.Length; i++)
            {
                Inject(created[i]);
            }

            for (int i = 0; i < created.Length; i++)
            {
                if (created[i] is IInitializable initializable)
                {
                    initializable.Initialize();
                }
            }
        }

        public T Resolve<T>()
        {
            return (T)Resolve(typeof(T));
        }

        public object Resolve(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            ThrowIfNotBuilt();
            return ResolveInternal(type, new List<Type>());
        }

        public bool TryResolve<T>(out T instance)
        {
            if (TryResolve(typeof(T), out var resolved))
            {
                instance = (T)resolved;
                return true;
            }

            instance = default;
            return false;
        }

        public bool TryResolve(Type type, out object instance)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            ThrowIfNotBuilt();
            if (!IsRegistered(type))
            {
                instance = null;
                return false;
            }

            instance = ResolveInternal(type, new List<Type>());
            return true;
        }

        public void Inject(object target)
        {
            if (target is IInjectable injectable)
            {
                injectable.Inject(this);
            }
        }

        public void InjectHierarchy(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            var injectables = root.GetComponentsInChildren<IInjectable>(true);
            for (int i = 0; i < injectables.Length; i++)
            {
                injectables[i].Inject(this);
            }
        }

        public Scope CreateChild(string name = null)
        {
            ThrowIfDisposed();
            return new Scope(this, name);
        }

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            IsDisposed = true;

            for (int i = _creationOrder.Count - 1; i >= 0; i--)
            {
                if (_creationOrder[i] is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            _creationOrder.Clear();
            _instances.Clear();
        }

        private void AddRegistration(Registration registration)
        {
            _registrations[registration.ServiceType] = registration;
            _registrationOrder.Add(registration);
        }

        private void ThrowIfDisposed()
        {
            if (IsDisposed)
            {
                throw new ScopeStateException($"Scope '{Name}' has been disposed.");
            }
        }

        private void ThrowIfNotBuilt()
        {
            ThrowIfDisposed();
            if (!IsBuilt)
            {
                throw new ScopeStateException($"Scope '{Name}' has not been built.");
            }
        }

        private void ThrowIfNotBuildable()
        {
            ThrowIfDisposed();
            if (IsBuilt)
            {
                throw new ScopeStateException($"Scope '{Name}' is already built and no longer accepts registrations.");
            }
        }

        private bool IsRegistered(Type type)
        {
            if (_registrations.ContainsKey(type))
            {
                return true;
            }

            return Parent != null && Parent.IsRegistered(type);
        }

        private object ResolveInternal(Type serviceType, List<Type> stack)
        {
            ThrowIfDisposed();

            if (_instances.TryGetValue(serviceType, out var existing))
            {
                return existing;
            }

            if (!_registrations.TryGetValue(serviceType, out var registration))
            {
                if (Parent != null)
                {
                    return Parent.ResolveInternal(serviceType, stack);
                }

                throw new ScopeResolveException($"Type '{serviceType.FullName}' is not registered in scope '{Name}'.");
            }

            if (stack.Contains(serviceType))
            {
                throw new ScopeCircularDependencyException(BuildCircularMessage(serviceType, stack));
            }

            stack.Add(serviceType);
            try
            {
                var instance = CreateInstance(registration, stack);
                _instances[serviceType] = instance;
                _creationOrder.Add(instance);
                return instance;
            }
            finally
            {
                stack.RemoveAt(stack.Count - 1);
            }
        }

        private object CreateInstance(Registration registration, List<Type> stack)
        {
            if (registration.ImplementationType != null)
            {
                return Construct(registration.ImplementationType, stack);
            }

            if (registration.Factory != null)
            {
                return registration.Factory(this);
            }

            return registration.Instance;
        }

        private object Construct(Type implementationType, List<Type> stack)
        {
            var constructor = GetConstructor(implementationType);
            var parameters = constructor.GetParameters();
            if (parameters.Length == 0)
            {
                return constructor.Invoke(null);
            }

            var arguments = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                arguments[i] = ResolveInternal(parameters[i].ParameterType, stack);
            }

            return constructor.Invoke(arguments);
        }

        private static ConstructorInfo GetConstructor(Type implementationType)
        {
            if (ConstructorCache.TryGetValue(implementationType, out var cached))
            {
                return cached;
            }

            var constructor = implementationType
                .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .OrderByDescending(candidate => candidate.GetParameters().Length)
                .FirstOrDefault();

            if (constructor == null)
            {
                throw new ScopeResolveException($"Type '{implementationType.FullName}' has no public constructor.");
            }

            ConstructorCache[implementationType] = constructor;
            return constructor;
        }

        private string BuildCircularMessage(Type serviceType, List<Type> stack)
        {
            var chain = new List<string>();
            for (int i = 0; i < stack.Count; i++)
            {
                chain.Add(stack[i].FullName);
            }

            chain.Add(serviceType.FullName);
            return $"Circular dependency detected in scope '{Name}': {string.Join(" -> ", chain)}";
        }

        private sealed class Registration
        {
            public Registration(Type serviceType, Type implementationType, Func<Scope, object> factory, object instance)
            {
                ServiceType = serviceType;
                ImplementationType = implementationType;
                Factory = factory;
                Instance = instance;
            }

            public Type ServiceType { get; }

            public Type ImplementationType { get; }

            public Func<Scope, object> Factory { get; }

            public object Instance { get; }
        }
    }

    public class ScopeException : Exception
    {
        public ScopeException(string message) : base(message)
        {
        }

        public ScopeException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public class ScopeResolveException : ScopeException
    {
        public ScopeResolveException(string message) : base(message)
        {
        }
    }

    public class ScopeCircularDependencyException : ScopeException
    {
        public ScopeCircularDependencyException(string message) : base(message)
        {
        }
    }

    public class ScopeStateException : ScopeException
    {
        public ScopeStateException(string message) : base(message)
        {
        }
    }
}
