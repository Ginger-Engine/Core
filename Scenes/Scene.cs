using Engine.Core.Behaviours;
using Engine.Core.Entities;
using GignerEngine.DiContainer;

namespace Engine.Core.Scenes;

public class Scene
{
    public EntityCollection Entities = new();
    public IReadOnlyDictionary<Type, ISceneBehaviour> SceneBehaviours => _sceneBehaviours;
    private readonly Dictionary<Type, ISceneBehaviour> _sceneBehaviours = new();
    private readonly EntityBehaviourManager _behaviourManager;
    private readonly DiContainer _di;

    public Scene(DiContainer di)
    {
        _di = di;
        _behaviourManager = _di.Resolve<EntityBehaviourManager>();
    }
    
    public void AttachBehaviour(Type behaviourType)
    {
        var behaviour = _di.Resolve(behaviourType);

        if (behaviour is ISceneBehaviour sb)
        {
            _sceneBehaviours.Add(behaviourType, sb);
            return;
        }
        
        throw new Exception($"Invalid behaviour type: {behaviourType}");
    }

    public void Start()
    {
        foreach (var (_, behaviour) in _sceneBehaviours)
        {
            behaviour.OnStart();
        }

        foreach (var (key, entity) in Entities.All)
        {
            _behaviourManager.Start(entity);
        }
    }

    public void Update(float dt)
    {
        foreach (var sb in _sceneBehaviours.Values)
            sb.OnUpdate(dt);
        UpdateEntities(Entities.RootEntities, dt);
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
