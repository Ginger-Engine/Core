using Engine.Core.Behaviours;
using Engine.Core.Entities;
using GignerEngine.DiContainer;

namespace Engine.Core.Scenes;

public class Scene
{
    public List<Entity> RootEntities = new();
    private readonly Dictionary<Type, ISceneBehaviour> _sceneBehaviours = new();
    private readonly Dictionary<Type, IEntityBehaviour> _entityBehaviours = new();
    private readonly EntityBehaviourManager _behaviourManager;
    private readonly DiContainer _di;

    public Scene(DiContainer di)
    {
        _di = di;
        _behaviourManager = _di.Resolve<EntityBehaviourManager>();
    }
    
    public void RegisterBehaviour(Type behaviourType)
    {
        var behaviour = _di.Resolve(behaviourType);

        if (behaviour is ISceneBehaviour sb && !_sceneBehaviours.ContainsKey(behaviourType))
        {
            _sceneBehaviours.Add(behaviourType, sb);
            sb.OnStart();
        }
        else if (behaviour is IEntityBehaviour eb && !_entityBehaviours.ContainsKey(behaviourType))
        {
            _entityBehaviours.Add(behaviourType, eb);
        }
    }

    public void Update(float dt)
    {
        foreach (var sb in _sceneBehaviours.Values)
            sb.OnUpdate(dt);
        UpdateEntities(RootEntities, dt);
    }

    private void UpdateEntities(IEnumerable<Entity> entities, float dt)
    {
        foreach (var entity in entities)
            if (entity.IsEnabled)
                UpdateEntity(entity, dt);
    }
    
    private void UpdateEntity(Entity entity, float dt)
    {
        _behaviourManager.Update(entity, dt);

        if (entity.Children.Count > 0)
        {
            UpdateEntities(entity.Children, dt);
        }
    }
}
