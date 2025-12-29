using Albatross.CommandLine;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine.SelfContainedOptions {
	[Verb<GetInstrumentDetails>("example instrument detail", Description = "Get details for a specific instrument")]
	public record class GetInstrumentDetailsOptions {
		[UseOption<InstrumentOption>]
		public required InstrumentSummary Summary { get; init; }
	}


	public class GetInstrumentDetails : BaseHandler<GetInstrumentDetailsOptions> {
		public GetInstrumentDetails(ParseResult result, GetInstrumentDetailsOptions options) : base(result, options) {
		}

		public override Task<int> InvokeAsync(CancellationToken cancellationToken) {
			this.Writer.WriteLine($"Instrument: {this.options.Summary}");
			return Task.FromResult(0);
		}
	}
}