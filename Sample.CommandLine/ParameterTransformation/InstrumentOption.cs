using Albatross.CommandLine;
using Albatross.CommandLine.Annotations;
using Sample.CommandLine.Services;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine.ParameterTransformation {
	[DefaultNameAliases("--instrument", "-i")]
	[OptionHandler<InstrumentOption, InstrumentOptionHandler, InstrumentSummary>]
	public class InstrumentOption : Option<string> {
		public InstrumentOption(string name, params string[] alias) : base(name, alias) {
			this.Description = "The security instrument identifier";
		}
	}

	public class InstrumentOptionHandler : IAsyncOptionHandler<InstrumentOption, InstrumentSummary> {
		private readonly InstrumentService instrumentProxy;

		public InstrumentOptionHandler(InstrumentService instrumentProxy) {
			this.instrumentProxy = instrumentProxy;
		}

		public async Task<OptionHandlerResult<InstrumentSummary>> InvokeAsync(InstrumentOption option, ParseResult result, CancellationToken cancellationToken) {
			var text = result.GetValue(option);
			if (string.IsNullOrEmpty(text)) {
				return new OptionHandlerResult<InstrumentSummary>();
			} else {
				var data = await instrumentProxy.GetSummary(text, cancellationToken);
				return new OptionHandlerResult<InstrumentSummary>(data);
			}
		}
	}
}