using Albatross.CommandLine;
using Albatross.CommandLine.Annotations;
using Albatross.CommandLine.Inputs;
using Albatross.Expression.Nodes;
using Albatross.Text.CliFormat;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine {
	[Verb<TestFormatExpression>("test format")]
	public record class TestFormatExpressionParams {
		[UseOption<FormatExpressionOption>]
		public IExpression? Format { get; init; }
	}
	public class TestFormatExpression : IAsyncCommandHandler {
		private readonly TestFormatExpressionParams parameters;

		public TestFormatExpression(TestFormatExpressionParams parameters) {
			this.parameters = parameters;
		}

		public Task<int> InvokeAsync(CancellationToken cancellationToken) {
			var data = new {
				Name = "Sample",
				Value = 123.456,
				Date = DateTime.Now
			};
			Console.Out.CliPrintWithExpression(data, parameters.Format);
			return Task.FromResult(0);
		}
	}
}
