using Engine.Core.Behaviours;
using Engine.Core.Entities;

namespace Engine.Core.Scenes;

public class SceneManager(EntityBehaviourManager behaviourManager)
{
    public Scene? CurrentScene => _currentScene;
    private Scene? _currentScene;

    public void SetCurrentScene(Scene scene)
    {
        _currentScene = scene;
    }

    public void UpdateCurrentScene(float dt)
    {
        if (_currentScene == null) throw new InvalidOperationException("Scene is not set");
        
        foreach (var sb in _currentScene.SceneBehaviours)
            sb.Value.OnUpdate(dt);
        
        UpdateEntities(_currentScene.Entities.All.Values, dt);
    }
    
    private void UpdateEntities(IEnumerable<Entity> entities, float dt)
    {
        foreach (var entity in entities)
            if (entity.IsEnabled)
                UpdateEntity(entity, dt);
    }
    
    private void UpdateEntity(Entity entity, float dt)
    {
        behaviourManager.Update(entity, dt);

        if (entity.Children.Count > 0)
        {
            UpdateEntities(entity.Children, dt);
        }
    }

    public void StartCurrentScene()
    {
        _currentScene.Start();
    }
}