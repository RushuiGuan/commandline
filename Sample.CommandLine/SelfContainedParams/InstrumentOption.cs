using Albatross.CommandLine;
using Albatross.CommandLine.Annotations;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine.SelfContainedParams {
	[DefaultNameAliases("--instrument", "-i")]
	[OptionHandler(typeof(InstrumentOptionHandler))]
	public class InstrumentOption : Option<string>, IUseContextValue {
		public InstrumentOption(string name, params string[] alias) : base(name, alias) {
			this.Description = "The security instrument identifier (e.g., ticker symbol, CUSIP, ISIN)";
			this.Required = true;
		}
	}

	public class InstrumentOptionHandler : IAsyncOptionHandler<InstrumentOption> {
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