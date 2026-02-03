using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MeshCore.Net.SDK.Demo.Demos;
using MeshCore.Net.SDK.Demo.Logging;
using MeshCore.Net.SDK.Transport;

namespace MeshCore.Net.SDK.Demo;

class Program
{
    static async Task Main(string[] args)
    {
        var config = ParseArguments(args);
        
        // Build the host with dependency injection and logging
        var host = CreateHostBuilder(args, config).Build();
        
        // Get the logger factory and ETW listener from DI
        var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        var etwListener = host.Services.GetRequiredService<MeshCoreSdkEventListener>();

        logger.LogInformation("MeshCore.Net.SDK Demo Application Started");
        logger.LogInformation("====================================");

        DisplayConfiguration(config, logger);

        try
        {
            if (config.IsAdvanced)
            {
                logger.LogInformation("Running Advanced Demo...");
                await AdvancedDemo.RunAsync(config.PreferredTransport, loggerFactory);
            }
            else
            {
                logger.LogInformation("Running Basic Demo...");
                await BasicDemo.RunAsync(config.PreferredTransport, loggerFactory);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fatal Error occurred during demo execution");
            if (ex.InnerException != null)
            {
                logger.LogError(ex.InnerException, "Inner Exception details");
            }
        }

        if (config.NoWait)
        {
            logger.LogInformation("Demo completed.");
        }
        else
        {
            logger.LogInformation("Demo completed. Press any key to exit...");
            Console.ReadKey();
        }

        // Dispose of the ETW listener
        etwListener.Dispose();
        
        await host.StopAsync();
    }

    private static IHostBuilder CreateHostBuilder(string[] args, DemoConfiguration config) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureLogging((context, logging) =>
            {
                // Clear default providers and add what we want
                logging.ClearProviders();
                
                // Add console logging with custom formatting
                logging.AddConsole(options =>
                {
                    options.FormatterName = "simple";
                });
                
                // Add Event Log logging on Windows
                if (OperatingSystem.IsWindows())
                {
                    try
                    {
                        logging.AddEventLog(options =>
                        {
                            options.SourceName = "MeshCore.Net.SDK.Demo";
                            options.LogName = "Application";
                        });
                    }
                    catch
                    {
                        // Event Log might not be available in all scenarios
                    }
                }
                
                // Set minimum log levels based on verbose mode
                if (config.Verbose)
                {
                    logging.SetMinimumLevel(LogLevel.Trace);
                }
                else
                {
                    logging.SetMinimumLevel(LogLevel.Debug);
                }
                
                // Configure log levels for different categories
                logging.AddFilter("Microsoft", LogLevel.Warning);
                logging.AddFilter("System", LogLevel.Warning);
                
                // Enable detailed logging for MeshCore SDK when verbose mode is enabled
                if (config.Verbose)
                {
                    logging.AddFilter("MeshCore.Net.SDK", LogLevel.Debug);
                    logging.AddFilter("MeshCore.Net.SDK.Demo", LogLevel.Debug);
                }
                else
                {
                    logging.AddFilter("MeshCore.Net.SDK", LogLevel.Information);
                    logging.AddFilter("MeshCore.Net.SDK.Demo", LogLevel.Information);
                }
            })
            .ConfigureServices((context, services) =>
            {
                // Register the ETW event listener as a singleton
                services.AddSingleton<MeshCoreSdkEventListener>();
            });

    private static DemoConfiguration ParseArguments(string[] args)
    {
        var config = new DemoConfiguration();

        foreach (var arg in args)
        {
            switch (arg.ToLowerInvariant())
            {
                case "--advanced":
                    config.IsAdvanced = true;
                    break;
                case "--usb":
                    config.PreferredTransport = DeviceConnectionType.USB;
                    config.TransportSpecified = true;
                    break;
                case "--bluetooth":
                case "--ble":
                    config.PreferredTransport = DeviceConnectionType.BluetoothLE;
                    config.TransportSpecified = true;
                    break;
                case "--no-wait":
                    config.NoWait = true;
                    break;
                case "--verbose":
                case "-v":
                    config.Verbose = true;
                    break;
                case "--help":
                case "-h":
                    ShowHelp();
                    Environment.Exit(0);
                    break;
                default:
                    if (arg.StartsWith("--"))
                    {
                        Console.WriteLine($"Unknown argument: {arg}");
                        Console.WriteLine("Use --help to see available options.");
                    }
                    break;
            }
        }

        return config;
    }

    private static void DisplayConfiguration(DemoConfiguration config, ILogger logger)
    {
        logger.LogInformation("Demo Configuration:");
        logger.LogInformation("   Mode: {Mode}", config.IsAdvanced ? "Advanced" : "Basic");
        
        if (config.TransportSpecified)
        {
            logger.LogInformation("   Preferred Transport: {Transport}", config.PreferredTransport);
        }
        else
        {
            logger.LogInformation("   Transport: Auto-detect (USB preferred)");
        }

        if (config.NoWait)
        {
            logger.LogInformation("   Exit behavior: Auto-exit (no wait)");
        }
        
        if (config.Verbose)
        {
            logger.LogInformation("   Logging: Verbose mode enabled");
        }
    }

    private static void ShowHelp()
    {
        Console.WriteLine("MeshCore.Net.SDK Demo Application");
        Console.WriteLine("====================================");
        Console.WriteLine();
        Console.WriteLine("USAGE:");
        Console.WriteLine("  MeshCore.Net.SDK.Demo [options]");
        Console.WriteLine();
        Console.WriteLine("OPTIONS:");
        Console.WriteLine("  --advanced          Run advanced demo with architecture showcase");
        Console.WriteLine("  --usb               Prefer USB transport (default)");
        Console.WriteLine("  --bluetooth, --ble  Prefer Bluetooth LE transport");
        Console.WriteLine("  --no-wait           Exit immediately without waiting for keypress");
        Console.WriteLine("  --verbose, -v       Enable verbose logging output");
        Console.WriteLine("  --help, -h          Show this help message");
        Console.WriteLine();
        Console.WriteLine("EXAMPLES:");
        Console.WriteLine("  # Basic demo with auto-detected transport");
        Console.WriteLine("  MeshCore.Net.SDK.Demo");
        Console.WriteLine();
        Console.WriteLine("  # Advanced demo with USB transport and verbose logging");
        Console.WriteLine("  MeshCore.Net.SDK.Demo --advanced --usb --verbose");
        Console.WriteLine();
        Console.WriteLine("  # Basic demo specifically for Bluetooth LE");
        Console.WriteLine("  MeshCore.Net.SDK.Demo --bluetooth");
        Console.WriteLine();
        Console.WriteLine("  # Advanced demo with Bluetooth LE");
        Console.WriteLine("  MeshCore.Net.SDK.Demo --advanced --ble");
        Console.WriteLine();
        Console.WriteLine("  # Basic demo with auto-exit (useful for automation)");
        Console.WriteLine("  MeshCore.Net.SDK.Demo --no-wait");
        Console.WriteLine();
        Console.WriteLine("TRANSPORT STATUS:");
        Console.WriteLine("  USB Serial:     Fully supported");
        Console.WriteLine("  Bluetooth LE:   Planned for v2.0");
        Console.WriteLine("  WiFi TCP:       Future consideration");
        Console.WriteLine();
        Console.WriteLine("LOGGING:");
        Console.WriteLine("  The demo includes ETW (Event Tracing for Windows) support.");
        Console.WriteLine("  ETW events from the SDK are captured and displayed in the console.");
        Console.WriteLine("  Use --verbose for detailed logging information.");
        Console.WriteLine("  On Windows, events are also written to the Event Log.");
        Console.WriteLine();
        Console.WriteLine("ETW MONITORING:");
        Console.WriteLine("  You can monitor ETW events externally using tools like:");
        Console.WriteLine("  - Windows Performance Toolkit (WPA)");
        Console.WriteLine("  - PerfView");
        Console.WriteLine("  - Custom ETW consumers");
        Console.WriteLine("  Event Source Name: 'MeshCore-Net-SDK'");
        Console.WriteLine();
        Console.WriteLine("NOTE:");
        Console.WriteLine("  If no transport is specified, the demo will auto-detect");
        Console.WriteLine("  available devices and prefer USB connections.");
        Console.WriteLine();
        Console.WriteLine("  The --no-wait flag is useful for automation scenarios where");
        Console.WriteLine("  the demo should exit immediately after completion.");
    }

    private class DemoConfiguration
    {
        public bool IsAdvanced { get; set; } = false;
        public DeviceConnectionType PreferredTransport { get; set; } = DeviceConnectionType.USB;
        public bool TransportSpecified { get; set; } = false;
        public bool NoWait { get; set; } = false;
        public bool Verbose { get; set; } = false;
    }
}