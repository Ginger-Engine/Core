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
        entity.SubscribeComponentChange<TransformComponent>((newValue, _) =>
        {
            UpdateWorldTransformRecursive(entity, newValue);
        });
        entity.SubscribeComponentChange<WorldTransformComponent>((newValue, _) =>
        {
            UpdateTransformRecursive(entity, newValue);
        });
    }
    
    private void UpdateTransformRecursive(Entity entity, WorldTransformComponent? world = null)
    {
        var parent = entity.Parent;
        if (parent == null)
            return;

        if (!parent.TryGetComponent<WorldTransformComponent>(out var parentWorld))
            throw new Exception("Parent entity has no WorldTransformComponent");
        
        WorldTransformComponent worldTransform;
        if (world == null)
        {
            if (!entity.TryGetComponent(out worldTransform))
                throw new Exception("Entity has no TransformComponent");
        }
        else
        {
            worldTransform = world.Value;
        }
        
        // Вычисляем локальную позицию на основе world и родителя
        var newLocalPosition = worldTransform.Position - parentWorld.Position;
        var newLocalScale = new Vector2(
            worldTransform.Scale.X / parentWorld.Scale.X,
            worldTransform.Scale.Y / parentWorld.Scale.Y
        );
        var newLocalRotation = worldTransform.Rotation - parentWorld.Rotation;

        entity.ApplyComponentSilently(new TransformComponent
        {
            Position = newLocalPosition,
            Scale = newLocalScale,
            Rotation = newLocalRotation
        });

        foreach (var child in entity.Children)
            UpdateTransformRecursive(child);
    }

    private void UpdateWorldTransformRecursive(Entity entity, TransformComponent? local = null)
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

        TransformComponent localTransform;
        if (local == null)
        {
            if (!entity.TryGetComponent(out localTransform))
                throw new Exception("Entity has no TransformComponent");
        }
        else
        {
            localTransform = local.Value;
        }

        entity.ApplyComponentSilently(new WorldTransformComponent
        {
            Position = parentPosition + localTransform.Position,
            Scale = parentScale * localTransform.Scale,
            Rotation = parentRotation + localTransform.Rotation,
        });

        foreach (var child in entity.Children)
            UpdateWorldTransformRecursive(child);
    }
}