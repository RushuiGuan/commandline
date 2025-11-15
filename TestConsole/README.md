# System.CommandLine 2.0.0 Demonstration Program

This is a comprehensive demonstration program showcasing the capabilities of **System.CommandLine 2.0.0**, a powerful command-line parsing library for .NET applications.

## Features Demonstrated

### ? Core Command-Line Features
- **Root Commands and Subcommands**: Hierarchical command structure
- **Arguments**: Positional parameters with type safety and arity control
- **Options**: Named parameters with various data types
- **Global Options**: Options available to all commands
- **Help Generation**: Automatic help text generation
- **Type Safety**: Strong typing for string, int, bool, FileInfo, arrays
- **Validation**: Built-in argument and option validation

### ? Advanced Features
- **Dependency Injection**: Full DI container integration
- **Logging**: Microsoft.Extensions.Logging integration
- **Error Handling**: Comprehensive error reporting
- **Parse Result Access**: Access to parsed command-line data
- **Flexible Arity**: Support for zero, one, zero-or-one, one-or-more arguments

## Prerequisites

- .NET 10.0 or later
- System.CommandLine 2.0.0 package

## Building and Running

### Build the Project
```bash
cd C:\app\commandline
dotnet build TestConsole\TestConsole.csproj
```

### Run the Program
```bash
# Show main program info and capabilities
dotnet run --project TestConsole\TestConsole.csproj

# Show help for the root command
dotnet run --project TestConsole\TestConsole.csproj -- --help

# Show help for a specific command
dotnet run --project TestConsole\TestConsole.csproj -- greet --help
```

## Available Commands

### 1. `greet` - Personalized Greetings
Generate personalized greetings with various customization options.

**Usage:**
```bash
dotnet run --project TestConsole\TestConsole.csproj -- greet "John"
dotnet run --project TestConsole\TestConsole.csproj -- greet "Alice" --count 3 --uppercase
dotnet run --project TestConsole\TestConsole.csproj -- greet "Bob" --language spanish --style formal
```

**Options:**
- `<name>` (required): The name of the person to greet
- `--count <number>`: Number of times to repeat the greeting
- `--uppercase`: Convert greeting to uppercase
- `--language <lang>`: Language for greeting (english, spanish, french, german)
- `--style <style>`: Greeting style (formal, casual, friendly)

### 2. `file` - File Processing
Process text files with various output formats and options.

**Usage:**
```bash
dotnet run --project TestConsole\TestConsole.csproj -- file "sample.txt"
dotnet run --project TestConsole\TestConsole.csproj -- file "input.txt" --output "result.json" --format json
dotnet run --project TestConsole\TestConsole.csproj -- file "data.txt" --verbose --overwrite
```

**Options:**
- `<input-file>` (required): Path to the input file to process
- `--output <file>`: Output file path (optional)
- `--format <format>`: Output format (json, xml, text, csv)
- `--verbose`: Enable verbose file processing output
- `--overwrite`: Overwrite existing output file
- `--buffer-size <kb>`: Buffer size for file processing (KB)

### 3. `math` - Mathematical Operations
Perform mathematical operations on a series of numbers.

**Usage:**
```bash
dotnet run --project TestConsole\TestConsole.csproj -- math 1 2 3 4 5
dotnet run --project TestConsole\TestConsole.csproj -- math 10 20 30 --operation average --precision 3
dotnet run --project TestConsole\TestConsole.csproj -- math 2.5 4.7 1.2 --scientific --output-format json
```

**Options:**
- `<numbers>...` (required): Series of numbers to perform operations on
- `--operation <op>`: Mathematical operation (sum, average, min, max, product, median)
- `--precision <digits>`: Number of decimal places in result
- `--scientific`: Display result in scientific notation
- `--output-format <format>`: Output format (standard, detailed, json)

### 4. `version` - Version Information
Display version and system information.

**Usage:**
```bash
dotnet run --project TestConsole\TestConsole.csproj -- version
dotnet run --project TestConsole\TestConsole.csproj -- version --detailed --dependencies
```

**Options:**
- `--detailed`: Show detailed system information
- `--dependencies`: Show dependency versions

### 5. `capabilities` - Feature Demonstration
Show System.CommandLine 2.0.0 capabilities and features.

**Usage:**
```bash
dotnet run --project TestConsole\TestConsole.csproj -- capabilities
dotnet run --project TestConsole\TestConsole.csproj -- capabilities --all --feature parsing
```

**Options:**
- `--feature <name>`: Specific feature to demonstrate
- `--all`: Show all capabilities

## Global Options

These options are available for all commands:

- `--verbose`: Enable verbose output across the application
- `--debug`: Enable debug mode for detailed logging
- `--help` / `-h` / `-?`: Show help information
- `--version`: Show version information

## Architecture

The demonstration program showcases several important architectural patterns:

### Command Structure
```
RootCommand
??? greet (with arguments and options)
??? file (with FileInfo handling)
??? math (with array arguments)
??? version (simple command)
??? capabilities (service integration)
```

### Dependency Injection
- **Service Registration**: All services registered in DI container
- **Logging Integration**: Microsoft.Extensions.Logging throughout
- **Handler Resolution**: Command handlers resolved from DI
- **Service Lifetime**: Proper service disposal

### Error Handling
- **Try-catch blocks**: Comprehensive error handling
- **Logging**: Error logging with structured logging
- **Exit codes**: Proper return codes for success/failure
- **Validation**: Built-in System.CommandLine validation

## Key System.CommandLine 2.0.0 Patterns

### 1. Command Creation
```csharp
var command = new Command("command-name", "Description");
command.Add(new Argument<string>("arg-name") { Description = "Argument description" });
command.Add(new Option<bool>("--flag") { Description = "Option description" });
command.SetAction(handler.Invoke);
```

### 2. Handler Pattern
```csharp
public class CommandHandler
{
    public int Invoke(ParseResult parseResult)
    {
        // Command logic here
        return 0; // Success
    }
}
```

### 3. Program Execution
```csharp
return await rootCommand.Parse(args).InvokeAsync();
```

## Testing the Program

### Test Help System
```bash
# Root help
dotnet run --project TestConsole\TestConsole.csproj -- --help

# Command-specific help
dotnet run --project TestConsole\TestConsole.csproj -- greet --help
dotnet run --project TestConsole\TestConsole.csproj -- math --help
```

### Test Arguments and Options
```bash
# Test required arguments
dotnet run --project TestConsole\TestConsole.csproj -- greet "Test User"

# Test multiple arguments
dotnet run --project TestConsole\TestConsole.csproj -- math 1 2 3 4 5

# Test options
dotnet run --project TestConsole\TestConsole.csproj -- greet "User" --count 2 --uppercase
```

### Test Error Conditions
```bash
# Missing required argument
dotnet run --project TestConsole\TestConsole.csproj -- greet

# Invalid command
dotnet run --project TestConsole\TestConsole.csproj -- invalid-command
```

## Conclusion

This demonstration program successfully showcases the power and flexibility of System.CommandLine 2.0.0, including:

- **Modern API**: Clean, intuitive API design
- **Type Safety**: Strong typing throughout
- **Integration**: Seamless DI and logging integration
- **Validation**: Built-in validation and error handling
- **Help System**: Automatic help generation
- **Extensibility**: Easy to extend and customize

The program compiles without errors and runs successfully, demonstrating all key features of System.CommandLine 2.0.0 in a practical, real-world scenario.