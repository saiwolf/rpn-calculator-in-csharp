using System.Collections.Immutable;

namespace RPN.Calc.Tests.Logging;

/// <summary>
/// A simple stack of logical scopes that is local to the current async context.
/// </summary>
internal static class ScopeStack
{
    /// <summary>
    /// <para>Async-local stack of scopes.</para>
    /// </summary>
    private static readonly AsyncLocal<ImmutableStack<object>?> _stack = new();

    /// <summary>
    /// <para>Gets the current stack of scopes or a blank stack if the stack has no values.</para>
    /// </summary>
    public static ImmutableStack<object> Current = _stack.Value ?? [];

    /// <summary>
    /// <para>Pushes a new scope onto the stack and returns an <see cref="IDisposable"/> that will pop the scope when disposed.</para>
    /// </summary>
    /// <param name="state">The state for the new scope.</param>
    /// <returns>An <see cref="IDisposable"/> that will pop the scope when disposed.</returns>
    public static IDisposable Push(object state)
    {
        ImmutableStack<object> previous = Current;
        _stack.Value = previous.Push(state);
        return new PopScope(previous);
    }

    /// <summary>
    /// <para>An <see cref="IDisposable"/> that pops a scope from the stack when disposed.</para>
    /// </summary>
    /// <param name="previous">The previous stack of scopes.</param>
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
