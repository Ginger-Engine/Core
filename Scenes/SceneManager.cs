using System.Collections;
using Engine.Core.Behaviours;
using Engine.Core.Entities;

namespace Engine.Core.Scenes;

public class SceneManager(EntityBehaviourManager behaviourManager)
{
    private Queue<Entity> _entitiesToAdd = [];
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

    public void AddEntityToCurrentScene(Entity entity)
    {
        _entitiesToAdd.Enqueue(entity);
    }
    
    private void UpdateEntities(IEnumerable<Entity> entities, float dt)
    {
        foreach (var entity in entities)
            if (entity.IsEnabled)
                behaviourManager.Update(entity, dt);
    }

    public void StartCurrentScene()
    {
        _currentScene.Start();
    }

    public void ProcessAddEntitiesQueue()
    {
        while (_entitiesToAdd.Count > 0)
        {
            var entity = _entitiesToAdd.Dequeue();
            _currentScene.Entities.Add(entity);
            behaviourManager.Start(entity);
        }
    }

    public void FlushPendingComponentChanges()
    {
        foreach (var entity in _currentScene?.Entities.All.Values ?? [])
        {
            entity.FlushPendingNotifications();
        }
    }
}