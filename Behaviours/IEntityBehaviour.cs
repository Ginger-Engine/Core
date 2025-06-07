namespace Engine.Core.Behaviours;

public interface IEntityBehaviour
{
    void OnAttach(Entities.Entity entity) {}
    void OnStart(Entities.Entity entity) {}
    void OnUpdate(Entities.Entity entity, float dt) {}
    void OnDestroy(Entities.Entity entity) {}
}