# Contact API Tests Summary

## Overview
Comprehensive integration tests for the Contact API functionality using a real MeshCore device connected to COM3. This consolidated test suite covers functional tests, advanced scenarios, edge cases, and error handling.

## Test Class: ContactApiTests

**File**: `ContactApiTests.cs`  
**Collection**: `SequentialTests` (ensures tests run sequentially to avoid COM port conflicts)  
**Device Required**: Physical MeshCore device on COM3

## Test Scenarios

### Basic Functional Tests
1. **Test_01_DeviceConnection_ShouldConnectToCOM3Successfully**
   - Verifies basic device connection functionality
   - Ensures COM3 connectivity is working

2. **Test_02_GetContacts_ShouldRetrieveContactList**
   - Tests contact list retrieval
   - Displays contact summary information

3. **Test_03_GetDeviceInfo_ShouldReturnDeviceDetails**
   - Validates device information retrieval
   - Shows device firmware, hardware, and battery status

### Advanced Contact Operations
4. **Test_04_ContactNameEncoding_ShouldHandleSpecialCharacters**
   - Tests various contact name formats:
     - ASCII_Basic: "TestContact123"
     - Special_Chars: "Test-Contact_#1"
     - Numbers_Only: "1234567890"
     - Mixed_Case: "tESt_CoNtAcT"

5. **Test_05_NodeIdValidation_ShouldHandleVariousFormats**
   - Tests different Node ID formats:
     - Standard_Hex: 32-char lowercase hex
     - Upper_Case_Hex: 32-char uppercase hex
     - Short_NodeId: 8-char hex
     - Alphanumeric: mixed alphanumeric string

6. **Test_06_ContactDataPersistence_ShouldMaintainDataIntegrity**
   - Tests data consistency across multiple retrievals
   - Validates contact data persistence

7. **Test_07_ContactCRUD_ShouldHandleFullLifecycle**
   - Complete CREATE, READ, DELETE lifecycle test
   - Verifies all CRUD operations work correctly

### Error Handling Tests
8. **Test_08_ErrorHandling_ShouldHandleInvalidOperations**
   - Tests error conditions:
     - Empty contact names
     - Empty node IDs
     - Non-existent contact deletion

## Key Features

### Shared Client Management
- Uses static shared client to avoid multiple connections
- Thread-safe initialization and management
- Improved test performance and reliability

### Comprehensive Cleanup
- Automatic cleanup of test contacts in Dispose()
- Prevents test data accumulation on device
- Graceful error handling during cleanup

### ETW Event Logging
- Integrated ETW event listener for debugging
- Captures SDK events during test execution
- Shared listener to avoid conflicts

### Sequential Execution
- All tests run sequentially via `SequentialTests` collection
- Prevents COM port conflicts between tests
- Ensures reliable test execution

## Test Data Management

### Generated Test Data
- Random Node IDs for each test contact
- Timestamped contact names to avoid collisions
- Automatic cleanup tracking

### Test Contact Tracking
- `_createdTestContacts` list tracks all added contacts
- Cleanup removes all test contacts after test completion
- Prevents device storage pollution

## Usage

### Prerequisites
- Physical MeshCore device connected to COM3
- Device should be powered on and responsive
- No other applications using COM3

### Running Tests
```bash
# Run all contact tests
dotnet test --filter "FullyQualifiedName~ContactApiTests"

# Run specific test
dotnet test --filter "Test_02_GetContacts_ShouldRetrieveContactList"

# Run with detailed output
dotnet test --filter "FullyQualifiedName~ContactApiTests" --verbosity normal
```

### Expected Behavior
- Tests run sequentially (not in parallel)
- Each test reports detailed progress via test output
- Test contacts are automatically cleaned up
- ETW events are captured for debugging

## Benefits of Consolidation

### Simplified Maintenance
- Single test file instead of two separate classes
- Unified test infrastructure and helper methods
- Consistent naming and organization

### Better Resource Management
- Shared client reduces connection overhead
- Single ETW listener avoids conflicts
- Consolidated cleanup logic

### Improved Test Coverage
- Combines functional and advanced scenarios
- Better test organization and flow
- Comprehensive error handling coverage

### Enhanced Debugging
- All contact tests in one place
- Consistent logging and output formatting
- Easier to trace test execution flow

## Migration Notes

The original two test classes have been consolidated:
- `ContactApiFunctionalTests.cs` (removed)
- `ContactApiAdvancedTests.cs` (removed)
- `ContactApiTests.cs` (new consolidated class)

All test functionality has been preserved and improved with better organization and shared infrastructure.