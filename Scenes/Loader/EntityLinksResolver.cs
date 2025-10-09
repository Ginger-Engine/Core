using System.Reflection;
using Engine.Core.Entities;

namespace Engine.Core.Scenes.Loader;

public class EntityLinksResolver
{
    private static void ResolveEntityLinks(Prefab prefab)
    {
        foreach (var (_, entity) in prefab.Entities.All)
        {
            ProcessEntity(prefab, entity);
        }
    }

    private static void ProcessEntity(Prefab prefab, Entity entity)
    {
        foreach (var (type, component) in entity.Components)
        {
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var fieldType = field.FieldType;

                // Одиночная ссылка
                if (fieldType == typeof(Entity))
                {
                    if (field.GetValue(component) is Entity stub && stub.Id != Guid.Empty)
                    {
                        if (prefab.Entities.TryGet(stub.Id, out var realEntity))
                            field.SetValue(component, realEntity);
                    }
                }

                // Массив ссылок
                else if (fieldType == typeof(Entity[]))
                {
                    if (field.GetValue(component) is Entity[] stubs)
                    {
                        var resolved = stubs
                            .Select(stub => prefab.Entities.TryGet(stub.Id, out var realEntity) ? realEntity : null)
                            .Where(e => e != null)
                            .ToArray();

                        field.SetValue(component, resolved);
                    }
                }

                // Список ссылок
                else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>)
                                                 && fieldType.GetGenericArguments()[0] == typeof(Entity))
                {
                    if (field.GetValue(component) is List<Entity> stubs)
                    {
                        var resolved = stubs
                            .Select(stub => prefab.Entities.TryGet(stub.Id, out var realEntity) ? realEntity : null)
                            .Where(e => e != null)
                            .ToList();

                        field.SetValue(component, resolved);
                    }
                }
            }
        }
    }
}