using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

namespace RPN.Calc.Tests.Logging;

internal sealed class TestContextLogger(string categoryName, TestContextLoggerProvider provider) : ILogger
{
    private readonly string _categoryName = categoryName;
    private readonly TestContextLoggerProvider _provider = provider;

    public IDisposable BeginScope<TState>(TState state) where TState : notnull
        => ScopeStack.Push(state);

    public bool IsEnabled(LogLevel logLevel) =>
        logLevel != LogLevel.None && logLevel >= _provider.GetEffectiveLevel(_categoryName);

    public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        string message = formatter(state, exception);
        if (string.IsNullOrEmpty(message) && exception is null) return;

        StringBuilder sb = new();

        sb.Append('[').Append(DateTime.Now.ToString("HH:mm:ss.fff")).Append("] ");

        sb.Append(LevelLabel(logLevel)).Append(' ');

        sb.Append(_categoryName);

        if (eventId.Id != 0)
        {
            sb.Append('[').Append(eventId.Id).Append(']');
        }

        ImmutableStack<object> scopes = ScopeStack.Current;

        if (!scopes.IsEmpty)
        {
            sb.Append(" => ");
            bool first = true;
            foreach (var scope in scopes)
            {
                if (!first) sb.Append(" => ");
                sb.Append(scope);
                first = false;
            }
        }

        sb.Append(": ").Append(message);
        if (exception is not null)
        {
            sb.Append(Environment.NewLine).Append(exception);
        }

        string logLine = sb.ToString();
        var ctx = _provider.CurrentTestContext;
        if (ctx is not null)
        {
            ctx.WriteLine(logLine);
        }
        else
        {
            Debug.WriteLine(logLine);
        }
    }

    private static string LevelLabel(LogLevel level) => level switch
    {
        LogLevel.Trace => "Trace",
        LogLevel.Debug => "Debug",
        LogLevel.Information => "Info",
        LogLevel.Warning => "Warn",
        LogLevel.Error => "Error",
        LogLevel.Critical => "Critical",
        LogLevel.None or _ => "None",
    };
}
