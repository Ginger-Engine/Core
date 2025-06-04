using GignerEngine.DiContainer;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Engine.Core;

public interface IBundle
{
    void InstallBindings(DiBuilder builder);

    void Configure(string config, IReadonlyDiContainer diContainer)
    {
    }
}