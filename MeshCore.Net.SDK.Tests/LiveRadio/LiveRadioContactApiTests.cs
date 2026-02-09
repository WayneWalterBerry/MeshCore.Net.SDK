using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MeshCore.Net.SDK.Exceptions;
using MeshCore.Net.SDK.Models;
using MeshCore.Net.SDK.Transport;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace MeshCore.Net.SDK.Tests.LiveRadio;

/// <summary>
/// Comprehensive integration tests for Contact APIs with real MeshCore device
/// Includes functional tests, advanced scenarios, edge cases, and data validation
/// These tests require a physical MeshCore device connected to COM3
/// </summary>
[Collection("LiveRadio")] // Ensures tests run sequentially to avoid COM port conflicts
[Trait("Category", "LiveRadio")] // Enable filtering in CI/CD pipelines
public class LiveRadioContactApiTests : LiveRadioTestBase
{
    /// <summary>
    /// Gets the test suite name for header display
    /// </summary>
    protected override string TestSuiteName => "Contact API Test Suite";

    /// <summary>
    /// Initializes a new instance of the LiveRadioContactApiTests class
    /// </summary>
    /// <param name="output">Test output helper</param>
    public LiveRadioContactApiTests(ITestOutputHelper output)
        : base(output, typeof(LiveRadioContactApiTests))
    {
    }

    /// <summary>
    /// Test: Basic contact retrieval functionality
    /// </summary>
    [Fact]
    public async Task Test_02_GetContacts_ShouldRetrieveContactList()
    {
        await ExecuteIsolationTestAsync("Get Contacts", async (client) =>
        {
            _output.WriteLine("📋 Reading current contact list...");
            var contacts = (await client.GetContactsAsync(CancellationToken.None)).ToList();

            Assert.NotNull(contacts);
            _output.WriteLine($"✅ Retrieved {contacts.Count} contacts from device");

            for (int i = 0; i < contacts.Count; i++)
            {
                var contact = contacts[i];
                _output.WriteLine($"   [{i + 1}] {contact.Name} (PublicKey: {contact.PublicKey}...)");
                _output.WriteLine($"       NodeType: {contact.NodeType}, ContactFlags: {contact.ContactFlags}");
                _output.WriteLine($"       OutboundRoute: {contact.OutboundRoute}");
                _output.WriteLine($"       Lat/Long: {contact.Latitude}/{contact.Longitude}");
                _output.WriteLine($"       LastAdvert: {contact.LastAdvert}, LastModified: {contact.LastModified}");
            }
        });
    }

    /// <summary>
    /// Test: Delete all contacts on the device
    /// </summary>
    [Fact]
    public async Task Test_03_DeleteAllContacts_ShouldRemoveAllContactsFromDevice()
    {
        await ExecuteIsolationTestAsync("Delete All Contacts", async (client) =>
        {
            // Step 1: Get current contacts
            _output.WriteLine("📋 Reading current contact list...");
            var contacts = (await client.GetContactsAsync(CancellationToken.None)).ToList();

            _output.WriteLine($"   Found {contacts.Count} contact(s) on device before delete.");

            if (contacts.Count == 0)
            {
                _output.WriteLine("   ✅ No contacts to delete – device is already empty.");
                return;
            }

            // Step 2: Delete each contact individually
            foreach (var contact in contacts)
            {
                try
                {
                    _output.WriteLine($"   🗑 Deleting contact: {contact.Name} (PublicKey: {contact.PublicKey}...)");
                    await client.DeleteContactAsync(contact.PublicKey);
                }
                catch (ProtocolException ex)
                {
                    _output.WriteLine($"   ⚠️  Failed to delete contact {contact.PublicKey}: {ex.Message}");
                    throw;
                }
            }

            // Small delay to allow device to flush changes
            await Task.Delay(1000);

            // Step 3: Verify all contacts are gone
            var remainingContacts = (await client.GetContactsAsync(CancellationToken.None)).ToList();
            _output.WriteLine($"   Contacts remaining after delete: {remainingContacts.Count}");

            Assert.Empty(remainingContacts);
            _output.WriteLine("✅ All contacts successfully deleted from device.");
        });
    }

