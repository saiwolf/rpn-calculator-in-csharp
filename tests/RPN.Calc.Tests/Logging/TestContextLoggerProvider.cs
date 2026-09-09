using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace RPN.Calc.Tests.Logging;

/// <summary>
/// <para>Provides <see cref="TestContextLogger"/> instances for use in unit tests.</para>
/// </summary>
/// <remarks>
/// <para>Uses <see cref="AsyncLocal{T}"/> to maintain test context between parallel test executions.</para>
/// </remarks>
/// <param name="minimumLevel">The minimum level for the logger. Defaults to <see cref="LogLevel.Information"/>.</param>
internal sealed class TestContextLoggerProvider(LogLevel minimumLevel = LogLevel.Information) : ILoggerProvider
{
    /// <summary>
    /// <para>Maintains the current <see cref="TestContext"/> for the executing test.</para>
    /// </summary>
    private readonly AsyncLocal<TestContext>? _testContext = new();

    /// <summary>
    /// <para>Maintains a thread safe dictionary of <see cref="TestContextLogger"/> instances for each category.</para>
    /// </summary>
    private readonly ConcurrentDictionary<string, TestContextLogger> _loggers = new();

    /// <summary>
    /// <para>Maintains a thread safe dictionary of category prefixes to log levels.</para>
    /// </summary>
    private readonly ConcurrentDictionary<string, LogLevel> _categoryLevels = new(StringComparer.Ordinal);

    /// <summary>
    /// Global fallback minimum level for categories with no explicit override.
    /// </summary>
    public LogLevel MinimumLevel { get; set; } = minimumLevel;

    /// <summary>
    /// <para>Gets or sets the current <see cref="TestContext"/> for the executing test.</para>
    /// </summary>
    public TestContext? CurrentTestContext
    {
        get => _testContext?.Value;
        set => _testContext?.Value = value!;
    }

    /// <summary>
    /// <para>Creates a logger for the specified category name and adds it to <see cref="_loggers"/>.</para>
    /// </summary>
    /// <param name="categoryName">The category name for messages produced by the logger.</param>
    /// <returns>Fully initialized logger.</returns>
    public ILogger CreateLogger(string categoryName) =>
        _loggers.GetOrAdd(categoryName, name => new TestContextLogger(name, this));

    /// <summary>
    /// <para>Disposes the provider by clearing the <see cref="_loggers"/> dictionary.</para>
    /// </summary>
    public void Dispose() => _loggers.Clear();

    /// <summary>
    /// <para>Overrides the minimum level for a category or category prefix.</para>
    /// </summary>
    /// <example>
    /// <code>
    /// SetLevel("Microsoft", LogLevel.Warning)
    /// SetLevel("Microsoft.AspNetCore", LogLevel.Debug)
    /// </code>
    /// </example>
    /// <remarks>
    /// <para>The longest matching prefix wins. Same semantics as appsettings.json <c>LogLevel</c> section.</para>
    /// </remarks>
    public void SetLevel(string categoryPrefix, LogLevel level) =>
        _categoryLevels[categoryPrefix] = level;

    /// <summary>
    /// <para>Clears all category level overrides by clearing the <see cref="_categoryLevels"/> dictionary.</para>
    /// </summary>
    public void ClearLevelOverrides() => _categoryLevels.Clear();

    /// <summary>
    /// <para>Gets the effective log level for a category name by checking for the longest matching prefix in <see cref="_categoryLevels"/>.</para>
    /// </summary>
    /// <param name="categoryName">The category name for messages produced by the logger.</param>
    /// <returns>The effective <see cref="LogLevel"/> that matches the <paramref name="categoryName"/> given.</returns>
    internal LogLevel GetEffectiveLevel(string categoryName)
    {
        LogLevel best = MinimumLevel;
        int bestLen = -1;
        foreach (KeyValuePair<string, LogLevel> categoryLevel in _categoryLevels)
        {
            if (categoryLevel.Key.Length > bestLen &&
                categoryName.StartsWith(categoryLevel.Key, StringComparison.Ordinal))
            {
                best = categoryLevel.Value;
                bestLen = categoryLevel.Key.Length;
            }
        }
        return best;
    }
}
