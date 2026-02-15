// <copyright file="LiveRadioCampaignTests.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Tests.LiveRadio
{
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// Live radio tests for MeshCoreCampaign functionality including
    /// periodic queue synchronization, channel caching, and message caching.
    ///
    /// Requirements:
    /// - Physical MeshCore device connected to COM3
    /// - Device should be within radio range of the mesh network
    /// </summary>
    [Collection("LiveRadio")]
    [Trait("Category", "LiveRadio")]
    public class LiveRadioCampaignTests : LiveRadioTestBase
    {
        /// <summary>
        /// Gets the test suite name for header display
        /// </summary>
        protected override string TestSuiteName => "MeshCore Live Radio Campaign Test Suite";

        /// <summary>
        /// Initializes a new instance of the <see cref="LiveRadioCampaignTests"/> class
        /// </summary>
        /// <param name="output">Test output helper</param>
        public LiveRadioCampaignTests(ITestOutputHelper output)
            : base(output, typeof(LiveRadioCampaignTests))
        {
        }
    }
}
