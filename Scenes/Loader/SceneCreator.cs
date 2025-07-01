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

    public SceneCreator(DiContainer container, TypeResolverRegistry resolvers, EntityBehaviourManager behaviourManager)
    {
        _container = container;
        _resolvers = resolvers;
        _behaviourManager = behaviourManager;
    }

    public Scene Create(SceneInfo info)
    {
        var scene = new Scene(_container);

        foreach (var entityInfo in info.Entities)
        {
            var entity = CreateEntity(entityInfo);
            scene.Entities.Add(entity);
            foreach (var child in entity.Children)
            {
                scene.Entities.Add(child);
            }
        }

        foreach (var behaviourName in info.Behaviours ?? [])
        {
            var type = Type.GetType(behaviourName) ?? AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.FullName == (string?)behaviourName || t.Name == (string?)behaviourName)
                ?? throw new Exception($"Unknown scene behaviour type: {behaviourName}");

            if (_container.Resolve(type) is not ISceneBehaviour behaviour)
                throw new Exception($"Invalid scene behaviour type: {behaviourName}");

            scene.AttachBehaviour(behaviour.GetType());
        }
        
        foreach (var (_, entity) in scene.Entities.All)
        {
            foreach (var (type, component) in entity.Components)
            {
                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    var fieldType = field.FieldType;

                    if (fieldType == typeof(Entity))
                    {
                        if (field.GetValue(component) is Entity stub && stub.Id != Guid.Empty)
                        {
                            if (scene.Entities.TryGet(stub.Id, out var realEntity))
                                field.SetValue(component, realEntity);
                        }
                    }

                    else if (fieldType == typeof(Entity[]))
                    {
                        if (field.GetValue(component) is Entity[] stubs)
                        {
                            var resolved = stubs
                                .Select(stub => scene.Entities.TryGet(stub.Id, out var realEntity) ? realEntity : null)
                                .Where(e => e != null)
                                .ToArray();

                            field.SetValue(component, resolved);
                        }
                    }

                    else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>)
                                                     && fieldType.GetGenericArguments()[0] == typeof(Entity))
                    {
                        if (field.GetValue(component) is List<Entity> stubs)
                        {
                            var resolved = stubs
                                .Select(stub => scene.Entities.TryGet(stub.Id, out var realEntity) ? realEntity : null)
                                .Where(e => e != null)
                                .ToList();

                            field.SetValue(component, resolved);
                        }
                    }
                }
            }
        }

        return scene;
    }

    private Entity CreateEntity(EntityInfo info)
    {
        var entity = new Entity(info.Id)
        {
            Name = info.Name,
        };

        foreach (var componentInfo in info.Components)
        {
            var type = Type.GetType(componentInfo.Type) ?? AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.FullName == (string?)componentInfo.Type || t.Name == (string?)componentInfo.Type)
                ?? throw new Exception($"Unknown component type: {componentInfo.Type}");

            var instance = Activator.CreateInstance(type)
                           ?? throw new Exception($"Cannot create component: {componentInfo.Type}");

            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!componentInfo.Parameters.TryGetValue(field.Name, out var raw)) continue;

                var value = _resolvers.Resolve(field.FieldType, raw);
                field.SetValue(instance, value);
            }

            entity.AddComponent((dynamic)instance);
        }

        foreach (var behaviourName in info.Behaviours)
        {
            var type = Type.GetType(behaviourName) ?? AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.FullName == (string?)behaviourName || t.Name == (string?)behaviourName)
                ?? throw new Exception($"Unknown behaviour type: {behaviourName}");

            if (_container.Resolve(type) is not IEntityBehaviour behaviour)
                throw new Exception($"Invalid entity behaviour type: {behaviourName}");

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
}