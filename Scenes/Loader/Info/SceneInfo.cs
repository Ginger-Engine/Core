namespace Engine.Core.Scenes.Loader.Info;

public record struct SceneInfo()
{
    public List<string> SceneBehaviours = [];
    public List<EntityInfo> Entities = [];
}