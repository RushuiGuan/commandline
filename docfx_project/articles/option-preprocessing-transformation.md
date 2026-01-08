# Advanced Scenarios - Option Action Handler
Options can be always be created directly with an Action.  But it has a couple limitations:
1. The action cannot use dependency injection.
2. The Terminating property (short circuit flag) of the action cannot be changed during execution.
`Albatross.CommandLine` supports async action handle that can handle both.  

## How it Works
```mermaid

```
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
public class InstrumentOptionHandler : IAsyncOptionHandler<InstrumentOption, InstrumentSummary> {
		private readonly ICommandContext context;
		private readonly InstrumentProxy instrumentProxy;

		public InstrumentOptionHandler(ICommandContext context, InstrumentProxy instrumentProxy) {
			this.context = context;
			this.instrumentProxy = instrumentProxy;
		}

		public async Task<OptionHandlerResult<InstrumentSummary>> InvokeAsync(InstrumentOption option, ParseResult result, CancellationToken cancellationToken) {
			var text = result.GetValue(option);
			if (string.IsNullOrEmpty(text)) {
				return new OptionHandlerResult<InstrumentSummary>();
			} else {
				var data = await instrumentProxy.GetInstrumentSummary(text, cancellationToken);
				return new OptionHandlerResult<InstrumentSummary>(data);
			}
		}
	}
```
## Transformation Use Case