using MeshCore.Net.SDK.Examples;

namespace MeshCore.Net.SDK.Demo;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("?? MeshCore.Net.SDK Demo Application");
        Console.WriteLine("====================================");
        Console.WriteLine();

        try
        {
            if (args.Length > 0 && args[0] == "--advanced")
            {
                Console.WriteLine("?? Running Advanced Example...");
                await AdvancedUsageExample.RunAdvancedExampleAsync();
            }
            else
            {
                Console.WriteLine("?? Running Basic Example...");
                await BasicUsageExample.RunExampleAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"?? Fatal Error: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"   Inner Exception: {ex.InnerException.Message}");
            }
        }

        Console.WriteLine();
        Console.WriteLine("? Demo completed. Press any key to exit...");
        Console.ReadKey();
    }
}