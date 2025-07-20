using Engine.Core.Behaviours;
using Engine.Core.Entities;

namespace Engine.Core.Prefabs;

public class Prefab
{
    public Entity RootEntity { get; }
    public Dictionary<Entity, List<IEntityBehaviour>> BehavioursByEntity { get; }

    public Prefab(Entity rootEntity, Dictionary<Entity, List<IEntityBehaviour>> behavioursByEntity)
    {
        RootEntity = rootEntity;
        BehavioursByEntity = behavioursByEntity;
    }
}