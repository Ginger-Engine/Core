using System.Reflection;
using Engine.Core.Behaviours;
using Engine.Core.Entities;
using Engine.Core.Exceptions;
using Engine.Core.Scenes.Loader.Info;
using Engine.Core.Serialization;
using GignerEngine.DiContainer;

namespace Engine.Core.Scenes.Loader;

public class SceneCreator
{
    private readonly DiContainer _container;
    private readonly TypeResolverRegistry _resolvers;
    private readonly World _world;

    public SceneCreator(
        DiContainer container,
        TypeResolverRegistry resolvers,
        World world)
    {
        _container = container;
        _resolvers = resolvers;
        _world = world;
    }

    public Prefab Create(EntityInfo sceneInfo)
    {
        var rootEntity = CreateEntity(sceneInfo);
        var entitiesCollection = new EntityCollection(rootEntity);
        var scene = new Prefab(
            entitiesCollection,
            ResolveBehaviours(ExtractBehaviourInfos(sceneInfo), entitiesCollection)
        );
        // scene.Entities.Add(rootEntity);

        ResolveEntityLinks(scene);

        return scene;
    }

    private static void ResolveEntityLinks(Prefab prefab)
    {
        foreach (var (_, entity) in prefab.Entities.All)
        {
            foreach (var (type, component) in entity.Components)
            {
                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    var fieldType = field.FieldType;

                    // Одиночная ссылка
                    if (fieldType == typeof(Entity))
                    {
                        if (field.GetValue(component) is Entity stub && stub.Id != Guid.Empty)
                        {
                            if (prefab.Entities.TryGet(stub.Id, out var realEntity))
                                field.SetValue(component, realEntity);
                        }
                    }

                    // Массив ссылок
                    else if (fieldType == typeof(Entity[]))
                    {
                        if (field.GetValue(component) is Entity[] stubs)
                        {
                            var resolved = stubs
                                .Select(stub => prefab.Entities.TryGet(stub.Id, out var realEntity) ? realEntity : null)
                                .Where(e => e != null)
                                .ToArray();

                            field.SetValue(component, resolved);
                        }
                    }

                    // Список ссылок
                    else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>)
                                                     && fieldType.GetGenericArguments()[0] == typeof(Entity))
                    {
                        if (field.GetValue(component) is List<Entity> stubs)
                        {
                            var resolved = stubs
                                .Select(stub => prefab.Entities.TryGet(stub.Id, out var realEntity) ? realEntity : null)
                                .Where(e => e != null)
                                .ToList();

                            field.SetValue(component, resolved);
                        }
                    }
                }
            }
        }
    }

    public Dictionary<EntityInfo, HashSet<string>> ExtractBehaviourInfos(EntityInfo entityInfo,
        Dictionary<EntityInfo, HashSet<string>>? result = null)
    {
        result ??= new Dictionary<EntityInfo, HashSet<string>>();
        result.Add(entityInfo, []);
        foreach (var entityInfoBehaviour in entityInfo.Behaviours)
        {
            result[entityInfo].Add(entityInfoBehaviour);
        }

        foreach (var entityInfoChild in entityInfo.Children)
        {
            ExtractBehaviourInfos(entityInfoChild, result);
        }

        return result;
    }

    public Dictionary<Type, HashSet<Entity>> ResolveBehaviours(
        Dictionary<EntityInfo, HashSet<string>> behavioursMap, EntityCollection entityCollection)
    {
        var result = new Dictionary<Type, HashSet<Entity>>();

        foreach (var (entityInfo, behaviours) in behavioursMap)
        {
            foreach (var behaviourName in behaviours)
            {
                var behaviour = ResolveBehaviour(behaviourName);
                var behaviourType = behaviour.GetType();
                if (!result.ContainsKey(behaviourType))
                {
                    result.Add(behaviourType, []);
                } 

                if (!entityCollection.TryGet(entityInfo.Id, out var entity))
                {
                    throw new EntityNotFoundException(entityInfo.Id);
                }

                result[behaviourType].Add(entity);
            }
        }

        return result;
    }

    private IEntityBehaviour ResolveBehaviour(string behaviourName)
    {
        var type = ResolveType(behaviourName);
        return _container.Resolve(type) as IEntityBehaviour
               ?? throw new Exception($"Invalid entity behaviour: {behaviourName}");
    }

    public Entity CreateEntity(EntityInfo info)
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