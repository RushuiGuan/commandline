using Albatross.CommandLine;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine {
	[Verb<TestSelfContainedPattern>("test self-contained", Description = "Test command using self-contained pattern")]
	public class TestSelfContainedPatternOptions {
		public InstrumentSummary Instrument { get; }
	}
	
	public class TestSelfContainedPattern : CommandHandler<TestSelfContainedPatternOptions> {
		public TestSelfContainedPattern(ParseResult result, TestSelfContainedPatternOptions options) : base(result, options) {
		}

		public override Task<int> InvokeAsync(CancellationToken cancellationToken) {
			throw new System.NotImplementedException();
		}
	}
}