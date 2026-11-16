using Albatross.CommandLine;
using Albatross.CommandLine.Annotations;
using Albatross.CommandLine.Outputs;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine {
	[Verb<TestInterruptableOutput>("test interrutable-output", Description = "Verify that control-c can interrupt large print out")]
	public record class TestInterruptableOutputParams {
		[Argument(DefaultToInitializer = true)]
		public int Count { get; init; } = 10000;

		[UseOption<CompactOption>]
		public bool Compact { get; init; }
	}

	public class TestInterruptableOutput : BaseHandler<TestInterruptableOutputParams> {
		public record TestData {
			public int Value { get; set; }
			public required string Text { get; init; }
		}


		public TestInterruptableOutput(ParseResult result, TestInterruptableOutputParams parameters) : base(result, parameters) {
		}

		public override async Task<int> InvokeAsync(CancellationToken cancellationToken) {
			var items = new List<TestData>();
			for (int i = 0; i < parameters.Count; i++) {
				items.Add(new TestData {
					Value = Random.Shared.Next(),
					Text = Guid.NewGuid().ToString(),
				});
			}

			await items.PrintAsync(parameters.Compact, cancellationToken, false);
			return 0;
		}
	}
}