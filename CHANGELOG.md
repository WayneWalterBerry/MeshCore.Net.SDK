# Changelog

All notable changes to the MeshCore.Net.SDK project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Initial release planning

### Changed
- TBD

### Deprecated
- TBD

### Removed
- TBD

### Fixed
- TBD

### Security
- TBD

## [1.0.0] - TBD

### Added
- Complete C# SDK for MeshCore devices
- USB serial protocol implementation with full frame support
- Async/await patterns throughout the API
- Real-time event notifications for messages, contacts, and network status
- Device discovery functionality
- Comprehensive error handling with custom exception types
- Cross-platform support for Windows, macOS, and Linux
- Strong typing with enums for commands and status codes
- Automatic device time synchronization
- Contact management operations (add, delete, update, list)
- Message operations (send, receive, mark read, delete)
- Network status monitoring and scanning
- Device configuration management
- Extensive unit test coverage (16+ tests)
- Demo console application with basic and advanced examples
- Comprehensive documentation and usage examples

### Protocol Support
- Inbound frames (PC ? Radio) with `0x3C` start byte
- Outbound frames (Radio ? PC) with `0x3E` start byte
- Little-endian length encoding
- Command support for:
  - Device operations (query, info, time, reset)
  - Contact management
  - Messaging
  - Network operations
  - Configuration

### Developer Experience
- Modern .NET 8 target framework
- Nullable reference types enabled
- IDisposable pattern for proper resource cleanup
- Thread-safe operations with SemaphoreSlim
- Comprehensive XML documentation
- NuGet package ready with proper metadata

## [0.1.0] - TBD

### Added
- Project structure and initial SDK framework
- Basic protocol definitions and frame parsing
- Core transport layer implementation

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