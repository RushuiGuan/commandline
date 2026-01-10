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