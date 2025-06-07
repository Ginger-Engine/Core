using System.ComponentModel;

namespace Engine.Core.Serialization;

public class TypeResolverRegistry
{
    private readonly Dictionary<Type, object> _resolvers = new();

    public void Register<T>(ITypeResolver<T> resolver)
    {
        _resolvers[typeof(T)] = resolver;
    }
    
    public void Register(Type targetType, ITypeResolver resolver)
    {
        _resolvers[targetType] = resolver;
    }

    public T? Resolve<T>(object raw)
    {
        if (!_resolvers.TryGetValue(typeof(T), out var resolverObj)) return default;
        if (resolverObj is ITypeResolver<T> resolver)
            return resolver.Resolve(raw);

        return default;
    }

    public object? Resolve(Type type, object raw)
    {
        if (type.IsArray)
        {
            var elementType = type.GetElementType();
            _resolvers.TryGetValue(elementType, out var resolverObj);
            var array = raw as List<object>;
            if (array == null) return null;
            
            var result = Array.CreateInstance(elementType, array.Count);
            for (var i = 0; i < array.Count; i++)
            {
                var item = resolverObj is ITypeResolver resolver
                    ? ResolveElement(resolver, elementType, array[i])
                    : ResolveElement(null, elementType, array[i]);
                result.SetValue(item, i);
            }
            return result;
        }
        else
        {
            if (_resolvers.TryGetValue(type, out var resolverObj))
                return ResolveElement((ITypeResolver)resolverObj, type, raw);
            
            return ResolveElement(null, type, raw);
        }
    }

    private object? ResolveElement(ITypeResolver? typeResolver, Type type, object raw)
    {
        if (typeResolver == null && IsScalarType(type))
        {
            return ConvertScalar((string)raw, type);
        }
        
        if (typeResolver != null)
        {
            return typeResolver.Resolve(type, raw);
        }
        
        throw new Exception($"Cannot resolve type {type.FullName}.");
    }
    
    private static bool IsScalarType(Type type)
    {
        return type.IsPrimitive
               || type.IsEnum
               || type == typeof(string)
               || type == typeof(decimal)
               || type == typeof(DateTime)
               || type == typeof(Guid);
    }
    
    private static object? ConvertScalar(string input, Type targetType)
    {
        if (targetType.IsEnum)
            return Enum.Parse(targetType, input, ignoreCase: true);

        var converter = TypeDescriptor.GetConverter(targetType);
        if (converter.CanConvertFrom(typeof(string)))
        {
            return converter.ConvertFromInvariantString(input);
        }

        throw new InvalidOperationException($"Cannot convert string to {targetType}");
    }
}