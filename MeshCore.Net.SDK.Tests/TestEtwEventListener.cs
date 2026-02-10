using System.Diagnostics.Tracing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MeshCore.Net.SDK.Tests;

/// <summary>
/// Simple ETW Event Listener for test scenarios
/// Captures MeshCore SDK events and logs them for test visibility
/// Supports dynamic enable/disable during test execution
/// </summary>
internal sealed class TestEtwEventListener : EventListener
{
    private readonly ILogger _logger;
    private volatile bool _isInitialized;
    private volatile bool _isEnabled = true;
    private EventSource? _meshCoreEventSource;

    /// <summary>
    /// Initializes a new instance of the TestEtwEventListener class
    /// </summary>
    /// <param name="logger">Logger instance for capturing ETW events</param>
    /// <param name="enabled">Initial enabled state (default: true)</param>
    public TestEtwEventListener(ILogger? logger = null, bool enabled = true)
    {
        // Use NullLogger as default to avoid null reference issues
        _logger = logger ?? NullLogger.Instance;
        _isEnabled = enabled;
        _isInitialized = true; // Mark as initialized after logger is set

        if (!enabled)
        {
            _logger.LogDebug("Test ETW Event Listener created in disabled state");
        }
    }

    /// <summary>
    /// Gets or sets whether the ETW listener is actively capturing events
    /// Can be toggled during test execution to control log verbosity
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled == value)
                return;

            _isEnabled = value;

            if (_meshCoreEventSource != null)
            {
                if (_isEnabled)
                {
                    EnableEvents(_meshCoreEventSource, EventLevel.Verbose);
                    _logger.LogInformation("Test ETW Event Listener enabled for MeshCore SDK events");
                }
                else
                {
                    DisableEvents(_meshCoreEventSource);
                    _logger.LogInformation("Test ETW Event Listener disabled for MeshCore SDK events");
                }
            }
        }
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
                _meshCoreEventSource = eventSource;

                if (_isEnabled)
                {
                    EnableEvents(eventSource, EventLevel.Verbose);
                    _logger.LogInformation("Test ETW Event Listener enabled for MeshCore SDK events");
                }
                else
                {
                    _logger.LogDebug("Test ETW Event Listener found MeshCore SDK EventSource but is disabled");
                }
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
        // Only proceed if we're fully initialized and enabled
        if (!_isInitialized || !_isEnabled)
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