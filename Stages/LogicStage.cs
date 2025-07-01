using Engine.Core.Scenes;

namespace Engine.Core.Stages;

public class LogicStage(SceneManager sceneManager) : IStage
{
    public Type[] Before { get; set; } = [];
    public Type[] After { get; set; } = [];

    public void Start()
    {
    }

    public void Update(float dt)
    {
        sceneManager.ProcessAddEntitiesQueue();
        sceneManager.UpdateCurrentScene(dt);
    }
}