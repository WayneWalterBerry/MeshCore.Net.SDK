using System.Threading.Tasks;
using MeshCore.Net.SDK.Models;
using Xunit;
using Xunit.Abstractions;

namespace MeshCore.Net.SDK.Tests.LiveRadio;

/// <summary>
/// Live radio integration tests for self advertisement operations using a real MeshCore device.
/// </summary>
[Collection("LiveRadio")] // Match other live radio tests to avoid COM port conflicts
[Trait("Category", "LiveRadio")] // Enable filtering in CI/CD pipelines
public class LiveRadioAdvertTests : LiveRadioTestBase
{
    /// <summary>
    /// Gets the test suite name for header display.
    /// </summary>
    protected override string TestSuiteName => "MeshCore Live Radio Advertisement Test Suite";

    /// <summary>
    /// Initializes a new instance of the <see cref="LiveRadioAdvertTests"/> class.
    /// </summary>
    /// <param name="output">The test output helper.</param>
    public LiveRadioAdvertTests(ITestOutputHelper output)
        : base(output, typeof(LiveRadioAdvertTests))
    {
    }

    /// <summary>
    /// Verifies that <see cref="MeshCoreClient.SendSelfAdvertAsync(Advertisement)"/> succeeds
    /// when issuing a basic zero-hop self advertisement.
    /// </summary>
    [Fact]
    public async Task Test_01_SendSelfAdvertAsync_ShouldSucceed_ForZeroHopAdvert()
    {
        await ExecuteIsolationTestAsync("Zero-hop self advertisement", async (client) =>
        {
            // Act - no exception means success
            await client.SendSelfAdvertZeroHopAsync();
        });
    }

    /// <summary>
    /// Verifies that <see cref="MeshCoreClient.SendSelfAdvertAsync(Advertisement)"/> succeeds
    /// when issuing a flood-mode self advertisement.
    /// </summary>
    [Fact]
    public async Task Test_02_SendSelfAdvertAsync_ShouldSucceed_ForFloodAdvert()
    {
        await ExecuteIsolationTestAsync("Flood-mode self advertisement", async (client) =>
        {
            // Act - no exception means success
            await client.SendSelfAdvertFloodAsync();
        });
    }

