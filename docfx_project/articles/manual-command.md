# Manual Commands

While Albatross.CommandLine excels at automatically generating commands from attributed classes, there are scenarios where you need full control over command creation. Manual commands allow you to directly use System.CommandLine's `Command` class while still integrating with the Albatross.CommandLine framework.

## When to Use Manual Commands

Manual commands are useful when you need:

- **Complex command logic** that doesn't fit the attribute-based pattern
- **Dynamic command creation** based on runtime conditions  
- **Direct System.CommandLine integration** with existing code
- **Custom validation or completion** that requires low-level access
- **Legacy command migration** from existing System.CommandLine code

## Basic Manual Command

Here's how to create a manual command by inheriting directly from System.CommandLine's `Command` class:

```csharp
using System;
using System.CommandLine;

public class ManualCommand : Command {
    public ManualCommand() : base("manual-command", "This command is created manually") {
        // Add arguments and options
        Add(TextArgument);
        Add(NameOption);
        
        // Set the command handler
        SetAction(Invoke);
    }

    // Define an argument with a default value
    public Argument<string> TextArgument { get; } = new Argument<string>("text") {
        DefaultValueFactory = (_) => "default text",
    };

    // Define a required option
    public Option<string> NameOption { get; } = new Option<string>("--name") {
        Description = "Name option for the manual command",
        Required = true,
    };

    // Command handler method
    int Invoke(ParseResult result) {
        var text = result.GetValue(TextArgument);
        var name = result.GetRequiredValue(NameOption);
        
        Console.WriteLine($"ManualCommand invoked with name: {name}");
        Console.WriteLine($"Text argument: {text}");
        
        return 0;
    }
}
```

## Integration with Albatross.CommandLine

To integrate your manual command with the Albatross.CommandLine framework, add it to the command builder in your `Program.cs`:

```csharp
using System.Threading.Tasks;
using Albatross.CommandLine;

namespace Sample.CommandLine {
    internal class Program {
        static Task<int> Main(string[] args) {
            var setup = new MySetup()
                // Add generated commands
                .AddCommands();
                
            // Add manual command with a parent key
            setup.CommandBuilder.AddWithParentKey("test", new ManualCommand());
            
            return setup.Parse(args).RegisterServices().Build().InvokeAsync();
        }
    }
}
```

This creates a command hierarchy where `manual-command` is a sub-command under `test`:

```bash
# Usage: test manual-command [arguments] [options]
dotnet run -- test manual-command "my text" --name "John"
```

### Adding to Root Level

```csharp
// Adds command directly to root
setup.CommandBuilder.Add(new ManualCommand());
```

### Adding with Parent Key

```csharp
// Adds as sub-command under "tools"
setup.CommandBuilder.AddWithParentKey("tools", new ManualCommand());
```