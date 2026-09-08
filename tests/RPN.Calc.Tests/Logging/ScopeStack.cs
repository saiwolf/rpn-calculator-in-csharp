using System.Collections.Immutable;

namespace RPN.Calc.Tests.Logging;

internal static class ScopeStack
{
    private static readonly AsyncLocal<ImmutableStack<object>?> _stack = new();

    public static ImmutableStack<object> Current = _stack.Value ?? [];

    public static IDisposable Push(object state)
    {
        ImmutableStack<object> previous = Current;
        _stack.Value = previous.Push(state);
        return new PopScope(previous);
    }

    private sealed class PopScope(ImmutableStack<object> previous) : IDisposable
    {
        private readonly ImmutableStack<object> _previous = previous;
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _stack.Value = _previous;
        }
    }
}
