using Engine.Core.Entities;

namespace Engine.Core;

public class EntityCompositeDisposable : IDisposable
{
    private Dictionary<Entity, CompositeDisposable> _disposables = [];
    private bool _disposed;
    
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var (entity, d) in _disposables)
        {
            d.Dispose();
        }

        _disposables.Clear();
    }
}