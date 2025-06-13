using System.Reflection;
using Engine.Core.Behaviours;
using Engine.Core.Helper;
using Engine.Core.Scenes;
using Engine.Core.Scenes.Loader;
using Engine.Core.Serialization;
using Engine.Core.Transform;
using GignerEngine.DiContainer;
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


    private static bool TryGetConfig(string name, out string yaml)
    {
        var filename = PathHelper.Normalize("resources/config/" + name + ".yaml");
        if (File.Exists(filename))
        {
            yaml = File.ReadAllText(filename);

            return true;
        }

        yaml = null!;
        return false;
    }

    public void Init()
    {
        var builder = new DiBuilder();

        foreach (var bundle in _bundles)
        {
            bundle.InstallBindings(builder);
        }
        
        _container = new DiContainer();
        builder.Bind<DiContainer>().FromInstance(_container);
        _container.Apply(builder);
        _container.Init();
        
        foreach (var bundle in _bundles)
        {
            if (TryGetConfig(bundle.GetType().FullName ?? throw new Exception(), out var config))
            {
                bundle.Configure(config, _container);
            }
        }
        
        var loader = _container.Resolve<SceneLoader>();
        var creator = _container.Resolve<SceneCreator>();
        var sceneManager = _container.Resolve<SceneManager>();
        var sceneInfo = loader.Load(_config.InitialScene);
        var scene = creator.Create(sceneInfo);
        sceneManager.SetCurrentScene(scene);
    }

    public void Run()
    {
        var sceneManager = _container.Resolve<SceneManager>();
        sceneManager.StartCurrentScene();
        var loop = _container.Resolve<Loop>();
        while (loop.IsRunning())
        {
            loop.Update();
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
