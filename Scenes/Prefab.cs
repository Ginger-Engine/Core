using Engine.Core.Behaviours;
using Engine.Core.Entities;

namespace Engine.Core.Scenes;

public class Prefab
{
    public readonly EntityCollection Entities;
    public readonly Dictionary<Type, HashSet<Entity>> EntityBehaviours;

    public Prefab(EntityCollection entityCollection, Dictionary<Type, HashSet<Entity>> entityBehaviours)
    {
        Entities = entityCollection;
        EntityBehaviours = entityBehaviours;
    }
}
