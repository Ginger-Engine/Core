namespace Engine.Core.Behaviours;

public interface IEntityBehaviour
{
    void OnStart(Entities.Entity entity) {}
    void OnUpdate(Entities.Entity entity, float dt) {}
    void OnDestroy(Entities.Entity entity) {}
}