namespace Engine.Core.Scenes.Loader.Info;

public record struct SceneInfo()
{
    public List<string> Behaviours = [];
    public List<EntityInfo> Entities = [];
}