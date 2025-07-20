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

    public Prefab Load(string prefabPath, Dictionary<string, object>? parameters = null)
    {
        var entityInfo = _loader.Load(prefabPath, parameters);
        var rootEntity = _creator.CreateEntity(entityInfo);
        var behaviours = _behaviourManager.GetBehavioursRecursive(rootEntity);
        return new Prefab(rootEntity, behaviours);
    }

    public Entity Instantiate(Prefab prefab, IEnumerable<IComponent>? overrideComponents = null)
    {
        // Сопоставим оригинальные сущности с их клонами
        var cloneMap = new Dictionary<Entity, Entity>();
        var clone = DeepCloneEntity(prefab.RootEntity, cloneMap);
        foreach (var component in overrideComponents ?? [])
        {
            clone.AddOrApplyComponent(component);
        }

        // Назначаем поведения из prefab.Behaviours на соответствующие клоны
        foreach (var (originalEntity, behaviours) in prefab.BehavioursByEntity)
        {
            if (cloneMap.TryGetValue(originalEntity, out var clonedEntity))
            {
                foreach (var behaviour in behaviours)
                {
                    _behaviourManager.AttachBehaviour(clonedEntity, behaviour);
                    _behaviourManager.Start(clonedEntity);
                }
            }
        }

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

        // Добавляем компоненты через AddComponent<T>, чтобы корректно сработал OnAddToEntity
        foreach (var component in original.Components.Values)
        {
            // runtime-generic вызов, сохраним тип компонента
            clone.AddComponent((dynamic)component);
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
