# Albatross.CommandLine

A .NET library that simplifies creating command-line applications with [System.CommandLine](https://learn.microsoft.com/en-us/dotnet/standard/commandline/). It provides automatic code generation and dependency injection support while maintaining full access to System.CommandLine's capabilities. The framework is opinionated toward async actions with out-of-the-box support for cancellation and graceful shutdown.

Designed for enterprise CLI applications, Albatross.CommandLine enforces consistent async patterns, built-in dependency injection, and graceful shutdown handling. These opinionated choices reduce complexity and ensure scalability from simple utilities to complex enterprise workflows.

## Key Features
- **Minimal Boilerplate** - Attribute-based command definition with automatic code generation
- **Dependency Injection** - Built-in DI container integration
- **Minimum Dependencies** - Only depends on `System.CommandLine` and `Microsoft.Extensions.Hosting`.
- **Full Flexibility** - Direct access to `System.CommandLine` when needed
- **Cancellation & Graceful Shutdown** - Built-in support for Ctrl+C interruption via cancellation tokens and graceful shutdown handling
- **Reusable Parameter** - Create customized `Option` and `Argument` classes and use `[UseOption<T>]` and `[UseArgument<T>]` attributes to compose them into commands with automatic code generation
- **Advanced Option Handlers** - Injectable async option handlers with `[OptionHandler]` attribute for pre-processing and input transformation patterns. Execute database validation, API calls, or transform simple inputs into complex objects before command execution
- **Easy Extensions** - Use `CommandHost.ConfigureHost()` to bootstrap additional services, or use [Albatross.CommandLine.Default](https://www.nuget.org/packages/Albatross.CommandLine.Default) for pre-configured Serilog logging and JSON/environment configuration support

## Language Support
C# Only

## Dependencies
- **System.CommandLine 2.0.1+**
- **Microsoft.Extensions.Hosting 8.0.1+**

## Prerequisites
- **C# Compiler 4.10.0+** (included with .NET 8 SDK)

