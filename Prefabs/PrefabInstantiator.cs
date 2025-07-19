using Engine.Core.Behaviours;
using Engine.Core.Entities;
using Engine.Core.Scenes.Loader;

namespace Engine.Core.Prefabs;

public class PrefabInstantiator
{
    private readonly SceneLoader _loader;
    private readonly SceneCreator _creator;
    private readonly EntityBehaviourManager _behaviourManager;

    public PrefabInstantiator(SceneLoader loader, SceneCreator creator, EntityBehaviourManager behaviourManager)
    {
        _loader = loader;
        _creator = creator;
        _behaviourManager = behaviourManager;
    }

    // Загружает префаб и возвращает его как Prefab (Entity + Behaviours)
    public Prefab Load(string prefabPath, Dictionary<string, object>? parameters = null)
    {
        var entityInfo = _loader.Load(prefabPath, parameters);
        var rootEntity = _creator.CreateEntity(entityInfo);
        return new Prefab(rootEntity, _behaviourManager.GetBehaviours(rootEntity).ToList());
    }

    // Инстанцирует Prefab (клонирует + применяет override-компоненты + возвращает новую сущность)
    public Entity Instantiate(Prefab prefab, IEnumerable<IComponent>? overrideComponents = null)
    {
        var clone = DeepCloneEntity(prefab.RootEntity);

        if (overrideComponents != null)
        {
            foreach (var component in overrideComponents)
            {
                clone.AddOrApplyComponent(component);
            }
        }
        
        foreach (var behaviour in prefab.Behaviours)
        {
            _behaviourManager.AttachBehaviour(clone, behaviour);
        }

        return clone;
    }

    private Entity DeepCloneEntity(Entity original)
    {
        var clone = new Entity
        {
            Name = original.Name,
            IsEnabled = original.IsEnabled
        };

        foreach (var (type, component) in original.Components)
        {
            if (component is not IComponent) continue;
            clone.AddOrApplyComponent(component);
        }

        foreach (var child in original.Children)
        {
            var childClone = DeepCloneEntity(child);
            childClone.Parent = clone;
            clone.Children.Add(childClone);
        }

        return clone;
    }
}
