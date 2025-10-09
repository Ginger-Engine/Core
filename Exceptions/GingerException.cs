namespace Engine.Core.Exceptions;

public class GingerException(string message = "") : Exception(message);

public class EntityNotFoundException(Guid entityId) : GingerException($"Entity '{entityId}' not found");