using Engine.Core.Transform;

namespace Engine.Core.Entities;

public class Entity(Guid id = default)
{
    private readonly Dictionary<Type, Action> _pendingNotifications = new();
    public readonly Guid Id = id == Guid.Empty ? Guid.NewGuid() : id;
    public required string Name;
    public Entity? Parent;
    public List<Entity> Children = [];
    public bool IsEnabled = true;

    public IReadOnlyDictionary<Type, IComponent> Components => _components;
    private readonly Dictionary<Type, IComponent> _components = new();
    private readonly Dictionary<Type, Delegate> _componentChangeHandlers = new();
    
    public delegate void ChangeComponentEvent<T>(T newValue, T oldValue);

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
        var componentType = typeof(T);
        if (!_components.TryGetValue(componentType, out var old))
            throw new InvalidOperationException($"Component {componentType.Name} not found before applying");
        _components[componentType] = component;
        if (!_pendingNotifications.ContainsKey(componentType))
            _pendingNotifications.Add(componentType, () => OnComponentChanged(component, (T)old));
    }
    
    public void ApplyComponentSilently<T>(T component) where T : struct, IComponent
    {
        var componentType = typeof(T);
        if (!_components.TryGetValue(componentType, out var old))
            throw new InvalidOperationException($"Component {componentType.Name} not found before applying");
        _components[componentType] = component;
    }

    private void OnComponentChanged<T>(T newValue, T oldValue) where T : IComponent
    {
        _componentChangeHandlers.TryGetValue(typeof(T), out var handler);
        handler?.DynamicInvoke(newValue, oldValue);
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
        if (!_componentChangeHandlers.TryGetValue(type, out var existingDelegate)) return;
        
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

    public void FlushPendingNotifications()
    {
        var pendingNotifications = new Dictionary<Type, Action>(_pendingNotifications);
        _pendingNotifications.Clear();
        foreach (var (_, value) in pendingNotifications)
        {
            value();
        }
    }
}