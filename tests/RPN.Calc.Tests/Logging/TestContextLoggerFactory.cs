using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace RPN.Calc.Tests.Logging;

/// <summary>
/// ILoggerFactory implementation. Wraps a TestContextLoggerProvider by default,
/// but also supports attaching additional ILoggerProviders (e.g. if you also
/// want output mirrored to a file or the console during local debugging).
/// </summary>
internal sealed class TestContextLoggerFactory(LogLevel minimumLevel = LogLevel.Information) : ILoggerFactory
{
    private readonly ConcurrentBag<ILoggerProvider> _extraProviders = new();
    private bool _disposed;

    /// <summary>
    /// <para>The primary TestContext-backed provider (exposes level control).</para>
    /// </summary>
    public TestContextLoggerProvider Provider { get; } = new(minimumLevel);

    public void AddProvider(ILoggerProvider provider)
    {
        ThrowIfDisposed();
        _extraProviders.Add(provider ?? throw new ArgumentNullException(nameof(provider)));
    }

    public ILogger CreateLogger(string categoryName)
    {
        ThrowIfDisposed();
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

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, nameof(TestContextLoggerFactory));

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Provider.Dispose();
        foreach (ILoggerProvider provider in _extraProviders) provider.Dispose();
    }

    /// <summary>
    /// <para>Fans a single log call out to multiple loggers. Used only when extra providers are attached.</para>
    /// </summary>
    /// <param name="loggers">List of loggers to send log data to.</param>
    private sealed class CompositeLogger(List<ILogger> loggers) : ILogger
    {
        private readonly List<ILogger> _loggers = loggers;

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            IDisposable[] disposables = new IDisposable[_loggers.Count];
            for (int i = 0; i < _loggers.Count; i++)
            {
                disposables[i] = _loggers[i].BeginScope(state) ?? NullScope.Instance;
            }
            return new CompositeDisposable(disposables);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            foreach (ILogger logger in _loggers)
            {
                if (logger.IsEnabled(logLevel)) return true;
            }
            return false;
        }

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

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }

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
