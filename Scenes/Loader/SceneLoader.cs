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

    public EntityInfo Load(string path, Dictionary<string, object>? parameters = null)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Scene file not found: {path}");

        var yaml = File.ReadAllText(path);
        var root = _deserializer.Deserialize<EntityInfo>(yaml);
        root.Parameters = parameters ?? [];

        // Этап 1: разрешение всех префабов и слотов
        ResolveAllPrefabsAndSlots(root, root.Slots);

        // Этап 2: применение параметров
        ApplyParametersRecursive(root, root.Parameters ?? []);

        return root;
    }

    private void ResolveAllPrefabsAndSlots(EntityInfo entity, Dictionary<string, List<EntityInfo>> parentSlots)
    {
        // Префабы могут быть вложенными: A -> B -> C
        while (!string.IsNullOrWhiteSpace(entity.Prefab))
        {
            var prefab = LoadPrefabOnly(entity.Prefab);

            var externalSlots = entity.Slots ?? [];
            var externalParameters = entity.Parameters ?? [];

            entity.Components = prefab.Components;
            entity.Behaviours = prefab.Behaviours;
            entity.Children = prefab.Children;
            entity.Slots = new Dictionary<string, List<EntityInfo>>(prefab.Slots);
            foreach (var (key, value) in parentSlots)
            {
                entity.Slots.Add(key, new List<EntityInfo>(value));
            }
            entity.Prefab = prefab.Prefab;

            // Поверх префабных слотов накладываем внешние
            foreach (var kv in externalSlots)
                entity.Slots[kv.Key] = kv.Value;

            if (entity.Parameters == null)
                entity.Parameters = externalParameters;
        }

        // Обработка слотов
        var newChildren = new List<EntityInfo>();
        foreach (var child in entity.Children ?? [])
        {
            if (!string.IsNullOrEmpty(child.Slot))
            {
                if (entity.Slots.TryGetValue(child.Slot, out var slotContent))
                {
                    foreach (var slotItem in slotContent)
                    {
                        ResolveAllPrefabsAndSlots(slotItem, entity.Slots);
                        newChildren.Add(slotItem);
                    }
                }
                if (parentSlots.TryGetValue(child.Slot, out var parentSlotContent))
                {
                    foreach (var slotItem in parentSlotContent)
                    {
                        ResolveAllPrefabsAndSlots(slotItem, entity.Slots);
                        newChildren.Add(slotItem);
                    }
                }
                // иначе пропускаем слот
            }
            else
            {
                ResolveAllPrefabsAndSlots(child, entity.Slots);
                newChildren.Add(child);
            }
        }

        entity.Children = newChildren;
    }

    private EntityInfo LoadPrefabOnly(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Prefab not found: {path}");

        var yaml = File.ReadAllText(path);
        return _deserializer.Deserialize<EntityInfo>(yaml);
    }

    private void ApplyParametersRecursive(EntityInfo entity, Dictionary<string, object> inherited)
    {
        var current = entity.Parameters ?? [];
        var merged = new Dictionary<string, object>(inherited);
        foreach (var kv in current)
            merged[kv.Key] = kv.Value;

        ApplyParameters(entity, merged);

        foreach (var child in entity.Children)
            ApplyParametersRecursive(child, merged);
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
        var result = ReplaceParametersInternal(node, parameters);
        return NormalizeTypes(result);
    }

    private object ReplaceParametersInternal(object? node, Dictionary<string, object> parameters)
    {
        switch (node)
        {
            case string s:
                return ResolveStringParameter(s, parameters);

            case Dictionary<object, object> dict:
                return dict.ToDictionary(kvp => kvp.Key, kvp => ReplaceParametersInternal(kvp.Value, parameters));

            case Dictionary<string, object> dictStr:
                return dictStr.ToDictionary(kvp => kvp.Key, kvp => ReplaceParametersInternal(kvp.Value, parameters));

            case List<object> list:
                return list.Select(item => ReplaceParametersInternal(item, parameters)).ToList();

            default:
                return node!;
        }
    }

    private object ResolveStringParameter(string input, Dictionary<string, object> parameters)
    {
        string result = input;
        int safety = 10; // предотвратить бесконечный цикл

        while (result.StartsWith('$') && safety-- > 0)
        {
            var key = result[1..];
            if (parameters.TryGetValue(key, out var value))
            {
                if (value is string strValue)
                    result = strValue;
                else
                    return value;
            }
            else break;
        }

        return result;
    }


    private object NormalizeTypes(object node)
    {
        return node switch
        {
            Dictionary<object, object> dict => dict.ToDictionary(kvp => kvp.Key.ToString() ?? "", kvp => NormalizeTypes(kvp.Value)),
            Dictionary<string, object> dictStr => dictStr.ToDictionary(kvp => kvp.Key, kvp => NormalizeTypes(kvp.Value)),
            List<object> list => list.Select(NormalizeTypes).ToList(),
            _ => node
        };
    }
}
