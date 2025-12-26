using Albatross.CommandLine;
using Albatross.CommandLine.Experimental;
using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine.SelfContainedOptions {
	public record class GetInstrumentDetailOptions {
		public required InstrumentSummary Summary { get; init; }
	}

	public class GetInstrumentDetailCommand : Command {
		public GetInstrumentDetailCommand() : base("detail", "Get instrument details") {
			Add(InstrumentOption);
			// InstrumentOption.Action = new AsycArgumentAction((result, token) => {
			// 	Console.WriteLine("I am called");
			// 	return Task.CompletedTask;
			// });
		}

		public InstrumentOption InstrumentOption { get; } = new ();
	}

	public class GetInstrumentDetails : BaseHandler<GetInstrumentDetailOptions> {
		public GetInstrumentDetails(ParseResult result, GetInstrumentDetailOptions options) : base(result, options) {
		}

		public override Task<int> InvokeAsync(CancellationToken cancellationToken) {
			this.Writer.WriteLine($"Instrument: {this.options.Summary}");
			return Task.FromResult(0);
		}
	}
}