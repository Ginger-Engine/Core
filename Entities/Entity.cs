using System.Diagnostics;

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
    private readonly Dictionary<Type, Delegate> _componentChangeHandlersImmediately = new();

    public struct Context
    {
        public Entity Entity;
        public StackFrame Frame;
    }
    public struct ChangeComponentEvent<T>
    {
        public T newValue;
        public T oldValue;
        public Context context;
    }
    public delegate void ChangeComponentEventDelegate<T>(ChangeComponentEvent<T> @event);

    public T GetComponent<T>() where T : IComponent
    {
        if (_components.TryGetValue(typeof(T), out IComponent component))
        {
            return (T)component;
        }

        throw new Exception($"Component {typeof(T)} of entity {Id} ('{Name}') not found");
    }

    public delegate void RefAction<T>(ref T value);

    public void Modify<T>(RefAction<T> mutator) where T : struct, IComponent
    {
        if (!TryGetComponent<T>(out var component))
            throw new Exception($"Component {typeof(T)} not found");

        mutator(ref component);
        ApplyComponent(component, new Context
        {
            Entity = this,
            Frame = new StackTrace(skipFrames: 1, fNeedFileInfo: true).GetFrame(0),
        });
    }

    public bool HasComponent<T>() where T : IComponent => _components.ContainsKey(typeof(T));

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
        ApplyComponent(component, new Context
        {
            Entity = this,
            Frame = new StackTrace(skipFrames: 1, fNeedFileInfo: true).GetFrame(0),
        });
    }

    private void ApplyComponent<T>(T component, Context context) where T : struct, IComponent
    {
        var componentType = typeof(T);
        if (!_components.TryGetValue(componentType, out var old))
            throw new InvalidOperationException($"Component {componentType.Name} not found before applying");
        _components[componentType] = component;
        
        OnComponentChangedImmediately(component, (T)old, context);
        
        if (!_pendingNotifications.ContainsKey(componentType))
        {
            var trace = new StackTrace(skipFrames: 1, fNeedFileInfo: true);
            // WriteStacktrace(trace, componentType);
            _pendingNotifications.Add(componentType, () => OnComponentChanged(component, (T)old, context));
        }
    }

    private void WriteStacktrace(StackTrace trace, Type componentType)
    {
        var frames = trace.GetFrames();
        var filteredFrames = frames.Take(2);
        var lines = new string[filteredFrames.Count() + 2];
        lines[0] = $"{componentType.Name} of {id.ToString().Substring(24)}('{Name}')\r\n";

        var i = 1;
        foreach (var frame in filteredFrames)
        {
            var method = frame.GetMethod();
            var file = frame.GetFileName();
            var line = frame.GetFileLineNumber();
            lines[i++] = $"  at {method?.Name} ({file}:{line})\r\n";
        }
        lines[i] = $"\n\n";
        Console.Write(string.Concat(lines));
    }
    
    public void ApplyComponentSilently<T>(T component) where T : struct, IComponent
    {
        var componentType = typeof(T);
        if (!_components.TryGetValue(componentType, out var old))
            throw new InvalidOperationException($"Component {componentType.Name} not found before applying");
        _components[componentType] = component;
    }

    public void ModifySilently<T>(RefAction<T> mutator) where T : struct, IComponent
    {
        if (!TryGetComponent<T>(out var component))
            throw new Exception($"Component {typeof(T)} not found");

        mutator(ref component);
        ApplyComponentSilently(component);
    }

    private void OnComponentChanged<T>(T newValue, T oldValue, Context context) where T : IComponent
    {
        _componentChangeHandlers.TryGetValue(typeof(T), out var handler);
        handler?.DynamicInvoke(new ChangeComponentEvent<T>
        {
            newValue = newValue,
            oldValue = oldValue, 
            context = context
        });
    }

    private void OnComponentChangedImmediately<T>(T newValue, T oldValue, Context context) where T : IComponent
    {
        _componentChangeHandlersImmediately.TryGetValue(typeof(T), out var handler);
        handler?.DynamicInvoke(new ChangeComponentEvent<T>
        {
            newValue = newValue,
            oldValue = oldValue, 
            context = context
        });
    }

    public IDisposable SubscribeComponentChange<T>(ChangeComponentEventDelegate<T> action) where T : IComponent
    {
        return new ComponentChangeSubscription<T>(this, action);
    }

    public void AddComponentChangeHandler<T>(ChangeComponentEventDelegate<T> handler) where T : IComponent
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

    public void AddComponentChangeHandlerImmediately<T>(ChangeComponentEventDelegate<T> handler) where T : IComponent
    {
        var type = typeof(T);
        if (_componentChangeHandlersImmediately.TryGetValue(type, out var existingDelegate))
        {
            _componentChangeHandlersImmediately[type] = Delegate.Combine(existingDelegate, handler);
        }
        else
        {
            _componentChangeHandlersImmediately[type] = handler;
        }
    }

    public void RemoveComponentChangeHandler<T>(ChangeComponentEventDelegate<T> handler) where T : IComponent
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

    

    public IDisposable SubscribeComponentChangeImmediately<T>(ChangeComponentEventDelegate<T> action) where T : IComponent
    {
        return new ComponentChangeSubscription<T>(this, action);
    }
}