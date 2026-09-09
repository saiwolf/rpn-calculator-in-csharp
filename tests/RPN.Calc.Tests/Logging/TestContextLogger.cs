using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

namespace RPN.Calc.Tests.Logging;

/// <summary>
/// <para>Logger implementation that writes to the current <see cref="TestContext"/>.</para>
/// </summary>
/// <param name="categoryName">The category name for messages produced by the logger.</param>
/// <param name="provider">The logger provider for this instance.</param>
internal sealed class TestContextLogger(string categoryName, TestContextLoggerProvider provider) : ILogger
{
    /// <summary>
    /// <para>The category name for messages produced by the logger.</para>
    /// </summary>
    private readonly string _categoryName = categoryName;

    /// <summary>
    /// <para>The logger provider for this instance.</para>
    /// </summary>
    private readonly TestContextLoggerProvider _provider = provider;

    /// <summary>
    /// <para>Begins a logical operation scope.</para>
    /// </summary>
    /// <typeparam name="TState">The type of the state to begin scope for.</typeparam>
    /// <param name="state">The state to begin scope for.</param>
    /// <remarks>
    /// <para>The scope is maintained in a stack and will be included in log messages.</para>
    /// </remarks>
    /// <returns>A disposable object that ends the logical operation scope on disposal.</returns>
    public IDisposable BeginScope<TState>(TState state) where TState : notnull
        => ScopeStack.Push(state);

    /// <summary>
    /// <para>Checks if the given log level is enabled for this logger.</para>
    /// </summary>
    /// <param name="logLevel">The log level to check.</param>
    /// <returns><c>true</c> if <paramref name="logLevel"/> is not <see cref="LogLevel.None"/>
    /// <strong>and</strong> <paramref name="logLevel"/> is greater than or equal to the effective level; otherwise, <c>false</c>.</returns>
    public bool IsEnabled(LogLevel logLevel) =>
        logLevel != LogLevel.None && logLevel >= _provider.GetEffectiveLevel(_categoryName);

    /// <summary>
    /// <para>Writes a log entry.</para>
    /// </summary>
    /// <typeparam name="TState">The type of the state to log.</typeparam>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="eventId">ID of the event.</param>
    /// <param name="state">The entry to be written. Can also be an object.</param>
    /// <param name="exception">The exception related to the entry.</param>
    /// <param name="formatter">Function to create a <see cref="string"/> message of the <paramref name="state"/> and <paramref name="exception"/>.</param>
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

    /// <summary>
    /// <para>Returns a string label for the given <see cref="LogLevel"/>.</para>
    /// </summary>
    /// <param name="level">The log level to get a label for.</param>
    /// <returns>The label for the given log level.</returns>
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
