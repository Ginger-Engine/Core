using System.Numerics;

namespace Engine.Core.Serialization.Resolvers;

public class Vector2TypeResolver : ITypeResolver<Vector2>
{
    public object? Resolve(Type type, object raw)
    {
        if (raw is Dictionary<string, object> dict)
        {
            var x = dict.TryGetValue("x", out var xVal) ? Convert.ToSingle(xVal) : 0f;
            var y = dict.TryGetValue("y", out var yVal) ? Convert.ToSingle(yVal) : 0f;
            return new Vector2(x, y);
        }
        
        return Vector2.Zero;
    }
}