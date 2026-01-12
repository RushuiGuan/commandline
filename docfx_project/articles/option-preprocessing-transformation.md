# Advanced Options: Pre-processing and Transformation

While `System.CommandLine` allows an `Action` to be attached directly to an option, this approach has limitations: the action cannot use dependency injection, and its execution flow is basic.

`Albatross.CommandLine` enhances this by introducing injectable, asynchronous option handlers. This enables two powerful, advanced patterns for reusable options: **pre-processing/validation** and **input transformation**.

These patterns are enabled by decorating a reusable option class with the `[OptionHandler]` attribute.

## Scenario 1: Pre-processing and Validation

This pattern allows you to run validation logic for an option before the main command handler is ever invoked. This is ideal for checking an input value against a database, a web service, or any other external resource. If validation fails, you can gracefully terminate the command.

**Use Case:** Before executing a command that requires an instrument ID, you want to verify that the ID is valid by checking it against an `IInstrumentService`.

### Implementation

1.  **Create the Reusable Option:**
    Decorate the option with `[OptionHandler]`, specifying the option type itself and the handler type (`VerifyInstrumentId`).

    ```csharp
    [DefaultNameAliases("--instrument", "-i")]
    [OptionHandler<InstrumentOption, VerifyInstrumentId>]
    public class InstrumentOption : Option<int> {
        public InstrumentOption(string name, params string[] aliases): base(name, aliases) {
            Description = "Specify a valid instrument id";
        }
    }
    ```

2.  **Implement the Option Handler:**
    The handler implements `IAsyncOptionHandler<T>`. It uses dependency injection to get the services it needs. Inside `InvokeAsync`, it performs the validation and, if the ID is invalid, it "short-circuits" the command by setting the action status on the `ICommandContext`.

    ```csharp
    public class VerifyInstrumentId : IAsyncOptionHandler<InstrumentOption> {
        private readonly IInstrumentService _service;
        private readonly ICommandContext _context;
        private readonly ILogger<VerifyInstrumentId> _logger;

        public VerifyInstrumentId(IInstrumentService service, ICommandContext context, ILogger<VerifyInstrumentId> logger) {
            _service = service;
            _context = context;
            _logger = logger;
        }

        public async Task InvokeAsync(InstrumentOption option, ParseResult result, CancellationToken cancellationToken) {
            var id = result.GetValueForOption(option);
            if (id != 0) {
                var valid = await _service.IsValidInstrument(id, cancellationToken);
                if (!valid) {
                    _logger.LogError("{id} is not a valid instrument id", id);
                    // Terminate the command because the input is invalid.
                    _context.SetInputActionStatus(new OptionHandlerStatus(option.Name, false, $"{id} is not a valid instrument id"));
                }
            }
        }
    }
    ```

3.  **Use the Option:**
    The command's parameter class can now use this option. The command handler (`GetPriceHandler`) will only execute if the `VerifyInstrumentId` handler completes successfully.

    ```csharp
    [Verb<GetPriceHandler>("price")]
    public class GetPriceParams {
        [UseOption<InstrumentOption>]
        public required int InstrumentId { get; init; }
    }
    ```

## Scenario 2: Input Transformation

This pattern transforms an option's input value (e.g., an ID) into a completely different, more complex object (e.g., a data transfer object) before it's passed to the command handler.

**Use Case:** Instead of passing a simple instrument ID to your command handler, you want to fetch the full `InstrumentSummary` object and pass that instead.

### Implementation

1.  **Create the Reusable Option:**
    This time, the `[OptionHandler]` attribute includes a third generic argument: the **output type** (`InstrumentSummary`).

    ```csharp
    [DefaultNameAliases("--instrument", "-i")]
    [OptionHandler<InstrumentOption, GetInstrumentSummary, InstrumentSummary>]
    public class InstrumentOption : Option<int> {
        public InstrumentOption(string name, params string[] aliases): base(name, aliases) {
            Description = "Specify a valid instrument id";
        }
    }
    ```

2.  **Implement the Transformation Handler:**
    The handler now implements `IAsyncOptionHandler<T, TOut>` and its `InvokeAsync` method returns the transformed object (`Task<InstrumentSummary>`).

    ```csharp
    public class GetInstrumentSummary : IAsyncOptionHandler<InstrumentOption, InstrumentSummary> {
        private readonly IInstrumentService _service;

        public GetInstrumentSummary(IInstrumentService service) {
            _service = service;
        }

        public async Task<InstrumentSummary> InvokeAsync(InstrumentOption option, ParseResult result, CancellationToken cancellationToken) {
            var id = result.GetValueForOption(option);
            // Fetch the data and return the transformed object.
            var summary = await _service.GetInstrument(id, cancellationToken);
            return summary;
        }
    }
    ```

3.  **Use the Option:**
    Crucially, the property in the parameters class is now of the **transformed type** (`InstrumentSummary`). The framework handles the transformation behind the scenes and injects the correct object into your command handler.

    ```csharp
    [Verb<GetPriceHandler>("price")]
    public class GetPriceParams {
        // Note: The property type is now InstrumentSummary, not int.
        [UseOption<InstrumentOption>]
        public required InstrumentSummary Instrument { get; init; }
    }
    ```

### Summary

By using `[OptionHandler]`, you can create highly sophisticated and reusable options that cleanly separate concerns like validation and data fetching from your core command logic, leading to cleaner, more maintainable code.