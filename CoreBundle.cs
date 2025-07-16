using System.Numerics;
using Engine.Core.Behaviours;
using Engine.Core.Entities;
using Engine.Core.Scenes;
using Engine.Core.Scenes.Loader;
using Engine.Core.Serialization;
using Engine.Core.Serialization.Resolvers;
using Engine.Core.Stages;
using Engine.Core.Transform;
using GignerEngine.DiContainer;

namespace Engine.Core;

public class CoreBundle : IBundle
{
    public void InstallBindings(DiBuilder builder)
    {
        builder.BindParameter("basePath", "./resources");
        builder.Bind<EntityBehaviourManager>();
        builder.Bind<SceneManager>();
        builder.Bind<ITypeResolver<Entity>>().From<EntityTypeResolver>();
        builder.Bind<ITypeResolver<Vector2>>().From<Vector2TypeResolver>();
        builder.Bind<ITypeResolver<Transform.Transform>>().From<TransformTypeResolver>();
        builder.Bind<TypeResolverRegistry>().AfterInit((registryObj, container) =>
        {
            var resolvers = container.ResolveAll(typeof(ITypeResolver<>));
            var registry = (TypeResolverRegistry)registryObj;

            foreach (var resolver in resolvers)
            {
                var resolverType = resolver.GetType();
                var iface = resolverType
                    .GetInterfaces()
                    .FirstOrDefault(i =>
                        i.IsGenericType &&
                        i.GetGenericTypeDefinition() == typeof(ITypeResolver<>));

                if (iface != null)
                {
                    var targetType = iface.GetGenericArguments()[0];
                    registry.Register(targetType, (ITypeResolver)resolver);
                }
            }
        });
        builder.Bind<LogicStage>();
    }
}