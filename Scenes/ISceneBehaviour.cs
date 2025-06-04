namespace Engine.Core.Scenes;

public interface ISceneBehaviour
{
    void OnStart() {}
    void OnUpdate(float dt) {}
    void OnActivate() {}
    void OnDeactivate() {}
    void OnDestroy() {}
}