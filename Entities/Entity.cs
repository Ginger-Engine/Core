using Engine.Core.Behaviours;
using Engine.Core.Scenes;

namespace Engine.Core.Entities;

public class Entity
{
    public readonly Scene Scene;
    public Entity? Parent;
    public List<Entity> Children = new();
    public bool IsEnabled = true;

    private readonly Dictionary<Type, object> _components = new();
    private readonly Dictionary<Type, Delegate> _componentChangeHandlers = new();
    
    public delegate void ChangeComponentEvent<T>(T newValue, T oldValue);

    public Entity(Scene scene)
    {
        Scene = scene;
    }

    public T GetComponent<T>() where T : IComponent
        => (T)_components[typeof(T)];
    
    public delegate void RefAction<T>(ref T value);
    public void Modify<T>(RefAction<T> mutator) where T : struct, IComponent
    {
        if (!TryGetComponent<T>(out var component))
            throw new Exception($"Component {typeof(T)} not found");

        mutator(ref component);
        ApplyComponent(component);
    }

    public bool IsComponentExists<T>() where T : IComponent => _components.ContainsKey(typeof(T));
    
    public bool TryGetComponent<T>(out T value) where T : IComponent
    {
        if (_components.TryGetValue(typeof(T), out var obj))
        {
            value = (T)obj;
            return true;
        }
        value = default!;
        return false;
    }

    public void AddComponent<T>(T component) where T : struct, IComponent
    {
        _components.Add(typeof(T), component);
        component.OnAddToEntity(this);
    }

    public void ApplyComponent<T>(T component) where T : struct, IComponent
    {
        if (_components.TryGetValue(typeof(T), out var old))
        {
            _components[typeof(T)] = component;
            OnComponentChanged(component, (T)old);
        }
    }

    private void OnComponentChanged<T>(T newValue, T oldValue) where T : IComponent
    {
        if (_componentChangeHandlers.TryGetValue(typeof(T), out var handler) && handler is Action<T, T> typed)
            typed.Invoke(newValue, oldValue);
    }

    public IDisposable SubscribeComponentChange<T>(ChangeComponentEvent<T> action) where T: IComponent
    {
        return new ComponentChangeSubscription<T>(this, action);
    }
    
    public void AddComponentChangeHandler<T>(ChangeComponentEvent<T> handler) where T: IComponent
    {
        var type = typeof(T);
        if (_componentChangeHandlers.TryGetValue(type, out var existingDelegate))
        {
            _componentChangeHandlers[type] = Delegate.Combine(existingDelegate, handler);
        }
        else
        {
            _componentChangeHandlers[type] = handler;
        }
    }
    
    public void RemoveComponentChangeHandler<T>(ChangeComponentEvent<T> handler) where T: IComponent
    {
        var type = typeof(T);
        if (_componentChangeHandlers.TryGetValue(type, out var existingDelegate))
        {
            var newDelegate = Delegate.Remove(existingDelegate, handler);
            if (newDelegate == null)
            {
                _componentChangeHandlers.Remove(type);
            }
            else
            {
                _componentChangeHandlers[type] = newDelegate;
            }
        }
    }
}