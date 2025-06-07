using System.Numerics;

namespace Engine.Core.Serialization.Resolvers;

public class Vector2TypeResolver : ITypeResolver<Vector2>
{
    public object? Resolve(Type type, object raw)
    {
        var dict = raw as Dictionary<object, object>;
        return new Vector2(float.Parse((string)dict["x"]), float.Parse((string)dict["y"]));
    }
}