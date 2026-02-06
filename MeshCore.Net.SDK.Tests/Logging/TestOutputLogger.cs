using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace MeshCore.Net.SDK.Tests.Logging;

/// <summary>
/// ILogger implementation that writes log messages to xUnit test output.
/// </summary>
/// <typeparam name="T">The logger category type.</typeparam>
internal sealed class TestOutputLogger<T> : ILogger<T>, IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _categoryName;
    private readonly LogLevel _minLevel;

    // Static so the header/footer only appear once per test run, even with multiple logger instances.
    private static bool _etwHeaderWritten;
    private static bool _etwFooterWritten;

    public TestOutputLogger(ITestOutputHelper output, LogLevel minLevel)
    {
        _output = output;
        _minLevel = minLevel;
        _categoryName = typeof(T).FullName ?? typeof(T).Name;
    }

    // Non-generic constructor used when category name is provided explicitly.
    public TestOutputLogger(ITestOutputHelper output, string categoryName, LogLevel minLevel)
    {
        _output = output;
        _minLevel = minLevel;
        _categoryName = categoryName;
    }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => this;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= _minLevel;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        if (formatter == null)
        {
            throw new ArgumentNullException(nameof(formatter));
        }

        var message = formatter(state, exception);

        var isEtw = message.StartsWith("[ETW]", StringComparison.Ordinal) ||
                    message.StartsWith("Test ETW Event Listener", StringComparison.Ordinal);

        if (isEtw)
        {
            // Write a single header the first time we see ETW output.
            if (!_etwHeaderWritten)
            {
                _output.WriteLine(string.Empty);
                _output.WriteLine("===== ETW SDK EVENT TRACE BEGIN =====");
                _etwHeaderWritten = true;
            }
        }

        _output.WriteLine($"[{logLevel}] {_categoryName} ({eventId.Id}): {message}");

        if (exception != null)
        {
            _output.WriteLine(exception.ToString());
        }

        if (isEtw)
        {
            // Mark that we've seen ETW so a footer can be written later.
            _etwFooterWritten = true;
        }
    }

    public void Dispose()
    {
        // When the last logger is disposed at the end of the test run, write a single footer
        // if we emitted any ETW lines. This keeps the test output structured without
        // cluttering every line.
        if (_etwFooterWritten)
        {
            _output.WriteLine("===== ETW SDK EVENT TRACE END =====");
            _output.WriteLine(string.Empty);

            // Reset for potential subsequent tests in the same process.
            _etwHeaderWritten = false;
            _etwFooterWritten = false;
        }
    }
}
