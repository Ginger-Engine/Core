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
            if (!_resolvers.TryGetValue(elementType, out var resolverObj)) return null;
            if (resolverObj is not ITypeResolver resolver) return null;
            var array = raw as List<object>;
            if (array == null) return null;
            
            var result = Array.CreateInstance(elementType, array.Count);
            for (var i = 0; i < array.Count; i++)
            {
                var item = resolver.Resolve(elementType, array[i]);
                result.SetValue(item, i);
            }
            return result;
        }
        else
        {
            if (!_resolvers.TryGetValue(type, out var resolverObj)) return null;
            if (resolverObj is ITypeResolver resolver)
                return resolver.Resolve(type, raw);

            return null;
        }
    }
}