namespace Engine.Core.Scenes.Loader.Info;

public class EntityInfo()
{
    public Guid Id = Guid.Empty;
    public string Name = string.Empty;
    public List<ComponentInfo> Components = [];
    public List<string> Behaviours = [];
    public List<EntityInfo> Children = [];
    public bool IsEnabled = false;
    public string Prefab = string.Empty;
    public Dictionary<string, object> Parameters = [];
    public Dictionary<string, List<EntityInfo>> Slots = [];
    public string Slot = string.Empty;
}