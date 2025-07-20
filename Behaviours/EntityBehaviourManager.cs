using Engine.Core.Entities;
using GignerEngine.DiContainer;

namespace Engine.Core.Behaviours;

public class EntityBehaviourManager(DiContainer container)
{
    private readonly Dictionary<Entity, List<IEntityBehaviour>> _behaviours = new();
    
    private readonly Dictionary<Type, IEntityBehaviour> _behavioursCache = new();

    private IEntityBehaviour FindBehaviour(Type behaviourType)
    {
        if (!_behavioursCache.TryGetValue(behaviourType, out var behaviour))
        {
            behaviour = container.Resolve(behaviourType) as IEntityBehaviour;
            _behavioursCache.Add(behaviourType, behaviour);
        }
        return behaviour;
    }

    public void AttachBehaviour(Entity entity, IEntityBehaviour behaviour)
    {
        if (!_behaviours.TryGetValue(entity, out var list))
            list = _behaviours[entity] = new List<IEntityBehaviour>();

        list.Add(behaviour);
        behaviour.OnAttach(entity);
    }

    public void AttachBehaviour(Entity entity, Type behaviourType)
    {
        AttachBehaviour(entity, FindBehaviour(behaviourType));
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
            {
                behaviour.OnStart(entity);
            }
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

    public Dictionary<Entity, List<IEntityBehaviour>> GetBehavioursRecursive(Entity rootEntity)
    {
        var result = new Dictionary<Entity, List<IEntityBehaviour>>();

        void Traverse(Entity entity)
        {
            var behaviours = GetBehaviours(entity).ToList();
            if (behaviours.Count > 0)
                result[entity] = behaviours;

            foreach (var child in entity.Children)
                Traverse(child);
        }

        Traverse(rootEntity);
        return result;
    }
}