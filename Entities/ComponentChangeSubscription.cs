namespace Engine.Core.Entities;

public class ComponentChangeSubscription<T> : IDisposable where T: IComponent
{
    private readonly Entities.Entity _entity;
    private readonly Entities.Entity.ChangeComponentEvent<T> _action;

    public ComponentChangeSubscription(Entities.Entity entity, Entities.Entity.ChangeComponentEvent<T> action)
    {
        _entity = entity;
        _action = action;
        _entity.AddComponentChangeHandler(_action);
    }

    public void Dispose()
    {
        _entity.RemoveComponentChangeHandler(_action);
    }
}