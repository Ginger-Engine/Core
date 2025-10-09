using Engine.Core.Behaviours;
using Engine.Core.Entities;
using Engine.Core.Scenes.Loader;

namespace Engine.Core.Scenes;

public class Instantiator
{
    private readonly SceneLoader _loader;
    private readonly SceneCreator _creator;
    private readonly World _world;

    public Instantiator(SceneLoader loader, SceneCreator creator, World world)
    {
        _loader = loader;
        _creator = creator;
        _world = world;
    }

    public Prefab Load(string scenePath, Dictionary<string, object>? parameters = null)
    {
        var entityInfo = _loader.Load(scenePath, parameters);
        return _creator.Create(entityInfo);
    }

    public Entity Instantiate(Prefab prefab, Entity? parent = null, IEnumerable<IComponent>? overrideComponents = null)
    {
        // Сопоставим оригинальные сущности с их клонами
        var cloneMap = new Dictionary<Entity, Entity>();
        var clone = DeepCloneEntity(prefab.Entities.All.First().Value, cloneMap);
        foreach (var component in overrideComponents ?? [])
        {
            clone.AddOrApplyComponent(component);
        }
        
        _world.AddAsChild(parent, clone, prefab.EntityBehaviours);

        return clone;
    }


    private Entity DeepCloneEntity(Entity original, Dictionary<Entity, Entity> cloneMap)
    {
        var clone = new Entity
        {
            Name = original.Name,
            IsEnabled = original.IsEnabled
        };

        cloneMap[original] = clone;

        foreach (var (_, component) in original.Components)
        {
            if (component is IComponent ic)
                clone.AddOrApplyComponent(ic);
        }

        foreach (var child in original.Children)
        {
            var childClone = DeepCloneEntity(child, cloneMap);
            childClone.Parent = clone;
            clone.Children.Add(childClone);
        }

        return clone;
    }
}
