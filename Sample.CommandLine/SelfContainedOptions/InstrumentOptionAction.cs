using Albatross.CommandLine.Experimental;
using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine.SelfContainedOptions {
	public class InstrumentOptionHandler :IAsyncArgumentHandler<InstrumentOption>{
		private readonly InstrumentProxy instrumentProxy;

		public InstrumentOptionHandler(InstrumentProxy instrumentProxy) {
			this.instrumentProxy = instrumentProxy;
		}

		public async Task InvokeAsync(InstrumentOption option, ParseResult result, CancellationToken cancellationToken) {
			Console.WriteLine("Invoking InstrumentOptionHandler");
			var text = result.GetRequiredValue(option);
			var summary = await instrumentProxy.GetInstrumentSummary(text, cancellationToken);
			option.Summary = summary;
		}
	}
}