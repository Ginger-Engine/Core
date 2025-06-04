using GignerEngine.DiContainer;

namespace Engine.Core;

public interface IBundle
{
    void InstallBindings(DiBuilder builder);

    void Configure(object config)
    {
        
    }
}