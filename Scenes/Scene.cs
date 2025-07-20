using Engine.Core.Behaviours;
using Engine.Core.Entities;

namespace Engine.Core.Scenes;

public class Scene
{
    public EntityCollection Entities = new();
    public Guid Id { get; set; }
    public string Name { get; set; }

    private readonly EntityBehaviourManager _behaviourManager;

    public Scene(EntityBehaviourManager behaviourManager)
    {
        _behaviourManager = behaviourManager;
    }

    public void Start()
    {
        foreach (var (key, entity) in Entities.All)
        {
            _behaviourManager.Start(entity);
        }
    }
}
