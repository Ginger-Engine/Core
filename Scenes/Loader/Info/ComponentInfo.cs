namespace Engine.Core.Scenes.Loader.Info;

public record struct ComponentInfo()
{
    public string Type = string.Empty;
    public Dictionary<string, object> Parameters = [];
}