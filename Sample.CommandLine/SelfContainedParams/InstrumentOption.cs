using Albatross.CommandLine;
using Albatross.CommandLine.Annotations;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine.SelfContainedParams {
	[DefaultNameAliases("--instrument", "-i")]
	[OptionHandler<InstrumentOption, InstrumentOptionHandler, InstrumentSummary>]
	public class InstrumentOption : Option<string> {
		public InstrumentOption(string name, params string[] alias) : base(name, alias) {
			this.Description = "The security instrument identifier (e.g., ticker symbol, CUSIP, ISIN)";
			this.Required = true;
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
}