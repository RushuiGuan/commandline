using Albatross.CommandLine;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine.SelfContainedOptions {
	[DefaultActionHandler(typeof(InstrumentOptionHandler))]
	public class InstrumentOption : Option<string> {
		public InstrumentOption() : base("--instrument", "-i") {
			this.Description = "The security instrument identifier (e.g., ticker symbol, CUSIP, ISIN)";
			this.Required = true;
		}
	}

	public class InstrumentOptionHandler : IAsyncCommandParameterHandler<InstrumentOption> {
		private readonly ICommandContext context;
		private readonly InstrumentProxy instrumentProxy;

		public InstrumentOptionHandler(ICommandContext context, InstrumentProxy instrumentProxy) {
			this.context = context;
			this.instrumentProxy = instrumentProxy;
		}

		public async Task InvokeAsync(InstrumentOption option, ParseResult result, CancellationToken cancellationToken) {
			var text = result.GetRequiredValue(option);
			var summary = await instrumentProxy.GetInstrumentSummary(text, cancellationToken);
			context.SetValue(option.Name, summary);
		}
	}
}