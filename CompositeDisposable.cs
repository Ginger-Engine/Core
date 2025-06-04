namespace Engine.Core;

public class CompositeDisposable : IDisposable
{
    public static CompositeDisposable operator +(CompositeDisposable composite, IDisposable disposable)
    {
        composite.Add(disposable);
        return composite;
    }
    private readonly List<IDisposable> _disposables = new();
    private bool _disposed;

    public void Add(IDisposable disposable)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(CompositeDisposable));
        _disposables.Add(disposable);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var d in _disposables)
        {
            d.Dispose();
        }

        _disposables.Clear();
    }
}