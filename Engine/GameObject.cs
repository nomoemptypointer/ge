using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Engine
{
    public class GameObject
    {
        private static long s_latestAssignedID = 0;

        private readonly Dictionary<Type, List<Component>> _components = [];
        private SystemRegistry _registry;
        private bool _enabled = true;
        private bool _enabledInHierarchy = true;

        public string Name { get; set; }

        public ulong ID { get; }

        public Transform Transform { get; }

        public bool Enabled
        {
            get { return _enabled; }
            set { if (value != _enabled) { SetEnabled(value); } }
        }

        public bool EnabledInHierarchy => _enabledInHierarchy;

        internal static event Action<GameObject> InternalConstructed;
        internal static event Action<GameObject> InternalDestroyRequested;
        internal static event Action<GameObject> InternalDestroyCommitted;

        public event Action<GameObject> Destroyed;

        public GameObject() : this(Guid.NewGuid().ToString())
        { }

        public GameObject(string name)
        {
            Transform t = new Transform();
            t.ParentChanged += OnTransformParentChanged;
            AddComponent(t);
            Transform = t;
            Name = name;
            InternalConstructed?.Invoke(this);
            ID = GetNextID();
        }

        private ulong GetNextID()
        {
            ulong newID = unchecked((ulong)Interlocked.Increment(ref s_latestAssignedID));
            Debug.Assert(newID != 0); // Overflow
            return newID;
        }

        public void AddComponent(Component component)
        {
            AddComponentInternal(component.GetType(), component);
            component.AttachToGameObject(this, _registry);
        }

        public void AddComponent<T>(T component) where T : Component
        {
            AddComponentInternal(typeof(T), component);
            component.AttachToGameObject(this, _registry);
        }

        private void AddComponentInternal(Type type, Component component)
        {
            if (!_components.TryGetValue(type, out var list))
            {
                list = [];
                _components[type] = list;
            }

            list.Add(component);
        }

        private bool RemoveComponentInternal(Type type, Component component)
        {
            if (_components.TryGetValue(type, out var list))
            {
                bool removed = list.Remove(component);
                if (list.Count == 0)
                    _components.Remove(type);
                return removed;
            }
            return false;
        }

        public void RemoveAll<T>() where T : Component
        {
            if (_components.TryGetValue(typeof(T), out var components))
            {
                foreach (var c in components)
                    c.InternalRemoved(_registry);

                _components.Remove(typeof(T));
            }
        }

        public void RemoveComponent<T>(T component) where T : Component
        {
            component.InternalRemoved(_registry);
            RemoveComponentInternal(typeof(T), component);
        }

        public void RemoveComponent(Component component)
        {
            RemoveComponentInternal(component.GetType(), component);
            component.InternalRemoved(_registry);
        }

        public T GetComponent<T>() where T : Component
        {
            return (T)GetComponent(typeof(T));
        }

        public Component GetComponent(Type type)
        {
            if (_components.TryGetValue(type, out var components))
                return components.FirstOrDefault();

            foreach (var kvp in _components)
            {
                if (type.IsAssignableFrom(kvp.Key) && kvp.Value.Count > 0)
                    return kvp.Value[0];
            }

            return null;
        }

        public IEnumerable<T> GetComponentsByInterface<T>()
        {
            foreach (var kvp in _components)
            {
                foreach (var component in kvp.Value)
                {
                    if (component is T)
                    {
                        yield return (T)(object)component;
                    }
                }
            }
        }

        public T GetComponentByInterface<T>()
        {
            return GetComponentsByInterface<T>().FirstOrDefault();
        }

        internal void SetRegistry(SystemRegistry systemRegistry)
        {
            _registry = systemRegistry;
        }

        public IEnumerable<T> GetComponents<T>() where T : Component
        {
            if (_components.TryGetValue(typeof(T), out var components))
            {
                foreach (var comp in components)
                    yield return (T)comp;
            }

            foreach (var kvp in _components)
            {
                if (typeof(T).IsAssignableFrom(kvp.Key))
                {
                    foreach (var comp in kvp.Value)
                        yield return (T)comp;
                }
            }
        }

        public T GetComponentInParent<T>() where T : Component
        {
            T component;
            GameObject parent = this;
            while ((parent = parent.Transform.Parent?.GameObject) != null)
            {
                component = parent.GetComponent<T>();
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }

        public T GetComponentInParentOrSelf<T>() where T : Component
        {
            T component;
            component = GetComponentInParent<T>();
            if (component == null)
            {
                component = GetComponent<T>();
            }

            return component;
        }

        public T GetComponentInChildren<T>() where T: Component
        {
            return (T)GetComponentInChildren(typeof(T));
        }

        public Component GetComponentInChildren(Type componentType)
        {
            foreach (var child in Transform.Children)
            {
                Component ret = child.GameObject.GetComponent(componentType) ?? child.GameObject.GetComponentInChildren(componentType);
                if (ret != null)
                {
                    return ret;
                }
            }

            return null;
        }

        public void Destroy()
        {
            InternalDestroyRequested.Invoke(this);
        }

        internal void CommitDestroy()
        {
            foreach (var child in Transform.Children.ToArray())
            {
                child.GameObject.CommitDestroy();
            }

            foreach (var componentList in _components)
            {
                foreach (var component in componentList.Value)
                {
                    component.InternalRemoved(_registry);
                }
            }

            _components.Clear();

            Destroyed?.Invoke(this);
            InternalDestroyCommitted.Invoke(this);
        }

        private void SetEnabled(bool state)
        {
            _enabled = state;

            foreach (var child in Transform.Children)
            {
                child.GameObject.HierarchyEnabledStateChanged();
            }

            HierarchyEnabledStateChanged();
        }

        private void OnTransformParentChanged(Transform t, Transform oldParent, Transform newParent)
        {
            HierarchyEnabledStateChanged();
        }

        private void HierarchyEnabledStateChanged()
        {
            bool newState = _enabled && IsParentEnabled();
            if (_enabledInHierarchy != newState)
            {
                CoreHierarchyEnabledStateChanged(newState);
            }
        }

        private void CoreHierarchyEnabledStateChanged(bool newState)
        {
            Debug.Assert(newState != _enabledInHierarchy);
            _enabledInHierarchy = newState;
            foreach (var component in GetComponents<Component>())
            {
                component.HierarchyEnabledStateChanged();
            }
        }

        private bool IsParentEnabled()
        {
            return Transform.Parent == null || Transform.Parent.GameObject.Enabled;
        }

        public override string ToString()
        {
            return $"{Name}, {_components.Values.Sum(irc => irc.Count)} components";
        }
    }
}