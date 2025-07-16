namespace Engine.Core.Entities;

public class ComponentChangeSubscription<T> : IDisposable where T: IComponent
{
    private readonly Entity _entity;
    private readonly Entity.ChangeComponentEventDelegate<T> _action;

    public ComponentChangeSubscription(Entity entity, Entity.ChangeComponentEventDelegate<T> action, bool immediately = false)
    {
        _entity = entity;
        _action = action;
        if (immediately)
        {
            _entity.AddComponentChangeHandlerImmediately(_action);
        }
        else
        {
            _entity.AddComponentChangeHandler(_action);
        }
    }

    public void Dispose()
    {
        _entity.RemoveComponentChangeHandler(_action);
    }
}