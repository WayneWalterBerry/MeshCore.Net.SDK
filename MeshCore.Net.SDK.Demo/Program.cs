using MeshCore.Net.SDK.Demo.Demos;
using MeshCore.Net.SDK.Transport;

namespace MeshCore.Net.SDK.Demo;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("MeshCore.Net.SDK Demo Application");
        Console.WriteLine("====================================");
        Console.WriteLine();

        var config = ParseArguments(args);
        DisplayConfiguration(config);

        try
        {
            if (config.IsAdvanced)
            {
                Console.WriteLine("Running Advanced Demo...");
                await AdvancedDemo.RunAsync(config.PreferredTransport);
            }
            else
            {
                Console.WriteLine("Running Basic Demo...");
                await BasicDemo.RunAsync(config.PreferredTransport);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fatal Error: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"   Inner Exception: {ex.InnerException.Message}");
            }
        }

        Console.WriteLine();
        if (config.NoWait)
        {
            Console.WriteLine("Demo completed.");
        }
        else
        {
            Console.WriteLine("Demo completed. Press any key to exit...");
            Console.ReadKey();
        }
    }

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

    private static void DisplayConfiguration(DemoConfiguration config)
    {
        Console.WriteLine("Demo Configuration:");
        Console.WriteLine($"   Mode: {(config.IsAdvanced ? "Advanced" : "Basic")}");
        
        if (config.TransportSpecified)
        {
            Console.WriteLine($"   Preferred Transport: {config.PreferredTransport}");
        }
        else
        {
            Console.WriteLine($"   Transport: Auto-detect (USB preferred)");
        }

        if (config.NoWait)
        {
            Console.WriteLine($"   Exit behavior: Auto-exit (no wait)");
        }
        
        Console.WriteLine();
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
        Console.WriteLine("  --help, -h          Show this help message");
        Console.WriteLine();
        Console.WriteLine("EXAMPLES:");
        Console.WriteLine("  # Basic demo with auto-detected transport");
        Console.WriteLine("  MeshCore.Net.SDK.Demo");
        Console.WriteLine();
        Console.WriteLine("  # Advanced demo with USB transport");
        Console.WriteLine("  MeshCore.Net.SDK.Demo --advanced --usb");
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
    }
}