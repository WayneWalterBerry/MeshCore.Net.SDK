# Coding Guidelines for MeshCore.Net.SDK

This document outlines the coding standards, conventions, and best practices for the MeshCore.Net.SDK project.

## ?? General Principles

- **Consistency**: Follow established patterns within the codebase
- **Readability**: Write code that is self-documenting and easy to understand
- **Maintainability**: Prefer simple, clear solutions over complex ones
- **Performance**: Consider performance implications, especially for serial communication
- **Error Handling**: Provide meaningful error messages and proper exception handling
- **Silent by Default**: Libraries should not produce output unless explicitly configured
- **Abstraction-Based**: Use `ILogger`/`ILoggerFactory` abstractions, never concrete implementations
- **Optional Logging**: Always provide no-op defaults when loggers are not supplied

## ?? Code Style

### Naming Conventions

#### Classes and Interfaces
- Use **PascalCase** for class names: `UsbTransport`, `MeshCoreFrame`
- Use **PascalCase** for interface names with 'I' prefix: `ITransport`
- Use descriptive names that clearly indicate the class purpose

#### Methods and Properties
- Use **PascalCase** for public methods and properties: `ConnectAsync()`, `IsConnected`
- Use **camelCase** for private fields with underscore prefix: `_serialPort`, `_writeLock`
- Use **camelCase** for local variables and parameters: `portName`, `frameBuffer`

#### Constants and Enums
- Use **SCREAMING_SNAKE_CASE** for constants: `FRAME_START_OUTBOUND`, `DEFAULT_TIMEOUT_MS`
- Use **PascalCase** for enum names and values: `MeshCoreCommand.CMD_DEVICE_QUERY`

#### Events and Delegates
- Use **PascalCase** with descriptive names: `FrameReceived`, `ErrorOccurred`
- Event handler parameters should follow standard .NET pattern: `(object? sender, TEventArgs e)`

### Code Organization

#### File Structure
```csharp
// 1. Using statements (grouped and sorted)
using System.IO.Ports;
using MeshCore.Net.SDK.Protocol;
using MeshCore.Net.SDK.Exceptions;

// 2. Namespace declaration
namespace MeshCore.Net.SDK.Transport;

// 3. Class documentation
/// <summary>
/// Brief description of the class purpose
/// </summary>
public class ClassName
{
    // 4. Fields (private first, then public)
    private readonly SerialPort _serialPort;
    
    // 5. Events
    public event EventHandler<FrameType>? EventName;
    
    // 6. Properties
    public bool PropertyName { get; }
    
    // 7. Constructors
    public ClassName() { }
    
    // 8. Public methods
    public async Task MethodName() { }
    
    // 9. Private methods
    private void HelperMethod() { }
    
    // 10. Dispose method (if applicable)
    public void Dispose() { }
}
```

#### Method Organization
- Public methods before private methods
- Async methods should have `Async` suffix: `ConnectAsync()`, `SendFrameAsync()`
- Group related methods together
- Separate method groups with blank lines

### Documentation

#### XML Documentation
All public APIs must have XML documentation:

```csharp
/// <summary>
/// Brief description of what the method does
/// </summary>
/// <param name="paramName">Description of the parameter</param>
/// <returns>Description of what is returned</returns>
/// <exception cref="ExceptionType">When this exception is thrown</exception>
public async Task<ReturnType> MethodName(ParameterType paramName)
```

#### Comments
- Use `//` for single-line comments
- Explain **why** something is done, not **what** is being done
- Add comments for complex logic or protocol-specific behavior
- Use TODO comments sparingly and with context: `// TODO: Optimize buffer management for high-frequency operations`

### Console Output Guidelines

#### SDK Libraries
- **Never use `Console.WriteLine`** in library code
- **Never write to fixed file paths** from libraries
- **Never hijack application output streams**
- All output must go through provided `ILogger` abstractions
- Applications control where and how logs are written
- Use clean, professional text-based indicators when logging
- Keep log output minimal and informative

