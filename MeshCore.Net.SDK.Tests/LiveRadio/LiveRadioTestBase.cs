// <copyright file="LiveRadioTestBase.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MeshCore.Net.SDK.Models;
using System.Diagnostics;
using Xunit.Abstractions;

namespace MeshCore.Net.SDK.Tests.LiveRadio;

/// <summary>
/// Base class for all LiveRadio integration test classes that use real MeshCore devices
/// Provides common infrastructure for device state management, connection handling, and test tracking
/// These tests require a physical MeshCore device connected to COM3
/// </summary>
public abstract class LiveRadioTestBase : IDisposable
{
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
    /// Shared MeshCore client instance across all test classes for efficiency
    /// </summary>
    private static MeshCodeClient? _sharedClient;

    /// <summary>
    /// Lock object for thread-safe client initialization
    /// </summary>
    private static readonly object _clientLock = new object();

    /// <summary>
    /// Flag indicating whether the shared client has been initialized
    /// </summary>
    private static bool _clientInitialized = false;

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

        // Create logger with the specific test class type
        _logger = NullLogger.Instance;

        // Capture the calling test method name for better tracking
        var stackTrace = new StackTrace();
        var testMethod = stackTrace.GetFrames()
            .FirstOrDefault(f => f.GetMethod()?.Name.StartsWith("Test_") == true);
        _testMethodName = testMethod?.GetMethod()?.Name ?? "Unknown";

        // Use shared ETW listener to avoid conflicts
        lock (_etwLock)
        {
            if (_sharedEtwListener == null)
            {
                _sharedEtwListener = new TestEtwEventListener(_logger);
            }
            _etwListener = _sharedEtwListener;
        }

        // Update test execution tracking
        lock (_clientLock)
        {
            _testExecutionCounter++;
            _testExecutionOrder.Add($"{_testExecutionCounter:D2}. {_testMethodName}");
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
    /// Captures the current device state for debugging purposes
    /// </summary>
    /// <param name="context">Context description for this state capture</param>
    /// <returns>Device state snapshot</returns>
    protected Task<DeviceInfo?> GetDeviceInfoAsync()
    {
        return _sharedClient?.GetDeviceInfoAsync();
    }

    /// <summary>
    /// Logs device state in a readable format
    /// </summary>
    /// <param name="deviceInfo">Device state snapshot to log</param>
    /// <param name="prefix">Optional prefix for indentation</param>
    protected void LogDeviceState(DeviceInfo? deviceInfo, string prefix = "")
    {
        _output.WriteLine($"{prefix}   Connected: {deviceInfo?.IsConnected}");
        _output.WriteLine($"{prefix}   Battery: {deviceInfo?.BatteryLevel}%");
        _output.WriteLine($"{prefix}   Firmware: {deviceInfo?.FirmwareVersion}");
    }

    #endregion

    #region Connection Management

    /// <summary>
    /// Ensures a connection to the MeshCore device on COM3
    /// </summary>
    protected async Task EnsureConnected()
    {
        MeshCodeClient? clientToConnect = null;

        lock (_clientLock)
        {
            if (!_clientInitialized)
            {
                _sharedClient = new MeshCodeClient("COM3");
                _clientInitialized = true;
                clientToConnect = _sharedClient;
            }
            else if (_sharedClient != null && !_sharedClient.IsConnected)
            {
                clientToConnect = _sharedClient;
            }
        }

        if (clientToConnect != null)
        {
            await clientToConnect.ConnectAsync();
            _output.WriteLine($"✅ Connected to device: {clientToConnect.ConnectionId}");
        }
    }

    /// <summary>
    /// Clears device state and stabilizes communication
    /// </summary>
    protected async Task ClearDeviceState()
    {
        try
        {
            if (_sharedClient?.IsConnected == true)
            {
                _output.WriteLine("🧹 Clearing device state...");

                // Add device stabilization - drain any pending frames from the device
                // by sending a simple, safe command and waiting for response
                try
                {
                    _output.WriteLine("   Stabilizing device communication...");

                    // Send a few simple, safe commands to flush any pending responses
                    for (int attempt = 0; attempt < 3; attempt++)
                    {
                        try
                        {
                            // Use device time query as it's safe and widely supported
                            var timeResponse = await _sharedClient.GetDeviceTimeAsync();
                            _output.WriteLine($"   Device time sync successful: {timeResponse}");
                            break; // Success - device is responding correctly
                        }
                        catch (Exception ex)
                        {
                            _output.WriteLine($"   Device stabilization attempt {attempt + 1} failed: {ex.Message}");
                            if (attempt < 2) // Don't delay on the last attempt
                            {
                                await Task.Delay(500);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"   Device stabilization failed: {ex.Message}");
                }

                // Additional stabilization delay
                await Task.Delay(GetStabilizationDelay());
                _output.WriteLine("   Device state clearing completed");
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"⚠️  Warning during device state clear: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the stabilization delay for device state clearing
    /// Override in derived classes if different timing is needed
    /// </summary>
    /// <returns>Delay in milliseconds</returns>
    protected virtual int GetStabilizationDelay()
    {
        return 1000; // Default 1 second
    }

    /// <summary>
    /// Gets the shared MeshCore client instance
    /// </summary>
    protected MeshCodeClient? SharedClient => _sharedClient;

    #endregion

    #region Common Test Patterns

    /// <summary>
    /// Executes a standard test pattern with state capture, connection, and comparison
    /// </summary>
    /// <param name="testName">Name of the test for logging</param>
    /// <param name="testAction">The test action to execute</param>
    /// <param name="requiresConnection">Whether the test requires a device connection</param>
    protected async Task ExecuteStandardTest(string testName, Func<Task> testAction, bool requiresConnection = true)
    {
        _output.WriteLine($"TEST: {testName}");
        _output.WriteLine(new string('=', testName.Length + 6));

        if (requiresConnection)
        {
            await EnsureConnected();
        }

        LogDeviceState(await GetDeviceInfoAsync(), "   ");

        await testAction();
    }

    /// <summary>
    /// Creates a standard device connection test
    /// </summary>
    /// <returns>Task representing the test</returns>
    protected async Task ExecuteDeviceConnectionTest()
    {
        await ExecuteStandardTest("Device Connection", async () =>
        {
            await EnsureConnected();

            if (_sharedClient == null || !_sharedClient.IsConnected)
            {
                throw new InvalidOperationException("Failed to establish device connection");
            }

            _output.WriteLine("✅ Device connection successful");

            // Clear any initial state
            await ClearDeviceState();
        }, requiresConnection: false); // Don't require connection since we're testing connection itself
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Generates a test node ID for contact testing
    /// </summary>
    /// <returns>Random hex string for testing</returns>
    protected static string GenerateTestNodeId()
    {
        var random = new Random();
        var bytes = new byte[16];
        random.NextBytes(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
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

            // Call derived class cleanup
            PerformCustomCleanup();

            // Final state capture
            if (_sharedClient?.IsConnected == true)
            {
                try
                {
                    LogDeviceState(GetDeviceInfoAsync().Result, "   ");
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"   ⚠️  Failed to capture final state: {ex.Message}");
                }
            }

            _output.WriteLine($"✅ {_testMethodName} cleanup completed");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"⚠️  Warning during cleanup: {ex.Message}");
        }
    }

    /// <summary>
    /// Virtual method for derived classes to perform custom cleanup
    /// </summary>
    protected virtual void PerformCustomCleanup()
    {
        // Override in derived classes for specific cleanup logic
    }

    #endregion
}