using System.Numerics;

namespace Engine.Core.Transform;

public struct TransformComponent : IComponent
{
    public Vector2 Position;
    public float Rotation;
    public Vector2 Scale;
}