#### Examples of Professional Log Output
```csharp
// ? Good: Clean, professional logging
_logger.LogInformation("Testing port {Port}", portName);
_logger.LogWarning("Connection timeout for {Port}", portName);
_logger.LogError("Error for {Port}: {ErrorType} - {ErrorMessage}", portName, "UnauthorizedAccess", "Access denied");

// ? Avoid: Console output or emoji icons in SDK libraries
Console.WriteLine("?? Testing port COM3...");
Console.WriteLine("  ? Connection timeout for COM3");
Console.WriteLine("  ?? Error for COM3: UnauthorizedAccess - Access denied");
```

### Error Handling

#### Exception Types
- Use specific exception types: `DeviceConnectionException`, `MeshCoreTimeoutException`
- Always include meaningful error messages
- Preserve original exceptions when wrapping: `throw new CustomException("Message", originalException)`

#### Async/Await Patterns
```csharp
// ? Good: Proper exception handling
try
{
    var result = await SomeAsyncOperation();
    return result;
}
catch (SpecificException ex)
{
    // Handle specific exception
    throw new CustomException("Meaningful message", ex);
}
finally
{
    // Cleanup resources
}

// ? Avoid: Catching and re-throwing generic exceptions
try
{
    var result = await SomeAsyncOperation();
    return result;
}
catch (Exception ex)
{
    throw; // This loses stack trace context
}
```

### Resource Management

#### IDisposable Pattern
```csharp
public void Dispose()
{
    if (!_disposed)
    {
        // Dispose managed resources
        _serialPort?.Dispose();
        _semaphore?.Dispose();
        
        // Set disposed flag
        _disposed = true;
    }
}
```

#### Using Statements
- Prefer `using` declarations over `using` blocks when possible
- Always dispose resources that implement `IDisposable`

```csharp
// ? Preferred: using declaration
using var transport = new UsbTransport(portName);
await transport.ConnectAsync();

// ? Alternative: using block for complex scenarios
using (var transport = new UsbTransport(portName))
{
    await transport.ConnectAsync();
    // more operations...
}
```

## ?? Technology-Specific Guidelines

### Library Logging Best Practices

#### Dependency Injection Pattern
- Accept `ILoggerFactory?` for multi-class SDKs
- Accept `ILogger<T>?` for single-class components
- Use `NullLogger` and `NullLoggerFactory` as defaults

```csharp
// ? Good: Optional logger with no-op default
public MeshCodeClient(ILoggerFactory? loggerFactory = null)
{
    _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
    _logger = _loggerFactory.CreateLogger<MeshCodeClient>();
}
```

#### High-Performance Logging
- Use source-generated logging with `LoggerMessage` attribute for performance-critical paths
- Avoid string interpolation in log messages
- Use structured logging with message templates
- Check log level before expensive operations: `if (_logger.IsEnabled(LogLevel.Debug))`

```csharp
// ? Good: Source-generated high-performance logging
[LoggerMessage(EventId = 1001, Level = LogLevel.Information, 
    Message = "Device discovery started for transport: {TransportType}")]
public static partial void LogDeviceDiscoveryStarted(this ILogger logger, string transportType);
```

#### ETW (Event Tracing for Windows) Integration
- Provide dual logging: both `ILogger` and ETW for comprehensive observability
- Use consistent event IDs and structured data across both systems
- ETW events should complement, not replace, `ILogger` functionality
- Enable external monitoring tools to consume ETW events independently

```csharp
// ? Good: Dual logging approach
_logger.LogDeviceConnectionStarted(deviceId, transportType);
MeshCoreSdkEventSource.Log.DeviceConnectionStarted(deviceId, transportType);
```

#### Logging Performance
- Use message templates, not string concatenation
- Avoid boxing in hot logging paths
- Prefer source-generated logging for frequently called methods
- Use structured logging with named parameters

```csharp
// ? Good: Structured logging with templates
_logger.LogInformation("Device {DeviceId} connected via {Transport} in {Duration}ms", 
    deviceId, transport, duration);

// ? Avoid: String interpolation or concatenation
_logger.LogInformation($"Device {deviceId} connected via {transport} in {duration}ms");
_logger.LogInformation("Device " + deviceId + " connected via " + transport);
```

