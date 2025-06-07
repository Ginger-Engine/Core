using Engine.Core.Scenes.Loader.Info;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Engine.Core.Scenes.Loader;

public class SceneLoader
{
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