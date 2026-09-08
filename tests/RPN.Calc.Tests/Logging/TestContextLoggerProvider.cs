using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace RPN.Calc.Tests.Logging;

internal sealed class TestContextLoggerProvider(LogLevel minimumLevel = LogLevel.Information) : ILoggerProvider
{
    private readonly AsyncLocal<TestContext>? _testContext = new();
    private readonly ConcurrentDictionary<string, TestContextLogger> _loggers = new();
    private readonly ConcurrentDictionary<string, LogLevel> _categoryLevels = new(StringComparer.Ordinal);

    /// <summary>
    /// Global fallback minimum level for categories with no explicit override.
    /// </summary>
    public LogLevel MinimumLevel { get; set; } = minimumLevel;

    public TestContext? CurrentTestContext
    {
        get => _testContext?.Value;
        set => _testContext?.Value = value!;
    }

    public ILogger CreateLogger(string categoryName) =>
        _loggers.GetOrAdd(categoryName, name => new TestContextLogger(name, this));

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

    public void ClearLevelOverrides() => _categoryLevels.Clear();

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
