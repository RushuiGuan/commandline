using Albatross.CommandLine;
using Albatross.CommandLine.Annotations;
using Albatross.CommandLine.Outputs;
using DevLab.JmesPath.Expressions;
using System.CommandLine;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine {
	[Verb<TestRoot>("")]
	public class TestRootParams {
		[UseOption<Albatross.CommandLine.Inputs.InputFileOption>]
		public required FileInfo File { get; init; }

		[UseOption<QueryOption>]
		public JmesPathExpression? Query{ get; init; }
	}
	public class TestRoot : BaseHandler<TestRootParams> {
		public TestRoot(ParseResult result, TestRootParams parameters) : base(result, parameters) {
		}

		public override Task<int> InvokeAsync(CancellationToken cancellationToken) {
			System.Console.WriteLine("I am here");
			return Task.FromResult(0);
		}
	}
}
