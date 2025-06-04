using Engine.Core.Scenes.Loader.Info;
using GignerEngine.DiContainer;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Engine.Core.Scenes.Loader;

public class YamlSceneLoader : ISceneLoader
{
    private readonly DiContainer _di;
    private readonly string _basePath;

    public YamlSceneLoader(DiContainer di, string basePath)
    {
        _di = di;
        _basePath = basePath;
    }

    public SceneInfo Load(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Scene file not found: {path}");

        var yaml = File.ReadAllText(path);

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var sceneInfo = deserializer.Deserialize<SceneInfo>(yaml);

        return sceneInfo;
    }
}