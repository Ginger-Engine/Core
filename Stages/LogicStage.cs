using Engine.Core.Scenes;
using GignerEngine.DiContainer;

namespace Engine.Core.Stages;

public class LogicStage(World world) : IStage
{
    public Type[] Before { get; set; } = [];
    public Type[] After { get; set; } = [];

    public void Start()
    {
        world.Start();
    }

    public void Update(float dt)
    {
        world.ProcessPendingEntities();
        world.ProcessPendingAttaches();
        world.Update(dt);
        world.FlushPendingComponentChanges();
    }
}