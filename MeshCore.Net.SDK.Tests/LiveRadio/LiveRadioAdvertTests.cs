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

            var contacts = await SharedClient!.GetContactsAsync();

            _output.WriteLine("Found {0} contacts to check advert paths for.", contacts.Count);

            foreach (var Contact in contacts)
            {
                var pubKeyPrefix = Contact.PublicKeyPrefix;
                Assert.NotNull(pubKeyPrefix ?? throw new InvalidOperationException("No PublicKey found for contact."));

                // Act
                AdvertPathInfo? advertPath = await SharedClient.TryGetAdvertPathAsync(pubKeyPrefix);

                if (advertPath != null)
                {
                    // Assert – it is valid for there to be no known path yet; the main check is that the call succeeds.
                    _output.WriteLine(
                        "GetAdvertPathAsync completed for contact '{0}' (PublicKey {1}). Path length: {2}",
                        Contact.Name,
                        Convert.ToHexString(Contact.PublicKey),
                        advertPath?.Path.Length ?? 0);
                }
            }
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
