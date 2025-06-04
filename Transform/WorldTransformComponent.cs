using System.Numerics;

namespace Engine.Core.Transform;

public struct WorldTransformComponent : IComponent
{
    public Vector2 Position;
    public float Rotation;
    public Vector2 Scale;
}