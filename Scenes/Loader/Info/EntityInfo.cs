namespace Engine.Core.Scenes.Loader.Info;

public record struct EntityInfo()
{

    public string Name = string.Empty;
    public List<ComponentInfo> Components = [];
    public List<string> Behaviours = [];
    public List<EntityInfo> Children = [];
    public bool IsEnabled = false;
}