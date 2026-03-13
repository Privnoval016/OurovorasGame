using System;
using System.Collections.Generic;

namespace Extensions.EntityComponent
{
    public class Entity<T> where T : class, IComponent
    {
        private readonly Dictionary<Type, T> _components = new();
        
        public void AddComponent<TComponent>(TComponent component) where TComponent : class, T
        {
            if (component == null) throw new ArgumentNullException(nameof(component));
            
            Type mostSpecificType = component.GetType();
            _components[mostSpecificType] = component; // assigns to most specific type
        }
        
        public TComponent GetComponent<TComponent>() where TComponent : class, T
        {
            _components.TryGetValue(typeof(TComponent), out var comp);
            return comp as TComponent; // safe cast; returns null if missing
        }
        
        public bool TryGetComponent<TComponent>(out TComponent component) where TComponent : class, T
        {
            if (_components.TryGetValue(typeof(TComponent), out var comp) && comp is TComponent typed)
            {
                component = typed;
                return true;
            }

            component = null;
            return false;
        }
        
        public TComponent GetOrAddComponent<TComponent>() where TComponent : class, T, new()
        {
            if (_components.TryGetValue(typeof(TComponent), out var existing))
                return (TComponent)existing;

            var newComp = new TComponent();
            _components[typeof(TComponent)] = newComp;
            return newComp;
        }
        
        public bool HasComponent<TComponent>() where TComponent : class, T
            => _components.ContainsKey(typeof(TComponent));
        
        public void RemoveComponent<TComponent>() where TComponent : class, T
            => _components.Remove(typeof(TComponent));
        
        public void ClearComponents()
            => _components.Clear();
        
        public IEnumerable<T> GetAllComponents()
            => _components.Values;
        

        public override string ToString()
            => $"Entity<{typeof(T).Name}>[{string.Join(", ", _components.Keys)}]";
    }
}