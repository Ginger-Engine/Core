namespace Engine.Core.Stages;

public interface IStage
{
    public Type[] Before { get; set; }
    public Type[] After { get; set; }
    
    public void Start();
    public void Update(float dt);
}