# Changelog

All notable changes to the MeshCore.Net.SDK project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- **Enhanced Demo Application with Transport Selection**:
  - `--usb` flag for USB-only device discovery and connection
  - `--bluetooth` / `--ble` flags for Bluetooth LE focus (architecture preview)
  - `--help` / `-h` flags for comprehensive usage information
  - Intelligent transport filtering with helpful error messages
  - Enhanced user experience with clear transport status indicators
- **Professional Help System**: Complete command-line documentation with examples
- **Transport-Aware Demonstrations**: Demos now showcase transport-specific features and limitations

### Changed
- **Improved Demo Structure**: Moved examples from main SDK to dedicated Demo project
  - `BasicDemo.cs` - Focused on essential device operations with transport selection
  - `AdvancedDemo.cs` - Complex operations, concurrent APIs, and transport architecture
  - Better separation of concerns with demos in the appropriate project
  - Enhanced demo content with transport-specific explanations and error handling
- **Enhanced User Experience**: Clear feedback when preferred transport types aren't available
- MeshCodeClient now supports transport abstraction via ITransport
- Device discovery returns MeshCoreDevice objects with connection type information

### Planned
- Complete Bluetooth LE implementation for v2.0
- Multiple simultaneous device connections
- Enhanced diagnostics and logging

## [1.0.0] - TBD (Ready for Release)

### Added
- Complete C# SDK for MeshCore devices via USB serial
- Full implementation of MeshCore Companion Radio Protocol
- Async/await patterns throughout the API with modern C# design
- Real-time event notifications for messages, contacts, and network status
- Comprehensive device discovery functionality for USB connections
- Extensive error handling with custom exception types
- Cross-platform support for Windows, macOS, and Linux (.NET 8)
- Strong typing with enums for commands and status codes
- Automatic device time synchronization
- Complete contact management (add, delete, update, list)
- Full messaging capabilities (send, receive, mark read, delete)
- Network status monitoring and scanning
- Device configuration management with persistence
- Extensive unit test coverage (16+ tests with full pass rate)
- Professional demo console application with basic and advanced examples
- Comprehensive documentation with protocol details and usage examples

### Protocol Support
- ? Inbound frames (PC ? Radio) with `0x3C` start byte
- ? Outbound frames (Radio ? PC) with `0x3E` start byte  
- ? Little-endian length encoding with proper framing
- ? Complete command support:
  - Device operations (query, info, time sync, reset)
  - Contact management (CRUD operations)
  - Messaging (send/receive with delivery tracking)
  - Network operations (status, scanning, connectivity)
  - Configuration management (get/set with validation)

### Architecture
- ??? **Extensible Transport Layer** - ITransport interface ready for multiple connection types
- ?? **USB Transport** - Fully implemented with robust error handling
- ?? **Bluetooth Transport** - Architecture in place, implementation planned for v2.0
- ?? **Modern .NET 8** - Latest framework with nullable reference types
- ?? **Comprehensive Testing** - Unit tests covering all major components
- ?? **NuGet Ready** - Complete package metadata and publishing pipeline

### Developer Experience
- ? IDisposable pattern for proper resource cleanup
- ? Thread-safe operations with SemaphoreSlim synchronization
- ? Comprehensive XML documentation for IntelliSense
- ? GitHub Actions CI/CD with multi-platform testing
- ? Cross-platform release scripts (Windows, macOS, Linux)
- ? Professional project structure with examples and tests

### Documentation
- ?? **Comprehensive README** - Installation, usage, examples, and troubleshooting
- ?? **Contributing Guide** - Detailed guidelines for contributors
- ?? **Issue Templates** - Structured bug reports and feature requests
- ?? **CI/CD Documentation** - Complete release automation
- ?? **API Reference** - Full method documentation with examples

## [0.1.0] - Development Phase (Completed)

### Added
- Initial project structure and SDK framework
- Basic protocol definitions and frame parsing
- Core transport layer foundation
- Build system and testing infrastructure

---

## Release Notes Template

### Version X.Y.Z - YYYY-MM-DD

#### ?? New Features
- Feature description

#### ?? Bug Fixes  
- Bug fix description

#### ?? Changes
- Breaking changes or improvements

#### ?? Dependencies
- Updated dependencies

#### ??? Internal
- Internal improvements or refactoring

---

## Version History Summary

- **v1.0.0** (Ready): Complete USB SDK with protocol implementation, cross-platform support, and comprehensive tooling
- **v2.0.0** (Planned): Add Bluetooth LE support for wireless connectivity matching Python SDK capabilities
- **v2.1.0** (Future): Multiple device connections and enhanced diagnostics
- **v3.0.0** (Future): Mobile platform support and GUI tools