using Engine.Core.Behaviours;
using Engine.Core.Entities;

namespace Engine.Core.Prefabs;

public class Prefab
{
    public Entity RootEntity { get; }
    public List<IEntityBehaviour> Behaviours { get; }

    public Prefab(Entity rootEntity, List<IEntityBehaviour> behaviours)
    {
        RootEntity = rootEntity;
        Behaviours = behaviours;
    }
}
