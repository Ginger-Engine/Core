using System.Reflection;
using Engine.Core.Behaviours;
using Engine.Core.Di;
using Engine.Core.Helper;
using Engine.Core.Scenes;
using Engine.Core.Scenes.Loader;
using Engine.Core.Serialization;
using Engine.Core.Transform;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using ITypeResolver = Engine.Core.Serialization.ITypeResolver;

namespace Engine.Core;

public class Project
{
    private readonly List<IBundle> _bundles = new();
    private DiContainer? _container;
    private ProjectConfig _config;
    public DiContainer Container => _container ?? throw new InvalidOperationException("Project not initialized");
    
    private ProjectState _state = new();

    public void RegisterBundle(IBundle bundle)
    {
        _bundles.Add(bundle);
    }


    private static bool TryGetConfig(string name, out object config)
    {
        var filename = PathHelper.Normalize("resources/config/" + name + ".yaml");
        if (File.Exists(filename))
        {
            var yaml = File.ReadAllText(filename);

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            config = deserializer.Deserialize(yaml);
            return true;
        }

        config = null!;
        return false;
    }

    public void Init()
    {
        var builder = new DiBuilder();

        foreach (var bundle in _bundles)
        {
            bundle.InstallBindings(builder);
            if (TryGetConfig(bundle.GetType().FullName ?? throw new Exception(), out var config))
            {
                bundle.Configure(config);
            }
        }

        InstallBindings(builder);
        
        _container = new DiContainer();
        builder.Bind<DiContainer>().FromInstance(_container);
        _container.Apply(builder);
        _container.Init();
        
        var loader = _container.Resolve<ISceneLoader>();
        var creator = _container.Resolve<ISceneCreator>();
        var sceneInfo = loader.Load(_config.InitialScene);
        var scene = creator.Create(sceneInfo);
        _state.CurrentScene = scene;
    }

    private void InstallBindings(DiBuilder builder)
    {
        builder.BindParameter("basePath", "./resources");
        builder.Bind<ISceneLoader>().From<YamlSceneLoader>();
        builder.Bind<TransformUpdaterBehaviour>();
        builder.Bind<EntityBehaviourManager>();
        builder.Bind<ISceneCreator>().From<SceneCreator>();
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
    }

    public void Run()
    {
        var loop = new Loop();
        while (loop.IsRunning())
        {
            loop.Update(_state.CurrentScene);
        }
    }

    public void Configure(string configPath)
    {
        var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configPath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Config file not found: {fullPath}");

        var yaml = File.ReadAllText(fullPath);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var config = deserializer.Deserialize<ProjectConfig>(yaml);
        if (config == null)
            throw new Exception("Invalid project config");

        _config = config;
        var assemblyNames = config.Bundles
            .Select(bundleName =>
            {
                // Предположим, что bundleName — это полное имя типа
                var nameParts = bundleName.Split('.');
                // Возьми всё кроме последнего — это имя пространства
                var ns = string.Join(".", nameParts.Take(nameParts.Length - 1));
                return ns;
            })
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct();

        foreach (var asmName in assemblyNames)
        {
            if (!AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == asmName))
            {
                try
                {
                    Assembly.Load(asmName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[warn] Failed to preload assembly '{asmName}': {ex.Message}");
                }
            }
        }
        foreach (var bundleName in config.Bundles)
        {
            var type = Type.GetType(bundleName)
                       ?? AppDomain.CurrentDomain.GetAssemblies()
                           .SelectMany(a => a.GetTypes())
                           .FirstOrDefault(t => t.FullName == bundleName);

            if (type == null)
                throw new Exception($"Bundle type not found: {bundleName}");

            if (Activator.CreateInstance(type) is not IBundle bundle)
                throw new Exception($"Type '{bundleName}' is not a valid IBundle");

            RegisterBundle(bundle);
        }
    }
} 