### Serial Communication
- Always check `IsConnected` before operations
- Use proper timeouts for all serial operations
- Handle `OperationCanceledException` in background loops
- Buffer management should prevent memory leaks

### Async Programming
- Use `ConfigureAwait(false)` for library code when appropriate
- Don't block on async operations with `.Result` or `.Wait()`
- Use `CancellationToken` for long-running operations
- Prefer `Task.Run()` for CPU-bound work in async contexts

### Event Handling
- Always check for null before invoking events: `EventName?.Invoke(this, args)`
- Unsubscribe from events to prevent memory leaks
- Use weak event patterns for long-lived objects if needed

## ?? Testing Guidelines

### Unit Tests
- Test file naming: `ClassNameTests.cs`
- Test method naming: `MethodName_Scenario_ExpectedResult`
- Use `Arrange, Act, Assert` pattern
- Mock external dependencies (serial ports, etc.)

### Integration Tests
- Test actual serial communication scenarios
- Use descriptive test names that explain the scenario
- Clean up resources in test teardown

## ?? Project Structure

### Folder Organization
```
MeshCore.Net.SDK/
??? Protocol/           # Protocol definitions and frame handling
??? Transport/          # Communication layer implementations  
??? Exceptions/         # Custom exception types
??? Models/            # Data models and DTOs
??? Logging/           # Logging infrastructure (ILogger + ETW)
??? Examples/          # Usage examples and documentation
```

### File Naming
- Use PascalCase for file names matching class names
- Group related classes in appropriate folders
- Keep file names descriptive and specific

## ?? Code Review Checklist

### Before Submitting
- [ ] Code follows naming conventions
- [ ] All public APIs have XML documentation
- [ ] Error handling is appropriate and consistent
- [ ] Resources are properly disposed
- [ ] Async patterns are used correctly
- [ ] Tests cover new functionality
- [ ] No hardcoded values (use constants/configuration)
- [ ] No console output or emoji icons in library code
- [ ] Logging uses ILogger abstractions with no-op defaults
- [ ] ETW events complement ILogger calls where appropriate

### Performance Considerations
- [ ] Minimize allocations in hot paths
- [ ] Use appropriate collection types
- [ ] Consider buffer reuse for serial communication
- [ ] Avoid blocking operations in async methods
- [ ] Use source-generated logging for performance-critical paths
- [ ] Check log levels before expensive logging operations

## ?? Common Anti-Patterns to Avoid

### General
```csharp
// ? Avoid: Generic catch-all exception handling
catch (Exception ex)
{
    // Log and continue - may hide important errors
}

// ? Avoid: Swallowing exceptions
try
{
    SomeOperation();
}
catch
{
    // Silent failure
}

// ? Avoid: Blocking on async operations
var result = SomeAsyncMethod().Result;
```

### Logging Anti-Patterns
```csharp
// ? Avoid: Direct console output in libraries
Console.WriteLine("Device connected");

// ? Avoid: String interpolation in logging
_logger.LogInformation($"Connected to {device}");

// ? Avoid: Forcing specific logging implementations
var logger = new ConsoleLogger(); // Forces console output
```

### Serial Communication
```csharp
// ? Avoid: Not checking connection state
await _serialPort.WriteAsync(data); // May throw if disconnected

// ? Avoid: Unbounded buffer growth
while (true)
{
    buffer.Add(ReadByte()); // No size limit
}
```

## ?? Additional Resources

- [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [.NET Framework Design Guidelines](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/)
- [Async/Await Best Practices](https://docs.microsoft.com/en-us/archive/msdn-magazine/2013/march/async-await-best-practices-in-asynchronous-programming)
- [High-performance logging in .NET](https://docs.microsoft.com/en-us/dotnet/core/extensions/high-performance-logging)
- [Logging library authors guidance](https://docs.microsoft.com/en-us/dotnet/core/extensions/logging-library-authors)

---

**Note**: These guidelines should be considered living documentation. As the project evolves, update this document to reflect new patterns and lessons learned.