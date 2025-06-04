namespace Engine.Core.Scenes;

public interface IComponentDeserializer
{
    bool CanHandle(Type type);
    object? Deserialize(Type type, object rawData);
}