using System.Numerics;

namespace Engine.Core.Transform;

public struct TransformComponent : IComponent
{
    public Transform Transform;

    public Transform WorldTransform;
}

public struct Transform()
{
    public Vector2 Position = Vector2.Zero;
    public float Rotation = 0;
    public Vector2 Scale = Vector2.One;

    public static Transform operator +(Transform transform, Transform offset)
    {
        transform.Position += offset.Position;
        transform.Rotation += offset.Rotation;
        transform.Scale *= offset.Scale;

        return transform;
    }

    public static Transform operator -(Transform transform, Transform offset)
    {
        transform.Position -= offset.Position;
        transform.Rotation -= offset.Rotation;
        transform.Scale /= offset.Scale;

        return transform;
    }

    public override string ToString()
    {
        return String.Concat(
            new[]
            {
                Position.ToString(),
                ", ",
                Rotation.ToString(),
                ", ",
                Scale.ToString(),
            }
        );
    }

    public static explicit operator string(Transform transform)
    {
        return string.Concat(
            new[]
            {
                transform.Position.ToString(),
                ", ",
                transform.Rotation.ToString(),
                ", ",
                transform.Scale.ToString(),
            }
        );
    }
}