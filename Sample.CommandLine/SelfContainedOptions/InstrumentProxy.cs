using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine.SelfContainedParams {
	public class InstrumentProxy {
		public async Task<InstrumentSummary> GetInstrumentSummary(string text, CancellationToken cancellationToken) {
			await Task.Delay(10, cancellationToken);
			if (text == "1") {
				return new InstrumentSummary { Id = 1, Name = "Test Instrument" };
			} else {
				throw new ArgumentException($"Instrument '{text}' not found");
			}
		}
	}
}