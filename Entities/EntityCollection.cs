namespace Engine.Core.Entities;

public class EntityCollection
{
    private readonly Dictionary<Guid, Entity> _entities = new();
    public IReadOnlyDictionary<Guid, Entity> All => _entities;

    public void Add(Entity entity)
    {
        if (!_entities.TryAdd(entity.Id, entity))
            throw new Exception($"Entity with id {entity.Id} already exists");

        var children = entity.Children.ToArray();
        foreach (var entityChild in children)
        {
            Add(entityChild);
            entity.Children.Add(entityChild);
        }
    }

    public bool TryGet(Guid id, out Entity? entity) => _entities.TryGetValue(id, out entity);

    public void Remove(Guid id)
    {
        if (!_entities.TryGetValue(id, out var entity)) return;
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
        _entities.Clear();
    }
}