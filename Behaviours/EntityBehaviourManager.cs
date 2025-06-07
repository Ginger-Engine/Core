using Engine.Core.Entities;

namespace Engine.Core.Behaviours;

public class EntityBehaviourManager()
{
    private readonly Dictionary<Entity, List<IEntityBehaviour>> _behaviours = new();

    public void AttachBehaviour(Entity entity, IEntityBehaviour behaviour)
    {
        if (!_behaviours.TryGetValue(entity, out var list))
            list = _behaviours[entity] = new List<IEntityBehaviour>();

        list.Add(behaviour);
        behaviour.OnAttach(entity);
    }

    public IReadOnlyList<IEntityBehaviour> GetBehaviours(Entity entity)
    {
        return _behaviours.TryGetValue(entity, out var list) ? list : Array.Empty<IEntityBehaviour>();
    }

    public void Start(Entity entity)
    {
        if (_behaviours.TryGetValue(entity, out var list))
        {
            foreach (var behaviour in list)
                behaviour.OnStart(entity);
        }
    }

    public void Update(Entity entity, float dt)
    {
        if (_behaviours.TryGetValue(entity, out var list))
        {
            foreach (var behaviour in list)
                behaviour.OnUpdate(entity, dt);
        }
    }
}