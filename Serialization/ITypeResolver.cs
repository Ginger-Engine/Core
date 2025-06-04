namespace Engine.Core.Serialization;

public interface ITypeResolver
{
    object? Resolve(Type type, object raw);
}
public interface ITypeResolver<out T> : ITypeResolver
{
    T? Resolve(object raw);
}