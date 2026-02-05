using System.Threading.Tasks;
using MeshCore.Net.SDK.Models;
using Xunit;
using Xunit.Abstractions;

namespace MeshCore.Net.SDK.Tests.LiveRadio;

/// <summary>
/// Live radio integration tests for self advertisement operations using a real MeshCore device.
/// </summary>
[Collection("SequentialTests")] // Match other live radio tests to avoid COM port conflicts
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
    /// Verifies that <see cref="MeshCodeClient.SendSelfAdvertAsync(Advertisement)"/> succeeds
    /// when issuing a basic zero-hop self advertisement.
    /// </summary>
    [Fact]
    public async Task Test_01_SendSelfAdvertAsync_ShouldSucceed_ForZeroHopAdvert()
    {
        await ExecuteStandardTest("Zero-hop self advertisement", async () =>
        {
            // Arrange
            await EnsureConnected();

            // Act - no exception means success
            await SharedClient!.SendSelfAdvertZeroHopAsync();
        });
    }

    /// <summary>
    /// Verifies that <see cref="MeshCodeClient.SendSelfAdvertAsync(Advertisement)"/> succeeds
    /// when issuing a flood-mode self advertisement.
    /// </summary>
    [Fact]
    public async Task Test_02_SendSelfAdvertAsync_ShouldSucceed_ForFloodAdvert()
    {
        await ExecuteStandardTest("Flood-mode self advertisement", async () =>
        {
            // Arrange
            await EnsureConnected();

            // Act - no exception means success
            await SharedClient!.SendSelfAdvertFloodAsync();
        });
    }

    /// <summary>
    /// Executes an integration test that verifies advert path retrieval for all contacts using the shared client.
    /// </summary>
    /// <remarks>This test ensures that the GetAdvertPathAsync method can be called for each contact and
    /// completes successfully, regardless of whether a path is found. The test writes output for each contact with a
    /// valid advert path.</remarks>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if a contact does not have a public key.</exception>
    [Fact]
    public async Task Test_03_GetAdvertPathSync_WalkContacts()
    {
        await ExecuteStandardTest("Get advert paths for all contacts", async () =>
        {
            // Arrange
            await EnsureConnected();

            var contacts = (await SharedClient!.GetContactsAsync(CancellationToken.None)).ToList();

            _output.WriteLine("Found {0} contacts to check advert paths for.", contacts.Count());

            foreach (var contact in contacts)
            {
                // Act
                AdvertPathInfo? advertPath = await SharedClient.TryGetAdvertPathAsync(contact.PublicKey);

                // Assert – it is valid for there to be no known path yet; the main check is that the call succeeds.
                _output.WriteLine(
                    "GetAdvertPathAsync completed for contact '{0}' (PublicKey {1}). Path length: {2}",
                    contact.Name,
                    contact.PublicKey,
                    advertPath?.Path.Length ?? 0);
            }
        });
    }

    /// <summary>
    /// Verifies that the device advert name can be changed using
    /// <see cref="MeshCodeClient.SetAdvertNameAsync(string, System.Threading.CancellationToken)"/>.
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

        await ExecuteStandardTest("Set advert name", async () =>
        {
            // Arrange
            await EnsureConnected();

            _output.WriteLine("Setting advert name to: {0}", newAdvertName);

            // Act – no exception indicates that the firmware accepted the new name.
            await SharedClient!.SetAdvertNameAsync(newAdvertName, CancellationToken.None);

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
