using System.Reflection;
using Engine.Core.Behaviours;
using Engine.Core.Entities;
using Engine.Core.Scenes.Loader.Info;
using Engine.Core.Serialization;
using GignerEngine.DiContainer;

namespace Engine.Core.Scenes.Loader;

public class SceneCreator
{
    private readonly DiContainer _container;
    private readonly TypeResolverRegistry _resolvers;
    private readonly EntityBehaviourManager _behaviourManager;
    private readonly SceneLoader _sceneLoader;

    public SceneCreator(
        DiContainer container,
        TypeResolverRegistry resolvers,
        EntityBehaviourManager behaviourManager,
        SceneLoader sceneLoader)
    {
        _container = container;
        _resolvers = resolvers;
        _behaviourManager = behaviourManager;
        _sceneLoader = sceneLoader;
    }

    public Scene Create(EntityInfo sceneInfo)
    {
        var scene = new Scene(_behaviourManager);

        var rootEntity = CreateEntity(sceneInfo);
        scene.Entities.Add(rootEntity);

        return scene;
    }

    private Entity CreateEntity(EntityInfo info)
    {
        var entity = new Entity(info.Id)
        {
            Name = info.Name
        };

        foreach (var componentInfo in info.Components)
        {
            var type = ResolveType(componentInfo.Type);
            var instance = Activator.CreateInstance(type)!;

            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!componentInfo.Parameters.TryGetValue(field.Name, out var raw)) continue;
                var value = _resolvers.Resolve(field.FieldType, raw);
                field.SetValue(instance, value);
            }

            entity.AddComponent((dynamic)instance);
        }

        foreach (var behaviourName in info.Behaviours ?? [])
        {
            var type = ResolveType(behaviourName);
            var behaviour = _container.Resolve(type) as IEntityBehaviour
                            ?? throw new Exception($"Invalid entity behaviour: {behaviourName}");
            _behaviourManager.AttachBehaviour(entity, behaviour);
        }

        foreach (var childInfo in info.Children)
        {
            var child = CreateEntity(childInfo);
            child.Parent = entity;
            entity.Children.Add(child);
        }

        return entity;
    }

    private Type ResolveType(string typeName)
    {
        return Type.GetType(typeName)
               ?? AppDomain.CurrentDomain.GetAssemblies()
                   .SelectMany(a => a.GetTypes())
                   .FirstOrDefault(t => t.FullName == typeName || t.Name == typeName)
               ?? throw new Exception($"Unknown type: {typeName}");
    }
}
