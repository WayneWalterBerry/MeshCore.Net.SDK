using System.Diagnostics.Tracing;
using Microsoft.Extensions.Logging;
using MeshCore.Net.SDK.Logging;

namespace MeshCore.Net.SDK.Demo.Logging;

/// <summary>
/// ETW Event Listener that captures MeshCore SDK events and republishes them to ILogger
/// This demonstrates how applications can consume the ETW events from the SDK
/// </summary>
public sealed class MeshCoreSdkEventListener : EventListener
{
    private readonly ILogger<MeshCoreSdkEventListener> _logger;

    public MeshCoreSdkEventListener(ILogger<MeshCoreSdkEventListener> logger)
    {
        _logger = logger;
    }

    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        // Enable the MeshCore SDK event source
        if (eventSource.Name == "MeshCore-Net-SDK")
        {
            EnableEvents(eventSource, EventLevel.Verbose);
            _logger.LogInformation("ETW Event Listener enabled for MeshCore SDK events");
        }
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        if (eventData.EventSource.Name != "MeshCore-Net-SDK")
            return;

        var logLevel = MapEventLevelToLogLevel(eventData.Level);
        var message = FormatEventMessage(eventData);
        
        // Log the ETW event through the application's logger
        _logger.Log(logLevel, eventData.EventId, "[ETW] {EventName}: {Message}", 
            eventData.EventName, message);
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
        if (eventData.PayloadNames == null || eventData.Payload == null)
            return eventData.Message ?? "No message";

        var message = eventData.Message ?? "";
        
        // Simple parameter substitution for ETW events
        for (int i = 0; i < eventData.PayloadNames.Count && i < eventData.Payload.Count; i++)
        {
            var paramName = eventData.PayloadNames[i];
            var paramValue = eventData.Payload[i]?.ToString() ?? "null";
            
            // Replace both {paramName} and {i} style placeholders
            message = message.Replace($"{{{paramName}}}", paramValue);
            message = message.Replace($"{{{i}}}", paramValue);
        }
        
        return message;
    }
}