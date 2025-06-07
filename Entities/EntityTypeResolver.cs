using Engine.Core.Serialization;

namespace Engine.Core.Entities;

public class EntityTypeResolver : ITypeResolver<Entity>
{
    public Entity? Resolve(object raw)
    {
        throw new NotImplementedException();
    }

    public object? Resolve(Type type, object raw)
    {
        var value = (string)raw;
        var guid = Guid.Parse(value);
        return new Entity(guid);
    }
}