    /// <summary>
    /// Executes an integration test that verifies advert path retrieval for all contacts using an isolated client.
    /// </summary>
    /// <remarks>This test ensures that the GetAdvertPathAsync method can be called for each contact and
    /// completes successfully, regardless of whether a path is found. The test writes output for each contact with a
    /// valid advert path.</remarks>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if a contact does not have a public key.</exception>
    [Fact]
    public async Task Test_03_GetAdvertPathSync_WalkContacts()
    {
        List<Contact> contacts = new List<Contact>();

        await ExecuteIsolationTestAsync("GET ALL CONTACTS", async (client) =>
        {
            contacts = (await client.GetContactsAsync(CancellationToken.None)).ToList();

            _output.WriteLine("Found {0} contacts to check advert paths for.", contacts.Count());
        }, enableLogging: false);

        List<(Contact contact, AdvertPathInfo pathInfo)> validAdvertPaths = new List<(Contact, AdvertPathInfo)>();
        await ExecuteIsolationTestAsync("GET ADVERT PATHES", async (client) =>
        {
            foreach (var contact in contacts)
            {
                // Act
                AdvertPathInfo? advertPath = await client.TryGetAdvertPathAsync(contact.PublicKey);

                // Assert – it is valid for there to be no known path yet; the main check is that the call succeeds.
                _output.WriteLine($"TryGetAdvertPathAsync() completed for contact '{contact.Name}' (PublicKey {contact.PublicKey}). {advertPath}");

                if (advertPath != null)
                {
                    validAdvertPaths.Add((contact, advertPath));
                }
            }
        });

        _output.WriteLine("Total valid advert paths found: {0}", validAdvertPaths.Count);

        // Output table of advertisement paths
        if (validAdvertPaths.Count > 0)
        {
            // Calculate column widths for better formatting
            var maxNameWidth = Math.Max("Contact Name".Length,
                validAdvertPaths.Max(p => p.contact.Name.Length)) + 1;
            var maxPathWidth = Math.Max("Advertisement Path".Length,
                validAdvertPaths.Max(p => Convert.ToBase64String(p.pathInfo.Path).Length + 2)); // +2 for backticks
            var hopsWidth = "Hops".Length;
            var timestampWidth = "Received Timestamp".Length + 2;

            _output.WriteLine("");
            _output.WriteLine("## Contacts with Advertisement Paths");
            _output.WriteLine("");

            // Header with proper padding
            _output.WriteLine("| {0} | {1} | {2} | {3} |",
                "Contact Name".PadRight(maxNameWidth),
                "Hops".PadRight(hopsWidth),
                "Advertisement Path".PadRight(maxPathWidth),
                "Received Timestamp".PadRight(timestampWidth));

            _output.WriteLine("|{0}|{1}|{2}|{3}|",
                new string('-', maxNameWidth + 2),
                new string('-', hopsWidth + 2),
                new string('-', maxPathWidth + 2),
                new string('-', timestampWidth + 2));

            foreach (var (contact, pathInfo) in validAdvertPaths.OrderBy(p => p.pathInfo.Path.Length))
            {
                var pathStr = $"`{Convert.ToBase64String(pathInfo.Path)}`";
                var timestampStr = pathInfo.ReceivedTimestamp.ToString("yyyy-MM-ddTHH:mm:ssZ") ?? "Unknown";

                _output.WriteLine("| {0} | {1} | {2} | {3} |",
                    contact.Name.PadRight(maxNameWidth),
                    pathInfo.Path.Length.ToString().PadRight(hopsWidth),
                    pathStr.PadRight(maxPathWidth),
                    timestampStr.PadRight(timestampWidth));
            }

            _output.WriteLine("");
            _output.WriteLine("## Summary Statistics");
            _output.WriteLine("");
            _output.WriteLine("- **Total contacts tested**: {0}", contacts.Count);
            _output.WriteLine("- **Contacts with advertisement paths**: {0}", validAdvertPaths.Count);
            _output.WriteLine("- **Contacts without advertisement paths**: {0}", contacts.Count - validAdvertPaths.Count);
            _output.WriteLine("- **Hop count range**: {0}-{1} hops",
                validAdvertPaths.Min(p => p.pathInfo.Path.Length),
                validAdvertPaths.Max(p => p.pathInfo.Path.Length));

            var hopCounts = validAdvertPaths.GroupBy(p => p.pathInfo.Path.Length).OrderByDescending(g => g.Count()).First();
            _output.WriteLine("- **Most common hop count**: {0} hops ({1} contacts)",
                hopCounts.Key, hopCounts.Count());

            var shortestPath = validAdvertPaths.OrderBy(p => p.pathInfo.Path.Length).First();
            var longestPath = validAdvertPaths.OrderByDescending(p => p.pathInfo.Path.Length).First();
            _output.WriteLine("- **Shortest path**: {0} ({1} hops)",
                shortestPath.contact.Name, shortestPath.pathInfo.Path.Length);
            _output.WriteLine("- **Longest path**: {0} ({1} hops)",
                longestPath.contact.Name, longestPath.pathInfo.Path.Length);
        }
        else
        {
            _output.WriteLine("No advertisement paths found for any contacts.");
        }
    }

    /// <summary>
    /// Verifies that the device advert name can be changed using
    /// <see cref="MeshCoreClient.SetAdvertNameAsync(string, System.Threading.CancellationToken)"/>.
    /// </summary>
    /// <remarks>
    /// This test sets a known advert name on the connected device and relies on the absence
    /// of exceptions to validate success. The new name is written to the test output for
    /// manual verification when needed.
    /// </remarks>
    [Fact]
    public async Task Test_04_SetAdvertName()
    {
        const string newAdvertName = "MeshCore.Net.SDK";

        await ExecuteIsolationTestAsync("Set advert name", async (client) =>
        {
            _output.WriteLine("Setting advert name to: {0}", newAdvertName);

            // Act – no exception indicates that the firmware accepted the new name.
            await client.SetAdvertNameAsync(newAdvertName, CancellationToken.None);

            _output.WriteLine("Advert name set successfully.");
        });
    }

    /// <summary>
    /// Allows derived classes to add suite-specific header information.
    /// </summary>
    protected override void DisplayAdditionalHeader()
    {
        _output.WriteLine("Advertisement tests will send zero-hop and flood-mode self adverts.");
    }
}
