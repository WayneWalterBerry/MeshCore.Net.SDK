using System.Diagnostics.Tracing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MeshCore.Net.SDK.Tests;

/// <summary>
/// Simple ETW Event Listener for test scenarios
/// Captures MeshCore SDK events and logs them for test visibility
/// </summary>
internal sealed class TestEtwEventListener : EventListener
{
    private readonly ILogger _logger;
    private volatile bool _isInitialized;

    public TestEtwEventListener(ILogger? logger = null)
    {
        // Use NullLogger as default to avoid null reference issues
        _logger = logger ?? NullLogger.Instance;
        _isInitialized = true; // Mark as initialized after logger is set
    }

    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        // Only proceed if we're fully initialized
        if (!_isInitialized)
            return;
            
        if (eventSource.Name == "MeshCore-Net-SDK")
        {
            try
            {
                EnableEvents(eventSource, EventLevel.Verbose);
                _logger.LogInformation("Test ETW Event Listener enabled for MeshCore SDK events");
            }
            catch (Exception ex)
            {
                // Fallback to console if logger fails
                Console.WriteLine($"ETW Event Listener initialization error: {ex.Message}");
            }
        }
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        // Only proceed if we're fully initialized
        if (!_isInitialized)
            return;
            
        if (eventData.EventSource.Name != "MeshCore-Net-SDK")
            return;

        try
        {
            var logLevel = MapEventLevelToLogLevel(eventData.Level);
            var message = FormatEventMessage(eventData);
            
            _logger.Log(logLevel, eventData.EventId, "[ETW] {EventName}: {Message}", 
                eventData.EventName, message);
        }
        catch (Exception ex)
        {
            // Fallback to console if logger fails
            Console.WriteLine($"ETW Event logging error: {ex.Message}");
        }
    }

    private static LogLevel MapEventLevelToLogLevel(EventLevel eventLevel)
    {
        return eventLevel switch
        {
            EventLevel.Critical => LogLevel.Critical,
            EventLevel.Error => LogLevel.Error,
            EventLevel.Warning => LogLevel.Warning,
            EventLevel.Informational => LogLevel.Information,
            EventLevel.Verbose => LogLevel.Debug,
            _ => LogLevel.Trace
        };
    }

    private static string FormatEventMessage(EventWrittenEventArgs eventData)
    {
        if (eventData.Payload == null || eventData.Payload.Count == 0)
            return eventData.Message ?? string.Empty;

        try
        {
            return string.Format(eventData.Message ?? "{0}", eventData.Payload.ToArray());
        }
        catch
        {
            return string.Join(", ", eventData.Payload);
        }
    }
}