using Engine.Core.Behaviours;
using Engine.Core.Entities;

namespace Engine.Core.Scenes;

public class Scene
{
    public EntityCollection Entities = new();
    public IReadOnlyDictionary<Type, ISceneBehaviour> SceneBehaviours => _sceneBehaviours;
    public Guid Id { get; set; }
    public string Name { get; set; }

    private readonly Dictionary<Type, ISceneBehaviour> _sceneBehaviours = new();
    private readonly EntityBehaviourManager _behaviourManager;

    public Scene(EntityBehaviourManager behaviourManager)
    {
        _behaviourManager = behaviourManager;
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
}
