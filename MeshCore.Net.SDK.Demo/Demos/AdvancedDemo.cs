using Microsoft.Extensions.Logging;
using MeshCore.Net.SDK;
using MeshCore.Net.SDK.Models;
using MeshCore.Net.SDK.Transport;
using System.Diagnostics;

namespace MeshCore.Net.SDK.Demo.Demos;

/// <summary>
/// Advanced demonstration showcasing transport architecture, concurrent operations, and ETW logging
/// </summary>
public class AdvancedDemo
{
    public static async Task RunAsync(DeviceConnectionType? preferredTransport = null, ILoggerFactory? loggerFactory = null)
    {
        var logger = loggerFactory?.CreateLogger<AdvancedDemo>() ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<AdvancedDemo>.Instance;

        logger.LogInformation("?? Advanced MeshCore.Net.SDK Demo");
        logger.LogInformation("This demo showcases:");
        logger.LogInformation("  ? Transport architecture and abstraction");
        logger.LogInformation("  ? ETW (Event Tracing for Windows) integration");
        logger.LogInformation("  ? Structured logging with Microsoft.Extensions.Logging");
        logger.LogInformation("  ? Concurrent device operations");
        logger.LogInformation("  ? Performance monitoring and metrics");
        logger.LogInformation("  ? Best practices for .NET 8 SDK development");

        try
        {
            await DemonstrateTransportArchitecture(preferredTransport, loggerFactory, logger);
            await DemonstrateLoggingCapabilities(loggerFactory, logger);
            await DemonstrateConcurrentOperations(preferredTransport, loggerFactory, logger);
            await DemonstratePerformanceMonitoring(preferredTransport, loggerFactory, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Advanced demo failed");
        }
        finally
        {
            logger.LogInformation("?? Advanced demo completed");
        }
    }

    private static async Task DemonstrateTransportArchitecture(DeviceConnectionType? preferredTransport, ILoggerFactory? loggerFactory, ILogger logger)
    {
        logger.LogInformation("???  === TRANSPORT ARCHITECTURE DEMONSTRATION ===");
        logger.LogInformation("The SDK is designed with a pluggable transport layer:");
        logger.LogInformation("  ITransport interface allows multiple connection types");
        logger.LogInformation("  UsbTransport: Fully implemented ?");
        logger.LogInformation("  BluetoothTransport: Architecture ready, implementation planned ??");
        logger.LogInformation("  Future transports: WiFi TCP, Serial, etc. ??");

        // Discover devices across all transport types
        logger.LogInformation("Discovering devices across all available transports...");

        var allDevices = new List<MeshCoreDevice>();

        // USB Transport
        try
        {
            var usbDevices = await UsbTransport.DiscoverDevicesAsync();
            allDevices.AddRange(usbDevices);
            logger.LogInformation("USB Transport: Found {DeviceCount} devices", usbDevices.Count);

            foreach (var device in usbDevices)
            {
                logger.LogInformation("  ?? {DeviceName} - {DeviceId}", device.Name, device.Id);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "USB transport discovery failed");
        }

        // Bluetooth Transport (architecture demonstration)
        try
        {
            logger.LogInformation("Bluetooth LE Transport: Architecture demonstration...");
            var bleDevices = await MeshCoreClient.DiscoverBluetoothDevicesAsync(TimeSpan.FromSeconds(2), loggerFactory);
            allDevices.AddRange(bleDevices);
            logger.LogInformation("Bluetooth LE: Would find {DeviceCount} devices (implementation coming in v2.0)", bleDevices.Count);
        }
        catch (NotImplementedException ex)
        {
            logger.LogInformation("? Bluetooth LE architecture in place: {Message}", ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Bluetooth transport discovery demonstration failed");
        }

        // Transport abstraction benefits
        logger.LogInformation("?? Transport Abstraction Benefits:");
        logger.LogInformation("  ? Same API works across USB, Bluetooth LE, and future transports");
        logger.LogInformation("  ? Applications don't need transport-specific code");
        logger.LogInformation("  ? Easy to add new transport types without breaking changes");
        logger.LogInformation("  ? Consistent error handling and logging across all transports");

        if (allDevices.Count > 0 && allDevices.Any(d => d.ConnectionType == DeviceConnectionType.USB))
        {
            var device = allDevices.First(d => d.ConnectionType == DeviceConnectionType.USB);
            logger.LogInformation("Demonstrating polymorphic transport usage with {DeviceName}...", device.Name);

            try
            {
                using (var client = await MeshCoreClient.ConnectAsync(device, loggerFactory))
                {
                    var deviceInfo = await client.GetDeviceInfoAsync();
                    logger.LogInformation("? Connected via {TransportType}: {DeviceId}", device.ConnectionType, deviceInfo.DeviceId);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Transport demonstration connection failed");
            }
        }

        logger.LogInformation("???  === END TRANSPORT ARCHITECTURE ===\n");
    }

    private static async Task DemonstrateLoggingCapabilities(ILoggerFactory? loggerFactory, ILogger logger)
    {
        logger.LogInformation("?? === LOGGING & ETW DEMONSTRATION ===");
        logger.LogInformation("The SDK implements .NET 8 logging best practices:");
        logger.LogInformation("  ? Microsoft.Extensions.Logging abstractions");
        logger.LogInformation("  ? ETW (Event Tracing for Windows) support");
        logger.LogInformation("  ? Source-generated high-performance logging");
        logger.LogInformation("  ? Structured logging with semantic events");
        logger.LogInformation("  ? No-op defaults - silent by default");

        logger.LogInformation("?? ETW Event Source: 'MeshCore-Net-SDK'");
        logger.LogInformation("Events are published to both ILogger and ETW simultaneously");

        // Demonstrate different log levels
        logger.LogTrace("?? This is a trace level message (most detailed)");
        logger.LogDebug("?? This is a debug level message (development info)");
        logger.LogInformation("??  This is an information level message (general info)");
        logger.LogWarning("??  This is a warning level message (potential issues)");
        logger.LogError("? This is an error level message (operation failures)");

        // Demonstrate structured logging
        var demoData = new
        {
            DeviceId = "demo-device-123",
            Operation = "LoggingDemo",
            Duration = TimeSpan.FromMilliseconds(42),
            Success = true
        };

        logger.LogInformation("?? Structured logging example: Operation {Operation} on device {DeviceId} took {Duration}ms (Success: {Success})",
            demoData.Operation, demoData.DeviceId, demoData.Duration.TotalMilliseconds, demoData.Success);

        logger.LogInformation("?? External ETW Monitoring:");
        logger.LogInformation("  You can monitor these events externally using:");
        logger.LogInformation("  - Windows Performance Toolkit (WPA)");
        logger.LogInformation("  - PerfView (Microsoft)");
        logger.LogInformation("  - Custom ETW consumers");
        logger.LogInformation("  - Azure Monitor / Application Insights");

        logger.LogInformation("?? === END LOGGING DEMONSTRATION ===\n");

        await Task.Delay(100); // Brief pause for demonstration
    }

    private static async Task DemonstrateConcurrentOperations(DeviceConnectionType? preferredTransport, ILoggerFactory? loggerFactory, ILogger logger)
    {
        logger.LogInformation("? === CONCURRENT OPERATIONS DEMONSTRATION ===");
        logger.LogInformation("Demonstrating thread-safe operations and concurrent device access...");

        try
        {
            // Discover devices
            var devices = await MeshCoreClient.DiscoverDevicesAsync(loggerFactory: loggerFactory);
            var usbDevices = devices.Where(d => d.ConnectionType == DeviceConnectionType.USB).ToList();

            if (usbDevices.Count == 0)
            {
                logger.LogInformation("No USB devices available for concurrent operations demo");
                return;
            }

            var device = usbDevices.First();
            logger.LogInformation("Using device {DeviceName} for concurrent operations demo", device.Name);

            using (var client = await MeshCoreClient.ConnectAsync(device, loggerFactory))
            {

                // Demonstrate concurrent operations (SDK handles thread safety)
                logger.LogInformation("Executing concurrent operations...");

                var tasks = new List<Task>
                {
                    ConcurrentOperation("GetDeviceInfo", () => client.GetDeviceInfoAsync(), logger),
                    ConcurrentOperation("GetDeviceTime", () => client.GetDeviceTimeAsync(), logger),
                    ConcurrentOperation("GetNetworkStatus", () => client.GetNetworkStatusAsync(), logger),
                    ConcurrentOperation("GetConfiguration", () => client.GetBatteryAndStorageAsync(), logger)
                };


                var stopwatch = Stopwatch.StartNew();
                await Task.WhenAll(tasks);
                stopwatch.Stop();

                logger.LogInformation("? All concurrent operations completed in {TotalTime}ms", stopwatch.ElapsedMilliseconds);
                logger.LogInformation("?? SDK handled thread safety automatically");
                logger.LogInformation("?? Operations were executed concurrently where possible");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Concurrent operations demonstration failed");
        }

        logger.LogInformation("? === END CONCURRENT OPERATIONS ===\n");
    }

    private static async Task ConcurrentOperation<T>(string operationName, Func<Task<T>> operation, ILogger logger)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            logger.LogDebug("Starting concurrent operation: {OperationName}", operationName);
            var result = await operation();
            stopwatch.Stop();

            logger.LogDebug("? {OperationName} completed in {Duration}ms", operationName, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogWarning(ex, "? {OperationName} failed after {Duration}ms", operationName, stopwatch.ElapsedMilliseconds);
        }
    }

    private static async Task DemonstratePerformanceMonitoring(DeviceConnectionType? preferredTransport, ILoggerFactory? loggerFactory, ILogger logger)
    {
        logger.LogInformation("?? === PERFORMANCE MONITORING DEMONSTRATION ===");
        logger.LogInformation("The SDK includes built-in performance monitoring:");
        logger.LogInformation("  ? Operation timing and metrics");
        logger.LogInformation("  ? ETW performance events");
        logger.LogInformation("  ? Slow operation detection");
        logger.LogInformation("  ? Memory allocation monitoring");

        try
        {
            var devices = await MeshCoreClient.DiscoverDevicesAsync(loggerFactory: loggerFactory);
            var usbDevices = devices.Where(d => d.ConnectionType == DeviceConnectionType.USB).ToList();

            if (usbDevices.Count == 0)
            {
                logger.LogInformation("No USB devices available for performance monitoring demo");
                return;
            }

            var device = usbDevices.First();
            logger.LogInformation("Measuring performance with device {DeviceName}...", device.Name);

            using (var client = await MeshCoreClient.ConnectAsync(device, loggerFactory))
            {

                // Measure connection time
                var connectionStopwatch = Stopwatch.StartNew();
                await client.ConnectAsync();
                connectionStopwatch.Stop();

                logger.LogInformation("?? Connection established in {ConnectionTime}ms", connectionStopwatch.ElapsedMilliseconds);

                // Measure multiple operations for performance baseline
                var operations = new Dictionary<string, Func<Task>>
                {
                    ["GetDeviceInfo"] = async () => await client.GetDeviceInfoAsync(),
                    ["GetDeviceTime"] = async () => await client.GetDeviceTimeAsync(),
                    ["GetNetworkStatus"] = async () => await client.GetNetworkStatusAsync(),
                    ["GetConfiguration"] = async () => await client.GetBatteryAndStorageAsync()
                };

                var performanceResults = new Dictionary<string, List<long>>();

                logger.LogInformation("Running performance baseline (5 iterations per operation)...");

                foreach (var (operationName, operation) in operations)
                {
                    var times = new List<long>();

                    for (int i = 0; i < 5; i++)
                    {
                        var sw = Stopwatch.StartNew();
                        try
                        {
                            await operation();
                            sw.Stop();
                            times.Add(sw.ElapsedMilliseconds);
                        }
                        catch (Exception ex)
                        {
                            sw.Stop();
                            logger.LogWarning(ex, "Operation {OperationName} failed on iteration {Iteration}", operationName, i + 1);
                        }
                    }

                    performanceResults[operationName] = times;
                }

                // Report performance statistics
                logger.LogInformation("?? Performance Results:");
                foreach (var (operationName, times) in performanceResults)
                {
                    if (times.Count > 0)
                    {
                        var avgTime = times.Average();
                        var minTime = times.Min();
                        var maxTime = times.Max();

                        logger.LogInformation("  {OperationName}: Avg={AvgTime:F1}ms, Min={MinTime}ms, Max={MaxTime}ms",
                            operationName, avgTime, minTime, maxTime);
                    }
                }

                logger.LogInformation("?? Performance Tips:");
                logger.LogInformation("  ? SDK operations are async and non-blocking");
                logger.LogInformation("  ? Connection reuse is recommended for multiple operations");
                logger.LogInformation("  ? ETW events can be used for external performance monitoring");
                logger.LogInformation("  ? Structured logging provides operation context and timing");
            }

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Performance monitoring demonstration failed");
        }

        logger.LogInformation("?? === END PERFORMANCE MONITORING ===\n");
    }
}