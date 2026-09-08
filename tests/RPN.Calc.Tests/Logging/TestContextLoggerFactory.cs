using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace RPN.Calc.Tests.Logging;

/// <summary>
/// ILoggerFactory implementation. Wraps a TestContextLoggerProvider by default,
/// but also supports attaching additional ILoggerProviders (e.g. if you also
/// want output mirrored to a file or the console during local debugging).
/// </summary>
/// <param name="minimumLevel">The minimum log level to fall back on.</param>
internal sealed class TestContextLoggerFactory(LogLevel minimumLevel = LogLevel.Information) : ILoggerFactory
{
    /// <summary>
    /// <para>Additional providers attached to this factory.</para>
    /// </summary>
    private readonly ConcurrentBag<ILoggerProvider> _extraProviders = [];

    /// <summary>
    /// <para>Tracks whether <see cref="Dispose()" /> has been called.</para>
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// <para>The primary TestContext-backed provider (exposes level control).</para>
    /// </summary>
    public TestContextLoggerProvider Provider { get; } = new(minimumLevel);

    /// <summary>
    /// <para>Attaches an additional provider to this factory.</para>
    /// </summary>
    /// <param name="provider">The provider to attach.</param>
    /// <remarks>
    /// <para>The <paramref name="provider"/> will be disposed when the factory is disposed.</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when the provider is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the factory has already been disposed.</exception>
    public void AddProvider(ILoggerProvider provider)
    {
        ThrowIfDisposed();
        _extraProviders.Add(provider ?? throw new ArgumentNullException(nameof(provider)));
    }

    /// <summary>
    /// <para>Creates a logger for the specified category name.</para>    
    /// </summary>
    /// <param name="categoryName">The category name for messages produced by the logger.</param>
    /// <remarks>
    /// <para>If there is more than one provider attached, the logger will fan out to all providers.</para>
    /// </remarks>
    /// <returns>Fully initialized logger.</returns>
    /// <exception cref="ArgumentException">Thrown when the category name is null or empty.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the factory has already been disposed.</exception>
    public ILogger CreateLogger(string categoryName)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrEmpty(categoryName, nameof(categoryName));

        if (_extraProviders.IsEmpty)
        {
            return Provider.CreateLogger(categoryName);
        }

        List<ILogger> loggers = [Provider.CreateLogger(categoryName)];

        foreach (ILoggerProvider provider in _extraProviders)
        {
            loggers.Add(provider.CreateLogger(categoryName));
        }

        return new CompositeLogger(loggers);
    }

    /// <summary>
    /// <para>Throws an <see cref="ObjectDisposedException"/> if the factory has already been disposed.</para>
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the factory has already been disposed.</exception>
    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, nameof(TestContextLoggerFactory));

    /// <summary>
    /// <para>Disposes the factory and all attached providers.</para>
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Provider.Dispose();
        foreach (ILoggerProvider provider in _extraProviders) provider.Dispose();
    }

    /// <summary>
    /// <para>Composite logger class that fans log data out to multiple loggers.</para>
    /// </summary>
    /// <param name="loggers">List of loggers to send log data to.</param>
    private sealed class CompositeLogger(List<ILogger> loggers) : ILogger
    {
        /// <summary>
        /// <para>List of loggers to send log data to.</para>
        /// </summary>
        private readonly List<ILogger> _loggers = loggers;

        /// <summary>
        /// <para>Begins a logical operation scope for all underlying loggers.</para>
        /// </summary>
        /// <typeparam name="TState">The type of the state to begin scope for.</typeparam>
        /// <param name="state">The identifier for the scope.</param>
        /// <returns>An <see cref="CompositeDisposable"/> that ends the logical operation scope for all underlying loggers.</returns>
        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            IDisposable[] disposables = new IDisposable[_loggers.Count];
            for (int i = 0; i < _loggers.Count; i++)
            {
                disposables[i] = _loggers[i].BeginScope(state) ?? NullScope.Instance;
            }
            return new CompositeDisposable(disposables);
        }

        /// <summary>
        /// <para>Checks if the given log level is enabled for any of the underlying loggers.</para>
        /// </summary>
        /// <param name="logLevel">The log level to check.</param>
        /// <returns><c>true</c> if the log level is enabled for any of the underlying loggers; otherwise, <c>false</c>.</returns>
        public bool IsEnabled(LogLevel logLevel)
        {
            foreach (ILogger logger in _loggers)
            {
                if (logger.IsEnabled(logLevel)) return true;
            }
            return false;
        }

        /// <summary>
        /// <para>Writes a log entry to all underlying loggers.</para>
        /// </summary>
        /// <typeparam name="TState">The type of the state to log.</typeparam>
        /// <param name="logLevel">Entry will be written on this level.</param>
        /// <param name="eventId">ID of the event.</param>
        /// <param name="state">The entry to be written. Can also be an object.</param>
        /// <param name="exception">The exception related to the entry.</param>
        /// <param name="formatter">Function to create a <see cref="string"/> message of the <paramref name="state"/> and <paramref name="exception"/>.</param>
        public void Log<TState>(LogLevel logLevel,
                                EventId eventId,
                                TState state,
                                Exception? exception,
                                Func<TState, Exception?, string> formatter)
        {
            foreach (ILogger logger in _loggers)
            {
                logger.Log(logLevel, eventId, state, exception, formatter);
            }
        }

        /// <summary>
        /// <para>Null scope implementation that does nothing on dispose.</para>
        /// </summary>
        private sealed class NullScope : IDisposable
        {
            /// <summary>
            /// <para>Singleton instance of <see cref="NullScope"/>.</para>
            /// </summary>
            public static readonly NullScope Instance = new();

            /// <summary>
            /// <para>Disposes the <see cref="NullScope"/> instance. Does nothing.</para>
            /// </summary>
            public void Dispose() { }
        }

        /// <summary>
        /// <para>Composite disposable that disposes multiple <see cref="IDisposable"/> instances.</para>
        /// </summary>
        /// <param name="items">Items to dispose.</param>
        private sealed class CompositeDisposable(IDisposable[] items) : IDisposable
        {
            private readonly IDisposable[] _items = items;

            public void Dispose()
            {
                foreach (var d in _items) d.Dispose();
            }
        }
    }
}
