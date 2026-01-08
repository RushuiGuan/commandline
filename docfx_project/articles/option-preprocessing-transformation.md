# Advanced Scenarios - Option Action Handler
Options can be always be created directly with an Action.  But it has a couple limitations:
1. The action cannot use dependency injection.
2. The Terminating property (short circuit flag) of the action cannot be changed during execution.
`Albatross.CommandLine` supports async action handle that can handle both.  

## PreProcessing Use Case
```csharp
[DefaultNameAliases("--instrument", "-i")]
[OptionHandler<InstrumentOption, VerifyInstrumentId>]
public class IntrumentOption : Option<int> {
	public InstrumentOption(string name, params string[] aliases): base(name, aliases) {
		Description = "Specify an valid instrument id";
	}
}
public class VerifyInstrumentId : IAsyncOptionHandler<InstrumentOption> {
	readonly IInstrumentService service;
	readonly ICommandContext context;
	readonly ILogger logger;
	public VerifyInstrumentId(IInstrumentService service, ICommandContext context, ILogger<VerifyInstrumentId> logger) {
		this.service = service;
		this.context = context;
		this.logger = logger;
	}
	public async Task InvokeAsync(InstrumentOption option, ParseResult result, CancellationToken cancellationToken) {
		var id = result.GetValue(option);
		if(id != 0) {
			var valid = await service.IsValidInstrument(id, cancellationToken);
			if(!valid) {
				logger.LogError("{id} is not a valid instrument id", id);
				// shortcircuit the command here since input id is not valid
				context.SetInputActionStatus(new OptionHandlerStatus(option.Name, false, $"{id} is not a valid instrument id"$, null));
			}
		}
	}
}
[Verb<GetPriceHandler>]("price")
public class GetPriceParams {
    [UseOption<InstrumentOption>]
    public required int InstrumentId { get; init; }
}
```


## Transformation Use Case

```csharp
[DefaultNameAliases("--instrument", "-i")]
[OptionHandler<InstrumentOption, GetInstrumentSummary, InstrumentSummary>]
public class IntrumentOption : Option<int> {
	public InstrumentOption(string name, params string[] aliases): base(name, aliases) {
		Description = "Specify an valid instrument id";
	}
}
public class GetInstrumentSummary : IAsyncOptionHandler<InstrumentOption, InstrumentSummary> {
	readonly IInstrumentService service;
	readonly ICommandContext context;
	readonly ILogger logger;
	public GetInstrumentSummary(IInstrumentService service, ICommandContext context, ILogger<VerifyInstrumentId> logger) {
		this.service = service;
		this.context = context;
		this.logger = logger;
	}
	public async Task<InstrumentSummary> InvokeAsync(InstrumentOption option, ParseResult result, CancellationToken cancellationToken) {
		var id = result.GetValue(option);
		if(id != 0) {
			var summary = await service.GetInstrument(id, cancellationToken);
            return summary;
		}
	}
}

[Verb<GetPriceHandler>]("price")
public class GetPriceParams {
    [UseOption<InstrumentOption>]
    public required InstrumentSummary Instrument { get; init; }
}
```