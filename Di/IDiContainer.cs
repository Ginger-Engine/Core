namespace Engine.Core.Di
{
    public interface IDiContainer : IReadonlyDiContainer
    {
        public void Init();
    }
}