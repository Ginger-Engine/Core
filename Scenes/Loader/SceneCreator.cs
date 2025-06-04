using System.Reflection;
using Engine.Core.Behaviours;
using Engine.Core.Entities;
using Engine.Core.Scenes.Loader.Info;
using Engine.Core.Serialization;
using GignerEngine.DiContainer;

namespace Engine.Core.Scenes.Loader;

public class SceneCreator : ISceneCreator
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
            var entity = CreateEntity(scene, entityInfo);
            scene.RootEntities.Add(entity);
        }

        foreach (var behaviourName in info.SceneBehaviours)
        {
            var type = Type.GetType(behaviourName) ?? AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.FullName == (string?)behaviourName || t.Name == (string?)behaviourName)
                ?? throw new Exception($"Unknown scene behaviour type: {behaviourName}");

            if (_container.Resolve(type) is not ISceneBehaviour behaviour)
                throw new Exception($"Invalid scene behaviour type: {behaviourName}");

            scene.RegisterBehaviour(behaviour.GetType());
        }

        return scene;
    }

    private Entity CreateEntity(Scene scene, EntityInfo info)
    {
        var entity = new Entity(scene);

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

            _behaviourManager.AddBehaviour(entity, behaviour);
        }

        foreach (var childInfo in info.Children)
        {
            var child = CreateEntity(scene, childInfo);
            entity.Children.Add(child);
        }

        return entity;
    }
}