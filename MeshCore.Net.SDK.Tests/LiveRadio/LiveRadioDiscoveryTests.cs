// <copyright file="Advertisement.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Tests.LiveRadio
{
    using System.IO;
    using System.Threading.Tasks;
    using MeshCore.Net.SDK.Models;
    using Xunit;
    using Xunit.Abstractions;
    using Xunit.Sdk;

    /// <summary>
    /// Live radio integration tests for self advertisement operations using a real MeshCore device.
    /// </summary>
    [Collection("LiveRadio")] // Match other live radio tests to avoid COM port conflicts
    [Trait("Category", "LiveRadio")] // Enable filtering in CI/CD pipelines
    public class LiveRadioDiscoveryTests : LiveRadioTestBase
    {
        /// <summary>
        /// Gets the test suite name for header display.
        /// </summary>
        protected override string TestSuiteName => "MeshCore Live Radio Discovery Test Suite";

        /// <summary>
        /// Initializes a new instance of the <see cref="LiveRadioDiscoveryTests"/> class.
        /// </summary>
        /// <param name="output">The test output helper.</param>
        public LiveRadioDiscoveryTests(ITestOutputHelper output)
            : base(output, typeof(LiveRadioDiscoveryTests))
        {
        }

        [Fact]
        public async Task Test_01_TryDiscoverPathAsync_ShouldSucceed()
        {
            IEnumerable<Contact> contacts = Enumerable.Empty<Contact>();

            await ExecuteIsolationTestAsync("Get Contacts", async (client) =>
            {
                contacts = await client.GetContactsAsync(CancellationToken.None);
            }, enableLogging: false);

            await ExecuteIsolationTestAsync("Discover Path", async (client) =>
            {
                var contact = contacts
                    .Where(contact => contact.NodeType == NodeType.Repeater)
                    .Where(contact => contact.Name == "VE7NA - Cottle Hill").First();

                using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                {
                    // Act - no exception means success
                    var path = await client.TryDiscoverPathAsync(contact, cancellationTokenSource.Token);

                    _output.WriteLine($"✅ Path {path} for {contact} successfully fetched from device.");
                }
            });
        }

        [Fact]
        public async Task Test_02_TryGetNeighborsAsync_ShouldSucceed()
        {
            IEnumerable<Contact> contacts = Enumerable.Empty<Contact>();

            await ExecuteIsolationTestAsync("Get Contacts", async (client) =>
            {
                contacts = await client.GetContactsAsync(CancellationToken.None);
            }, enableLogging: false);

            IOrderedEnumerable<Contact> repeaters = contacts
                .Where(contact => contact.NodeType == NodeType.Repeater)
                .OrderBy(_ => Guid.NewGuid());

            await ExecuteIsolationTestAsync("Get Neighbors", async (client) =>
            {
                foreach (Contact contact in repeaters)
                {
                    // Act - Non-null means success, empty list is still a valid response
                    var neighborList = await client.TryGetNeighborsAsync(contact);
                    if (neighborList != null)
                    {
                        _output.WriteLine($"✅ Neighbors for {contact} successfully fetched from device. {neighborList}");
                    }
                    else
                    {
                        _output.WriteLine($"⚠️ No neighbors found for {contact}.");
                    }
                }
            });
        }

        /// <summary>
        /// Verifies that the TrySingleHopTraceAsync method successfully discovers a path to a repeater contact without
        /// throwing exceptions.
        /// </summary>
        /// <remarks>
        /// This test ensures that the client can retrieve contacts and perform a single-hop
        /// trace to a specific repeater node. The test passes if no exceptions are thrown during the trace
        /// operation.
        /// Example: meshcli -s COM3 -D disc_path 2f
        /// </remarks>
        /// <returns></returns>
        [Fact]
        public async Task Test_03_TrySingleHopTraceAsync_ShouldSucceed()
        {
            IEnumerable<Contact> contacts = Enumerable.Empty<Contact>();

            await ExecuteIsolationTestAsync("Get Contacts", async (client) =>
            {
                contacts = await client.GetContactsAsync(CancellationToken.None);
            }, enableLogging: false);

            var contact = contacts
                .Where(contact => contact.NodeType == NodeType.Repeater)
                .Where(contact => contact.Name == "🐋 // Samish Crest").First();

            await ExecuteIsolationTestAsync("Try Single Hop Trace", async (client) =>
            {
                // Act - no exception means success
                var path = await client.TrySingleHopTraceAsync(contact);

                _output.WriteLine($"✅ Path {path} for {contact} successfully fetched from device.");
            });
        }

        [Fact]
        public async Task Test_04_TryRequestStatusAsync_ShouldSucceed()
        {
            IEnumerable<Contact> contacts = Enumerable.Empty<Contact>();

            await ExecuteIsolationTestAsync("Single Hop Advert", async (client) =>
            {
                await client.SendSelfAdvertZeroHopAsync();
            }, enableLogging: false);

            await Task.Delay(TimeSpan.FromSeconds(20));

            await ExecuteIsolationTestAsync("Get Contacts", async (client) =>
            {
                contacts = await client.GetContactsAsync(CancellationToken.None);
            }, enableLogging: false);

            var contact = contacts
                .Where(contact => contact.NodeType == NodeType.Repeater)
                .Where(contact => contact.Name == "VE7NA - Cottle Hill").First();

            await ExecuteIsolationTestAsync("Request Status", async (client) =>
            {
                using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                {
                    // Act - Non-null means success, empty list is still a valid response
                    StatusInfo? statusInfo = await client.TryRequestStatusAsync(contact, cancellationTokenSource.Token);
                    if (statusInfo != null)
                    {
                        _output.WriteLine($"✅ Status Info for {contact} successfully fetched from device. {statusInfo}");
                    }
                    else
                    {
                        _output.WriteLine($"⚠️ No status info found for {contact}.");
                    }
                }
            });
        }
    }
}
