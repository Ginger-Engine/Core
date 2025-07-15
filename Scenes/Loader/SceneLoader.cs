using Engine.Core.Scenes.Loader.Info;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Engine.Core.Scenes.Loader;

public class SceneLoader
{
    private readonly IDeserializer _deserializer;

    public SceneLoader()
    {
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
    }

    public EntityInfo Load(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Scene file not found: {path}");

        var yaml = File.ReadAllText(path);
        var root = _deserializer.Deserialize<EntityInfo>(yaml);
        ResolvePrefabsRecursive(root, new Dictionary<string, object>());
        return root;
    }

    private void ResolvePrefabsRecursive(EntityInfo entity, Dictionary<string, object> inheritedParameters)
    {
        // Merge parameters from parent and current entity
        var currentParams = entity.Parameters ?? [];
        var mergedParams = new Dictionary<string, object>(inheritedParameters);
        foreach (var kv in currentParams)
            mergedParams[kv.Key] = kv.Value;

        if (!string.IsNullOrWhiteSpace(entity.Prefab))
        {
            var prefabPath = entity.Prefab;
            var prefab = Load(prefabPath);
            ApplyParameters(prefab, mergedParams);

            entity.Components = prefab.Components;
            entity.Behaviours = prefab.Behaviours;
            entity.Children = prefab.Children;
            entity.Prefab = null;
            entity.Parameters = null;
        }

        foreach (var child in entity.Children)
        {
            ResolvePrefabsRecursive(child, mergedParams);
        }
    }

    private void ApplyParameters(EntityInfo entity, Dictionary<string, object> parameters)
    {
        foreach (var component in entity.Components)
        {
            var keys = component.Parameters.Keys.ToList();
            foreach (var key in keys)
            {
                if (component.Parameters[key] is string s && s.StartsWith('$'))
                {
                    var paramKey = s[1..];
                    if (parameters.TryGetValue(paramKey, out var value))
                        component.Parameters[key] = value;
                }
            }
        }
    }
}
