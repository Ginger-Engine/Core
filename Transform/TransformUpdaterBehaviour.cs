using Engine.Core.Behaviours;
using Engine.Core.Entities;

namespace Engine.Core.Transform;

public class TransformUpdaterBehaviour : IEntityBehaviour
{
    public void OnStart(Entity entity)
    {
        entity.SubscribeComponentChangeImmediately<TransformComponent>(e =>
        {
            var newValue = e.newValue;
            var oldValue = e.oldValue;

            if (!Equals(newValue.Transform, oldValue.Transform))
            {
                var offset = newValue.Transform - oldValue.Transform;
                entity.ModifySilently((ref TransformComponent transform) =>
                {
                    transform.WorldTransform += offset;
                });

                foreach (var entityChild in entity.Children)
                {
                    entityChild.Modify((ref TransformComponent transform) =>
                    {
                        transform.WorldTransform += offset;
                    });
                }
                
                return;
            }
            
            if (!Equals(newValue.WorldTransform, oldValue.WorldTransform))
            {
                var offset = newValue.WorldTransform - oldValue.WorldTransform;
                entity.ModifySilently((ref TransformComponent transform) =>
                {
                    transform.Transform += offset;
                });

                foreach (var entityChild in entity.Children)
                {
                    entityChild.Modify((ref TransformComponent transform) =>
                    {
                        transform.Transform += offset;
                    });
                }
            }
        });
        InitTransformSilently(entity);
    }
    
    private static void InitTransformSilently(Entity entity)
    {
        var parent = entity.Parent;
        if (parent == null)
        {
            entity.ModifySilently((ref TransformComponent transformComponent) =>
            {
                transformComponent.WorldTransform = transformComponent.Transform;
            });
            return;
        }
        
        var parentWorldTransform = parent.GetComponent<TransformComponent>().WorldTransform;
        entity.ModifySilently((ref TransformComponent transformComponent) =>
        {
            transformComponent.WorldTransform = new Transform
            {
                Position = parentWorldTransform.Position + transformComponent.Transform.Position,
                Rotation = parentWorldTransform.Rotation + transformComponent.Transform.Rotation,
                Scale = parentWorldTransform.Scale * transformComponent.Transform.Scale
            };
        });
    }
}