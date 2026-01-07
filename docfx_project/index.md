# Albatross.CommandLine

A .NET library that simplifies creating command-line applications with [System.CommandLine](https://learn.microsoft.com/en-us/dotnet/standard/commandline/). It provides automatic code generation and dependency injection support while maintaining full access to System.CommandLine's capabilities. The framework is opinionated toward async actions with out-of-the-box support for cancellation and graceful shutdown.

Albatross.CommandLine is designed specifically for enterprise CLI applications that require consistency, efficiency, and maintainability at scale. Rather than providing maximum flexibility, it enforces proven patterns that reduce complexity and cognitive load across large development teams. The library minimizes boilerplate through automatic code generation while ensuring all commands follow standardized async patterns with built-in dependency injection and graceful shutdown capabilities.

**Design Philosophy: Enterprise-First Approach** - The library's opinionated nature reflects the realities of building production CLI applications in enterprise environments. Async-first architecture ensures efficient handling of I/O-bound operations that are common when interacting with databases, APIs, and external services. Built-in dependency injection provides the structured service management and testability required for complex business logic, while graceful shutdown and cancellation support ensures reliable operation in containerized and orchestrated environments. These choices create a consistent foundation that scales from simple utilities to complex enterprise workflows without requiring architectural changes as applications grow.

## Key Features
- **Minimal Boilerplate** - Attribute-based command definition with automatic code generation
- **Dependency Injection** - Built-in DI container integration
- **Minimum Dependencies** - Only depends on `System.CommandLine` and `Microsoft.Extensions.Hosting`.
- **Full Flexibility** - Direct access to `System.CommandLine` when needed
- **Cancellation & Graceful Shutdown** - Built-in support for Ctrl+C interruption via cancellation tokens and graceful shutdown handling
- **Reusable Parameter** - Reusable parameter classes with predefined options and arguments that can be composed into complex commands
- **Easy Extensions** - Use `CommandHost.ConfigureHost()` to bootstrap additional services, or use [Albatross.CommandLine.Default](https://www.nuget.org/packages/Albatross.CommandLine.Default) for pre-configured Serilog logging and JSON/environment configuration support


## 🔧 Dependencies
- **System.CommandLine 2.0.1+**
- **Microsoft.Extensions.Hosting 8.0.1+**

## 🔧 Prerequisites
- **C# Compiler 4.10.0+** (included with .NET 8 SDK)

