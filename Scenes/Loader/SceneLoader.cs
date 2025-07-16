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
            component.Parameters = (Dictionary<string, object>)ReplaceParameters(component.Parameters, parameters);
        }
    }

    private object ReplaceParameters(object? node, Dictionary<string, object> parameters)
    {
        switch (node)
        {
            case string s when s.StartsWith('$'):
                var key = s.Substring(1);
                return parameters.TryGetValue(key, out var value) ? value : s;

            case Dictionary<object, object> dict:
                var replacedDict = new Dictionary<object, object>();
                foreach (var kvp in dict)
                {
                    replacedDict[kvp.Key] = ReplaceParameters(kvp.Value, parameters);
                }
                return replacedDict;

            case Dictionary<string, object> dictString:
                var replacedDictStr = new Dictionary<string, object>();
                foreach (var kvp in dictString)
                {
                    replacedDictStr[kvp.Key] = ReplaceParameters(kvp.Value, parameters);
                }
                return replacedDictStr;

            case List<object> list:
                var replacedList = new List<object>();
                foreach (var item in list)
                {
                    replacedList.Add(ReplaceParameters(item, parameters));
                }
                return replacedList;

            default:
                return node!;
        }
    }
}
