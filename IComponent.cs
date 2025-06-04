namespace Engine.Core;

public interface IComponent
{
    void OnAddToEntity(Entities.Entity entity) {}
}