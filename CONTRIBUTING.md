# Contributing to MeshCore.Net.SDK

Thank you for your interest in contributing to MeshCore.Net.SDK! This document provides guidelines and information for contributors.

## ?? Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- Git
- A code editor (Visual Studio, VS Code, JetBrains Rider, etc.)

### Setting Up the Development Environment

1. **Fork and Clone the Repository**
   ```bash
   git clone https://github.com/YourUsername/MeshCore.Net.SDK.git
   cd MeshCore.Net.SDK
   ```

2. **Restore Dependencies**
   ```bash
   dotnet restore
   ```

3. **Build the Project**
   ```bash
   dotnet build
   ```

4. **Run Tests**
   ```bash
   dotnet test
   ```

5. **Run the Demo** (optional)
   ```bash
   dotnet run --project MeshCore.Net.SDK.Demo
   ```

## ?? Project Structure

```
MeshCore.Net.SDK/
??? MeshCore.Net.SDK/           # Main SDK library
?   ??? Protocol/               # Protocol implementation
?   ??? Models/                 # Data models
?   ??? Exceptions/             # Custom exceptions
?   ??? Transport/              # USB communication
?   ??? Examples/               # Usage examples
?   ??? MeshCodeClient.cs       # Main client
??? MeshCore.Net.SDK.Tests/     # Unit tests
??? MeshCore.Net.SDK.Demo/      # Console demo application
??? .github/                    # GitHub workflows and templates
```

## ?? How to Contribute

### Reporting Issues

1. Check existing [issues](https://github.com/WayneWalterBerry/MeshCore.Net.SDK/issues) to avoid duplicates
2. Use the appropriate issue template (Bug Report or Feature Request)
3. Provide detailed information including:
   - SDK version
   - .NET version
   - Operating system
   - Steps to reproduce
   - Expected vs actual behavior

### Submitting Changes

1. **Create a Feature Branch**
   ```bash
   git checkout -b feature/your-feature-name
   # or
   git checkout -b fix/your-bug-fix
   ```

2. **Make Your Changes**
   - Follow the coding standards (see below)
   - Add tests for new functionality
   - Update documentation if needed

3. **Test Your Changes**
   ```bash
   dotnet build
   dotnet test
   ```

4. **Commit Your Changes**
   ```bash
   git add .
   git commit -m "feat: add new feature description"
   ```

5. **Push and Create Pull Request**
   ```bash
   git push origin feature/your-feature-name
   ```
   Then create a pull request on GitHub.

### Commit Message Guidelines

We use [Conventional Commits](https://www.conventionalcommits.org/) format:

```
type(scope): description

[optional body]

[optional footer]
```

**Types:**
- `feat`: New features
- `fix`: Bug fixes
- `docs`: Documentation changes
- `test`: Adding or updating tests
- `refactor`: Code refactoring
- `perf`: Performance improvements
- `chore`: Maintenance tasks

**Examples:**
```
feat(transport): add support for custom baud rates
fix(protocol): resolve frame parsing edge case
docs(readme): update installation instructions
test(client): add integration tests for messaging
```

## ?? Coding Standards

### C# Guidelines

1. **Follow Microsoft C# Coding Conventions**
   - Use PascalCase for public members
   - Use camelCase for private fields and local variables
   - Use meaningful names for variables, methods, and classes

2. **Code Style**
   ```csharp
   // Good
   public async Task<DeviceInfo> GetDeviceInfoAsync()
   {
       var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_GET_DEVICE_INFO);
       return ParseDeviceInfo(response.GetDataPayload());
   }
   
   // Use nullable reference types
   public string? DeviceId { get; set; }
   
   // Use async/await consistently
   public async Task<List<Contact>> GetContactsAsync()
   {
       // Implementation
   }
   ```

3. **Documentation**
   - Add XML documentation for public APIs
   - Include parameter descriptions and return value information
   - Provide usage examples for complex methods

   ```csharp
   /// <summary>
   /// Sends a text message to a contact
   /// </summary>
   /// <param name="toContactId">The ID of the recipient contact</param>
   /// <param name="content">The message content</param>
   /// <returns>The sent message with ID and status</returns>
   /// <exception cref="ProtocolException">Thrown when the send operation fails</exception>
   public async Task<Message> SendMessageAsync(string toContactId, string content)
   ```

### Testing Guidelines

1. **Unit Tests**
   - Use xUnit framework
   - Test both happy path and error scenarios
   - Mock external dependencies
   - Aim for good test coverage

2. **Test Naming**
   ```csharp
   [Fact]
   public void CreateInbound_ShouldCreateCorrectFrame()
   {
       // Arrange
       var payload = new byte[] { 0x16, 0x01, 0x02 };
       
       // Act
       var frame = MeshCoreFrame.CreateInbound(payload);
       
       // Assert
       Assert.Equal(ProtocolConstants.FRAME_START_INBOUND, frame.StartByte);
   }
   ```

## ?? Areas for Contribution

### High Priority
- ?? Additional protocol command implementations
- ?? Integration tests with real hardware
- ?? More usage examples and tutorials
- ?? Bug fixes and stability improvements

### Medium Priority
- ?? Performance optimizations
- ?? Additional platform-specific features
- ?? Logging and diagnostics improvements
- ?? Security enhancements

### Nice to Have
- ?? Mobile platform support (Xamarin/MAUI)
- ?? GUI demo application
- ?? Metrics and monitoring support
- ?? Internationalization

## ?? Code Review Process

1. **Automated Checks**
   - All GitHub Actions must pass
   - Code coverage should not decrease significantly
   - No compiler warnings

2. **Manual Review**
   - Code follows project conventions
   - Changes are well-tested
   - Documentation is updated
   - Breaking changes are properly communicated

3. **Review Timeline**
   - Small fixes: 1-2 days
   - New features: 3-7 days
   - Large changes: 1-2 weeks

## ?? Documentation

### API Documentation
- Use XML comments for all public APIs
- Include code examples where appropriate
- Document exceptions and edge cases

### README Updates
- Update installation instructions for new features
- Add examples for new functionality
- Keep the feature list current

### Changelog
- Add entries for all user-facing changes
- Follow the Keep a Changelog format
- Include migration guides for breaking changes

## ??? Release Process

1. **Version Bumping**
   - Follow semantic versioning (semver)
   - Update version in project files
   - Update changelog

2. **Testing**
   - All tests must pass
   - Manual testing with real devices (if available)
   - Performance regression testing

3. **Release**
   - Create GitHub release with detailed notes
   - Publish NuGet package
   - Update documentation

## ? Getting Help

- ?? [GitHub Discussions](https://github.com/WayneWalterBerry/MeshCore.Net.SDK/discussions) for questions
- ?? [GitHub Issues](https://github.com/WayneWalterBerry/MeshCore.Net.SDK/issues) for bugs
- ?? Email maintainers for security issues

## ?? License

By contributing to MeshCore.Net.SDK, you agree that your contributions will be licensed under the MIT License.

---

Thank you for contributing! ??