using System.Numerics;

namespace Engine.Core.Serialization.Resolvers;

public class TransformTypeResolver(Vector2TypeResolver vector2TypeResolver) : ITypeResolver<Transform.Transform>
{
    public object? Resolve(Type type, object raw)
    {
        var dict = raw as Dictionary<string, object>;
        return new Transform.Transform
        {
            Position = (Vector2)vector2TypeResolver.Resolve(typeof(Vector2), dict["Position"]),
            Rotation = float.Parse(dict["Rotation"].ToString()),
            Scale =    (Vector2)vector2TypeResolver.Resolve(typeof(Vector2), dict["Scale"]),
        };
    }
}