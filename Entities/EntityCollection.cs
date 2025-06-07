namespace Engine.Core.Entities;

public class EntityCollection
{
    private readonly Dictionary<Guid, Entity> _entities = new();
    private readonly List<Entity> _rootEntities = new();

    public IReadOnlyList<Entity> RootEntities => _rootEntities;
    public IReadOnlyDictionary<Guid, Entity> All => _entities;

    public void Add(Entity entity, Guid? parentGuid = null)
    {
        if (!_entities.TryAdd(entity.Id, entity))
            throw new Exception($"Entity with id {entity.Id} already exists");

        if (parentGuid != null && _entities.TryGetValue(parentGuid.Value, out var parent))
        {
            parent.Children.Add(entity);
        }
        else
        {
            _rootEntities.Add(entity);
        }
    }

    public bool TryGet(Guid id, out Entity? entity) => _entities.TryGetValue(id, out entity);

    public void Remove(Guid id)
    {
        if (!_entities.TryGetValue(id, out var entity)) return;

        if (!_rootEntities.Remove(entity))
        {
            foreach (var e in _entities.Values)
                e.Children.Remove(entity);
        }

        RemoveRecursive(entity);
    }

    private void RemoveRecursive(Entity entity)
    {
        foreach (var child in entity.Children)
        {
            RemoveRecursive(child);
        }

        _entities.Remove(entity.Id);
    }

    public void Clear()
    {
        _rootEntities.Clear();
        _entities.Clear();
    }
}