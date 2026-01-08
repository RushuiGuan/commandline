using Albatross.CommandLine;
using Albatross.CommandLine.Annotations;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine.ParameterTransformation {
	[Verb<GetInstrumentPrice>("price", Description = "Get price of an instrument")]
	public record class GetInstrumentPriceParams {
		[UseOption<InstrumentIdOption>]
		public required int InstrumentId { get; init; }
	}
	public class GetInstrumentPrice : BaseHandler<GetInstrumentPriceParams> {
		public GetInstrumentPrice(ParseResult result, GetInstrumentPriceParams parameters) : base(result, parameters) {
		}

		public override Task<int> InvokeAsync(CancellationToken cancellationToken) {
			this.Writer.WriteLine($"Parameter: {this.parameters}");
			return Task.FromResult(0);
		}
	}
}