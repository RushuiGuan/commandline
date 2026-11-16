using Albatross.CommandLine;
using Albatross.CommandLine.Annotations;
using Albatross.CommandLine.Outputs;
using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine {
	[OptionHandler<SlowOption, SlowOptionHandler>]
	public class SlowOption : Option<string> {
		public SlowOption(string name, params string[] aliases) : base(name, aliases) {
			Description = "This is an option that will take 10 seconds to run";
		}
	}

	public class SlowOptionHandler : IAsyncOptionHandler<SlowOption> {
		public async Task InvokeAsync(SlowOption symbol, ParseResult result, CancellationToken cancellationToken) {
			try {
				System.Console.WriteLine("Running slow option");
				var value = result.GetValue<string>(symbol);
				if (value == "err") {
					throw new ArgumentException("input is wrong");
				}
				await Task.Delay(10000, cancellationToken);
			} catch (Exception err) {
				throw;
			}
		}
	}

	[Verb<LongRunning>("long-running")]
	public class LongRunningParams {
		[UseOption<SlowOption>]
		public string? Slow { get; init; }
	}

	public class LongRunning : BaseHandler<LongRunningParams> {
		public LongRunning(ParseResult result, LongRunningParams parameters) : base(result, parameters) {
		}

		public override async Task<int> InvokeAsync(CancellationToken cancellationToken) {
			System.Console.WriteLine("Running command");
			await Task.Delay(10000, cancellationToken);
			return 0;
		}
	}
}