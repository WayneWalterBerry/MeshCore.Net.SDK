// <copyright file="LiveRadioTestBase.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MeshCore.Net.SDK.Models;
using MeshCore.Net.SDK.Tests.Logging;
using MeshCore.Net.SDK.Transport;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using Xunit.Abstractions;

namespace MeshCore.Net.SDK.Tests.LiveRadio;

/// <summary>
/// Base class for all LiveRadio integration test classes that use real MeshCore devices
/// Provides common infrastructure for device state management, connection handling, and test tracking
/// These tests require a physical MeshCore device connected to COM3
/// </summary>
public abstract class LiveRadioTestBase : IDisposable
{
    private static readonly SemaphoreSlim _clientSemaphore = new(1, 1);

    #region Protected Fields

    /// <summary>
    /// Test output helper for writing test diagnostics
    /// </summary>
    protected readonly ITestOutputHelper _output;

    /// <summary>
    /// Logger instance for the specific test class
    /// </summary>
    protected readonly ILogger _logger;

    /// <summary>
    /// Logger factory for creating SDK loggers
    /// </summary>
    protected readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// ETW event listener for SDK diagnostics (internal accessibility)
    /// </summary>
    private readonly TestEtwEventListener _etwListener;

    /// <summary>
    /// Name of the currently executing test method
    /// </summary>
    protected readonly string _testMethodName;

    /// <summary>
    /// Display name for this test suite (overridden by derived classes)
    /// </summary>
    protected abstract string TestSuiteName { get; }

    #endregion

    #region Shared Client Management (Static)

    /// <summary>
    /// Shared ETW event listener to avoid conflicts
    /// </summary>
    private static TestEtwEventListener? _sharedEtwListener;

    /// <summary>
    /// Lock object for thread-safe ETW listener initialization
    /// </summary>
    private static readonly object _etwLock = new object();

    #endregion

    #region Enhanced State Tracking (Static)

    /// <summary>
    /// Counter for tracking test execution order across all test classes
    /// </summary>
    private static int _testExecutionCounter = 0;

    /// <summary>
    /// List of test execution order for diagnostics
    /// </summary>
    private static readonly List<string> _testExecutionOrder = new();

    /// <summary>
    /// Dictionary storing device states before each test method
    /// </summary>
    private static readonly Dictionary<string, DeviceStateSnapshot> _preTestStates = new();

    /// <summary>
    /// Dictionary storing device states after each test method
    /// </summary>
    private static readonly Dictionary<string, DeviceStateSnapshot> _postTestStates = new();

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the LiveRadioTestBase class
    /// </summary>
    /// <param name="output">Test output helper for writing diagnostics</param>
    /// <param name="loggerType">Type of the derived test class for logger creation</param>
    protected LiveRadioTestBase(ITestOutputHelper output, Type loggerType)
    {
        _output = output;

        // Create a logger that writes through xUnit so ETW events are visible in test output.
        _loggerFactory = new TestOutputLoggerFactory(output);
        _logger = _loggerFactory.CreateLogger(loggerType.FullName ?? loggerType.Name);

        // Use shared ETW listener to avoid conflicts
        lock (_etwLock)
        {
            if (_sharedEtwListener == null)
            {
                _sharedEtwListener = new TestEtwEventListener(_logger);
            }

            _etwListener = _sharedEtwListener;
        }

        // Display test suite header
        DisplayTestHeader();
    }

    /// <summary>
    /// Displays the test suite header with execution information
    /// </summary>
    private void DisplayTestHeader()
    {
        _output.WriteLine(TestSuiteName);
        _output.WriteLine(new string('=', TestSuiteName.Length));
        _output.WriteLine($"Test: {_testMethodName} (#{_testExecutionCounter})");
        _output.WriteLine($"Execution Order: {string.Join(" → ", _testExecutionOrder.TakeLast(3))}");

        // Add any suite-specific header information
        DisplayAdditionalHeader();
    }

    /// <summary>
    /// Virtual method for derived classes to add suite-specific header information
    /// </summary>
    protected virtual void DisplayAdditionalHeader()
    {
        // Override in derived classes to add specific information
    }

    #endregion

    #region Enhanced State Management

    /// <summary>
    /// Logs device state in a readable format
    /// </summary>
    /// <param name="deviceInfo">Device state snapshot to log</param>
    /// <param name="prefix">Optional prefix for indentation</param>
    protected void LogDeviceState(DeviceInfo? deviceInfo, string prefix = "")
    {
        _output.WriteLine($"{prefix}   {deviceInfo}");
    }

    #endregion

    #region Connection Management

    /// <summary>
    /// Gets the stabilization delay for device state clearing
    /// Override in derived classes if different timing is needed
    /// </summary>
    /// <returns>Delay in milliseconds</returns>
    protected virtual int GetStabilizationDelay()
    {
        return 1000; // Default 1 second
    }

    #endregion

    #region Common Test Patterns

    protected async Task ExecuteStandardTestAsync(string testName, Func<Task> testAction, bool requiresConnection = true)
    {
        _output.WriteLine("");
        _output.WriteLine($"{testName}");
        _output.WriteLine(new string('=', testName.Length + 6));

        await _clientSemaphore.WaitAsync();

        try
        {
            await testAction();
        }
        finally
        {
            _clientSemaphore.Release();
        }
    }

    protected async Task ExecuteIsolationTestAsync(string testName, Func<MeshCoreClient, Task> testAction)
    {
        await ExecuteStandardTestAsync(testName, async () =>
        {
            var transport = new UsbTransport("COM3", loggerFactory: _loggerFactory);

            using (MeshCoreClient client = await MeshCoreClient.ConnectAsync(transport, _loggerFactory))
            {
                await testAction(client);
            }
        });
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Generates a test node ID for contact testing
    /// </summary>
    /// <returns>Random hex string for testing</returns>
    protected static ContactPublicKey GeneratePublicKey()
    {
        var random = new Random();
        var bytes = new byte[16];
        random.NextBytes(bytes);
        return new ContactPublicKey(bytes);
    }

    /// <summary>
    /// Gets test execution order information for diagnostics
    /// </summary>
    /// <returns>Test execution summary</returns>
    protected string GetExecutionSummary()
    {
        return $"Total Tests: {_testExecutionOrder.Count}, Order: {string.Join(" → ", _testExecutionOrder)}";
    }

    #endregion

    #region Disposal

    /// <summary>
    /// Disposes of test resources and provides cleanup diagnostics
    /// </summary>
    public virtual void Dispose()
    {
        try
        {
            _output.WriteLine("");
            _output.WriteLine("🧹 CLEANUP & FINAL DIAGNOSTICS");
            _output.WriteLine("==============================");
            _output.WriteLine($"Test: {_testMethodName} completing");

            // Show test execution summary
            _output.WriteLine($"📊 Test Execution Summary:");
            _output.WriteLine($"   Total Tests Run: {_testExecutionOrder.Count}");
            _output.WriteLine($"   Execution Order: {string.Join(" → ", _testExecutionOrder)}");

            _output.WriteLine($"✅ {_testMethodName} cleanup completed");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"⚠️  Warning during cleanup: {ex.Message}");
        }
    }

    #endregion
}