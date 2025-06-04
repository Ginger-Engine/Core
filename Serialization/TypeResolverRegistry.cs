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
        if (!_resolvers.TryGetValue(type, out var resolverObj)) return default;
        if (resolverObj is ITypeResolver resolver)
            return resolver.Resolve(type, raw);

        return default;
    }
}