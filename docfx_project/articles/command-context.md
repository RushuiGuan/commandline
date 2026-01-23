# Command Context

The @Albatross.CommandLine.CommandContext class is designed to maintain execution context for the duration of a single command execution. It serves as a communication hub between different parts of your command pipeline, particularly between the option handlers and the command handler.

## Key Features

- **Short-circuiting Commands**: Stop command execution early based on validation or preprocessing results
- **Shared State**: Pass information between option handlers and command handlers
- **Status Tracking**: Monitor the success/failure status of individual components
- **Dependency Injection**: Fully injectable for easy testing and composition

## Primary Use Cases

### 1. Short-Circuiting Command Execution

The most common use case is to stop a command from executing when an option handler detects an invalid state.  

```csharp
public class VerifyApiKeyHandler : IAsyncOptionHandler<ApiKeyOption> {
    private readonly IApiService _apiService;
    private readonly ICommandContext _context;
    
    public VerifyApiKeyHandler(IApiService apiService, ICommandContext context) {
        _apiService = apiService;
        _context = context;
    }
    
    public async Task InvokeAsync(ApiKeyOption option, ParseResult result, CancellationToken cancellationToken) {
        var apiKey = result.GetValueForOption(option);
        
        if (string.IsNullOrEmpty(apiKey)) {
            _context.SetResult(1, new OptionHandlerStatus(
                optionName: option.Name, 
                success: false, 
                message: "API key is required but not provided"
            ));
            return; // Command handler will not execute
        }
        
        var isValid = await _apiService.ValidateKeyAsync(apiKey, cancellationToken);
        if (!isValid) {
            _context.SetResult(1, new OptionHandlerStatus(
                optionName: option.Name, 
                success: false, 
                message: "Invalid API key provided"
            ));
        }
    }
}
```

### 2. Passing Data Between Handlers
While the command context *can* be used to share computed or fetched data between option handlers and command handlers, this approach should be used sparingly. The preferred method is to use the built-in [Input Transformation](option-preprocessing-transformation.md#scenario-2-input-transformation) pattern, which provides a more structured and type-safe way to pass data from option handlers to command handler.

The example below demonstrates the context-based approach:

```csharp
public class LoadConfigHandler : IAsyncOptionHandler<ConfigFileOption> {
    private readonly ICommandContext _context;
    
    public LoadConfigHandler(ICommandContext context) {
        _context = context;
    }
    
    public async Task InvokeAsync(ConfigFileOption option, ParseResult result, CancellationToken cancellationToken) {
        var configPath = result.GetValue(option);
        var config = await LoadConfigurationAsync(configPath);
        // Store the loaded config in the context for the command handler to use
        _context.SetValue(option.Name, config);
    }
}

public class MyCommandHandler : BaseHandler<MyCommandParams> {
    readonly Config _config;
    public MyCommandHandler(ParseResult result, MyCommandParams parameters, ICommandContext context) 
        : base(result, parameters) {
        this._config = context.GetRequiredValue("--config-file");
    }
    
    public override async Task<int> InvokeAsync(CancellationToken cancellationToken) {
        // Use the pre-loaded configuration
        // ...
    }
}
```

### 3. Automatic Resource Disposal

The `CommandContext` implements `IAsyncDisposable` and automatically disposes any values stored via `SetValue` that implement `IDisposable` or `IAsyncDisposable`. This ensures proper cleanup of resources created by option handlers without requiring manual disposal logic.

When the command execution completes, the context iterates through all stored values and disposes them in the following order:
1. Values implementing `IAsyncDisposable` are disposed asynchronously
2. Values implementing only `IDisposable` are disposed synchronously

This pattern is particularly useful for option handlers that create resources like file handles, database connections, or trackers:

```csharp
public class TrackerHandler : IAsyncOptionHandler<TrackerOption, Tracker> {
    public Task<OptionHandlerResult<Tracker>> InvokeAsync(
        TrackerOption symbol,
        ParseResult result,
        CancellationToken cancellationToken) {

        var file = result.GetValue(symbol);
        if (file != null) {
            var tracker = new Tracker(file, StringComparer.OrdinalIgnoreCase);
            // The Tracker implements IDisposable and will be
            // automatically disposed when the command completes
            return Task.FromResult(new OptionHandlerResult<Tracker>(tracker));
        }
        return Task.FromResult(new OptionHandlerResult<Tracker>());
    }
}
```

> [!NOTE]
> You do not need to manually dispose values stored in the `CommandContext`. The disposal is handled automatically when the command execution pipeline completes
```