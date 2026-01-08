using Sample.CommandLine.ParameterTransformation;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine.Services {
	public class InstrumentService {
		public async Task<InstrumentSummary> GetSummary(string text, CancellationToken cancellationToken) {
			await Task.Delay(100, cancellationToken);
			if (text == "1") {
				return new InstrumentSummary { Id = 1, Name = "Test Instrument" };
			} else {
				throw new ArgumentException($"Instrument '{text}' not found");
			}
		}

		public async Task<bool> VerifyId(int id) {
			await Task.Delay(50);
			return id == 1;
		}
	}
}