using Engine.Core.Behaviours;
using Engine.Core.Entities;
using Engine.Core.Exceptions;
using Engine.Core.Scenes;
using GignerEngine.DiContainer;

namespace Engine.Core;

public class World(DiContainer container)
{
    internal struct PendingEntity
    {
        public Entity? Parent;
        public Entity Entity;
        public Dictionary<Type, HashSet<Entity>> EntityBehaviours;
    }

    private readonly List<PendingEntity> _pendingToAdd = [];
    private readonly List<(Entity, Type)> _pendingAttaches = [];

    public EntityCollection Entities;
    public Dictionary<Entity, HashSet<IEntityBehaviour>> EntityBehaviours;

    public void UseScene(Prefab prefab)
    {
        Entities = prefab.Entities;
        EntityBehaviours = new Dictionary<Entity, HashSet<IEntityBehaviour>>();
        foreach (var (behaviour, entities) in prefab.EntityBehaviours)
        {
            foreach (var entity in entities)
            {
                AttachBehaviour(behaviour, entity);                
            }
        }
    }

    private readonly Dictionary<Type, IEntityBehaviour> _behavioursCache = new();

    private IEntityBehaviour FindBehaviour(Type behaviourType)
    {
        if (_behavioursCache.TryGetValue(behaviourType, out var behaviour)) return behaviour;

        behaviour = container.Resolve(behaviourType) as IEntityBehaviour;
        if (behaviour == null) throw new GingerException($"Could not resolve behaviour type {behaviourType.FullName}");
        _behavioursCache.Add(behaviourType, behaviour);
        return behaviour;
    }

    public void AttachBehaviour<T>(Entity entity) where T : IEntityBehaviour
    {
        
        _pendingAttaches.Add((entity, typeof(T)));
    }

    public void AttachBehaviour(Type behaviour, Entity entity)
    {
        _pendingAttaches.Add((entity, behaviour));
    }

    private void AddBehaviourToEntity(IEntityBehaviour behaviour, Entity entity)
    {
        if (!EntityBehaviours.ContainsKey(entity))
        {
            EntityBehaviours.Add(entity, new HashSet<IEntityBehaviour>());
        }
        
        EntityBehaviours[entity].Add(behaviour);
        behaviour.OnAttach(entity);
    }

    public void ProcessPendingEntities()
    {
        foreach (var  pendingEntity in _pendingToAdd)
        {
            pendingEntity.Entity.Parent ??= pendingEntity.Parent;
            pendingEntity.Entity.Parent?.Children.Add(pendingEntity.Entity);
            Entities.Add(pendingEntity.Entity);

            foreach (var (behaviourType, entities) in pendingEntity.EntityBehaviours)
            {
                var behaviour = FindBehaviour(behaviourType);
                foreach (var entity in entities)
                {
                    AddBehaviourToEntity(behaviour, entity);
                    behaviour.OnAttach(entity);
                }
            }

            foreach (var (behaviourType, entities) in pendingEntity.EntityBehaviours)
            {
                var behaviour = FindBehaviour(behaviourType);
                foreach (var entity in entities)
                {
                    behaviour.OnStart(entity);
                }
            }
        }

        _pendingToAdd.Clear();
    }

    public void ProcessPendingAttaches()
    {
        foreach (var (entity, type) in _pendingAttaches)
        {
            var behaviour = FindBehaviour(type);
            AddBehaviourToEntity(behaviour, entity);
            behaviour.OnStart(entity);
        }
        _pendingAttaches.Clear();
    }

    internal void Update(float dt)
    {
        foreach (var (entity, entityBehaviours) in EntityBehaviours)
        {
            foreach (var entityBehaviour in entityBehaviours)
            {
                entityBehaviour.OnUpdate(entity, dt);
            }
        }
    }

    public void AddAsChild(Entity? parent, Entity child,
        Dictionary<Type, HashSet<Entity>> prefabEntityBehaviours)
    {
        _pendingToAdd.Add(new PendingEntity
        {
            Parent = parent,
            Entity = child,
            EntityBehaviours = prefabEntityBehaviours,
        });
    }

    public void AddAsChild(Entity parent, Prefab prefab)
    {
        _pendingToAdd.Add(new PendingEntity
        {
            Parent = parent,
            Entity = prefab.Entities.RootEntity,
            EntityBehaviours = prefab.EntityBehaviours,
        });
    }

    public void FlushPendingComponentChanges()
    {
        foreach (var (_, entity) in Entities.All)
        {
            entity.FlushPendingNotifications();
        }
    }

    public void Start()
    {
        foreach (var (entity, entityBehaviours) in EntityBehaviours)
        {
            foreach (var entityBehaviour in entityBehaviours)
            {
                entityBehaviour.OnStart(entity);
            }
        }
    }
}