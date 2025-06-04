namespace Engine.Core.Serialization;

public abstract class TypeResolverBase<T> : ITypeResolver<T>
{
    public abstract T? Resolve(object raw);

    public object? Resolve(Type type, object raw)
    {
        if (type != typeof(T))
            throw new InvalidOperationException($"Invalid type requested: {type}, expected: {typeof(T)}");

        return Resolve(raw);
    }
}