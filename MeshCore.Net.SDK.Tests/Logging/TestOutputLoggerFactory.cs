using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace MeshCore.Net.SDK.Tests.Logging;

/// <summary>
/// Simple ILoggerFactory that creates loggers writing to xUnit test output.
/// </summary>
internal sealed class TestOutputLoggerFactory : ILoggerFactory
{
    private readonly ITestOutputHelper _output;
    private readonly LogLevel _minLevel;

    public TestOutputLoggerFactory(ITestOutputHelper output, LogLevel minLevel = LogLevel.Debug)
    {
        _output = output;
        _minLevel = minLevel;
    }

    public void AddProvider(ILoggerProvider provider)
    {
        // Providers are not used in this minimal factory.
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new TestOutputLogger<object>(_output, categoryName, _minLevel);
    }

    public ILogger<T> CreateLogger<T>()
    {
        return new TestOutputLogger<T>(_output, _minLevel);
    }

    public void Dispose()
    {
        // Nothing to dispose.
    }
}
