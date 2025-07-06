using System.Numerics;
using Engine.Core.Behaviours;
using Engine.Core.Entities;

namespace Engine.Core.Transform;

public class TransformUpdaterBehaviour : IEntityBehaviour
{
    public void OnAttach(Entity entity)
    {
        if (!entity.IsComponentExists<WorldTransformComponent>())
        {
            entity.AddComponent(new WorldTransformComponent());
        }
    }

    public void OnStart(Entity entity)
    {
        UpdateWorldTransformRecursive(entity);
        entity.SubscribeComponentChange<TransformComponent>((newValue, oldValue) =>
        {
            UpdateWorldTransformRecursive(entity);
        });
        entity.SubscribeComponentChange<WorldTransformComponent>((newValue, oldValue) =>
        {
            UpdateTransformRecursive(entity);
        });
    }
    
    private void UpdateTransformRecursive(Entity entity)
    {
        var parent = entity.Parent;
        if (parent == null)
            return;

        if (!entity.TryGetComponent<TransformComponent>(out var local))
            return;

        if (!parent.TryGetComponent<WorldTransformComponent>(out var parentWorld))
            throw new Exception("Parent entity has no WorldTransformComponent");

        if (!entity.TryGetComponent<WorldTransformComponent>(out var world))
            throw new Exception("Entity has no WorldTransformComponent");

        // Вычисляем локальную позицию на основе world и родителя
        var newLocalPosition = world.Position - parentWorld.Position;
        var newLocalScale = new Vector2(
            world.Scale.X / parentWorld.Scale.X,
            world.Scale.Y / parentWorld.Scale.Y
        );
        var newLocalRotation = world.Rotation - parentWorld.Rotation;

        entity.ApplyComponent(new TransformComponent
        {
            Position = newLocalPosition,
            Scale = newLocalScale,
            Rotation = newLocalRotation
        });

        foreach (var child in entity.Children)
            UpdateTransformRecursive(child);
    }

    private void UpdateWorldTransformRecursive(Entity entity)
    {
        var parent = entity.Parent;
        var parentPosition = new Vector2();
        var parentScale = new Vector2(1, 1);
        var parentRotation = 0f;
        if (parent != null)
        {
            if (!parent.TryGetComponent<WorldTransformComponent>(out var parentWorld))
            {
                throw new Exception("Parent entity has no WorldTransformComponent");
            }
            parentPosition = parentWorld.Position;
            parentScale = parentWorld.Scale;
            parentRotation = parentWorld.Rotation;
        }

        if (!entity.TryGetComponent<TransformComponent>(out var local))
        {
            throw new Exception("Entity has no TransformComponent");
        }

        entity.ApplyComponent(new WorldTransformComponent
        {
            Position = parentPosition + local.Position,
            Scale = parentScale * local.Scale,
            Rotation = parentRotation + local.Rotation,
        });

        foreach (var child in entity.Children)
            UpdateWorldTransformRecursive(child);
    }
}