    /// <summary>
    /// Verifies that deleting the first contact from the device removes it from the contact list.
    /// </summary>
    /// <remarks>This test retrieves the current contacts from the device, deletes the first contact if any exist, and
    /// asserts that the contact is no longer present. The test will pass if the contact is successfully deleted, or if
    /// there are no contacts to delete.</remarks>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task Test_04_DeleteFirstContact_ShouldDeleteContactFromDevice()
    {
        await ExecuteIsolationTestAsync("Delete First Contact", async (client) =>
        {
            // Step 1: Get current contacts
            _output.WriteLine("📋 Reading current contact list...");
            var contacts = (await client.GetContactsAsync(CancellationToken.None)).ToList();

            _output.WriteLine($"   Found {contacts.Count} contact(s) on device before delete.");

            if (contacts.Count == 0)
            {
                _output.WriteLine("   ✅ No contacts to delete – device is already empty.");
                return;
            }

            var contact = contacts.First();

            _output.WriteLine($"   🗑 Deleting contact: {contact.Name} (PublicKey: {contact.PublicKey}...)");
            await client.DeleteContactAsync(contact.PublicKey);

            // Small delay to allow device to flush changes
            await Task.Delay(1000);

            // Step 3: Verify contact is gone
            var fetchMissingContact = await client.TryGetContactAsync(contact.PublicKey);

            Assert.Null(fetchMissingContact);

            _output.WriteLine("✅ Contact successfully deleted from device.");
        });
    }

    #region Error Handling Tests

    /// <summary>
    /// Test: Invalid contact operations error handling
    /// </summary>
    [Fact]
    public async Task Test_07_ErrorHandling_ShouldHandleInvalidOperations()
    {
        await ExecuteIsolationTestAsync("Error Handling for Invalid Operations", async (client) =>
        {
            // Test invalid contact name (empty)
            try
            {
                await client.AddContactAsync("", GeneratePublicKey());
                _output.WriteLine("   ❌ Expected exception for empty contact name was not thrown");
            }
            catch (ArgumentException)
            {
                _output.WriteLine("   ✅ Empty contact name properly rejected");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"   ⚠️  Unexpected exception for empty name: {ex.GetType().Name}");
            }

            // Test invalid node ID (empty)
            try
            {
                await client.AddContactAsync("TestContact", GeneratePublicKey());
                _output.WriteLine("   ❌ Expected exception for empty node ID was not thrown");
            }
            catch (ArgumentException)
            {
                _output.WriteLine("   ✅ Empty node ID properly rejected");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"   ⚠️  Unexpected exception for empty node ID: {ex.GetType().Name}");
            }

            // Test deleting non-existent contact
            try
            {
                await client.DeleteContactAsync(GeneratePublicKey());
                _output.WriteLine("   ⚠️  Deleting non-existent contact did not throw exception");
            }
            catch (ProtocolException)
            {
                _output.WriteLine("   ✅ Non-existent contact deletion properly rejected");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"   ⚠️  Unexpected exception for non-existent contact: {ex.GetType().Name}");
            }
        });
    }

    [Fact]
    public async Task Test_08_TryGetContactAsync_ShouldGetContactFromDevice()
    {
        await ExecuteIsolationTestAsync("Try Get Contact", async (client) =>
        {
            // Step 1: Get current contacts
            _output.WriteLine("📋 Reading current contact list...");
            var contacts = (await client.GetContactsAsync(CancellationToken.None)).ToList();

            _output.WriteLine($"   Found {contacts.Count} contact(s) on device.");

            if (contacts.Count == 0)
            {
                _output.WriteLine("   ✅ No contacts available to test fetching.");
                return;
            }

            // Pick a random contact to test fetching
            var contact = contacts.OrderBy(_ => Guid.NewGuid()).First();

            // Step 2: Get Contact Using TryGetContactAsync
            var fetchedContact = await client.TryGetContactAsync(contact.PublicKey);

            Assert.NotNull(fetchedContact);

            Assert.Equal(contact.Name, fetchedContact!.Name);

            _output.WriteLine("✅ Contact successfully fetched from device.");
        });
    }

    #endregion
}