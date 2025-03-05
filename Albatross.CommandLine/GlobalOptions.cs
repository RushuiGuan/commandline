using Microsoft.Extensions.Logging;

namespace Albatross.CommandLine {
	public record class GlobalOptions {
		public LogLevel? Verbosity { get; set; }
		public bool Benchmark { get; set; }
		public bool ShowStack { get; set; }
	}
}