using Xunit;

namespace MeshCore.Net.SDK.Tests;

/// <summary>
/// Collection definition for tests that must run sequentially to avoid COM port conflicts
/// </summary>
[CollectionDefinition("SequentialTests")]
public class SequentialTestsCollection
{
    // This class has no code, and is never instantiated.
    // Its purpose is simply to be the place to apply [CollectionDefinition]
    // and all the tests that belong to this collection.
    // Tests that access COM3 or other shared resources should be in this collection.
}