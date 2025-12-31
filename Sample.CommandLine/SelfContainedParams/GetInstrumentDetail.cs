using Albatross.CommandLine;
using Albatross.CommandLine.Annotations;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine.SelfContainedParams {
	[Verb<GetInstrumentDetails>("example instrument detail", Description = "Get details for a specific instrument")]
	public record class GetInstrumentDetailsParams {
		[UseOption<InstrumentOption>]
		public required InstrumentSummary Summary { get; init; }
	}


	public class GetInstrumentDetails : BaseHandler<GetInstrumentDetailsParams> {
		public GetInstrumentDetails(ParseResult result, GetInstrumentDetailsParams parameters) : base(result, parameters) {
		}

		public override Task<int> InvokeAsync(CancellationToken cancellationToken) {
			this.Writer.WriteLine($"Instrument: {this.parameters.Summary}");
			return Task.FromResult(0);
		}
	}
}