using Albatross.CommandLine;
using Albatross.CommandLine.Annotations;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine {
	[DefaultNameAliases("reusable-array-options")]
	[OptionHandler<ReusableArrayOption, ReusableArrayOptionHandler>]
	public class ReusableArrayOption : Option<string[]> {
		public ReusableArrayOption(string name, params string[] aliases) : base(name, aliases) {
		}
	}

	public class ReusableArrayOptionHandler : IAsyncOptionHandler<ReusableArrayOption> {
		public Task InvokeAsync(ReusableArrayOption symbol, ParseResult result, CancellationToken cancellationToken) {
			throw new System.NotImplementedException();
		}
	}

	[DefaultNameAliases("transformation-array-options")]
	[OptionHandler<TransformationArrayOption, TransformationArrayOptionHandler, int[]>]
	public class TransformationArrayOption : Option<string[]> {
		public TransformationArrayOption(string name, params string[] aliases) : base(name, aliases) {
		}
	}

	public class TransformationArrayOptionHandler : IAsyncOptionHandler<TransformationArrayOption, int[]> {
		public Task<OptionHandlerResult<int[]>> InvokeAsync(TransformationArrayOption symbol, ParseResult result, CancellationToken cancellationToken) {
			throw new System.NotImplementedException();
		}
	}

	[Verb<TestArrayOption>("test array-option", Description = "Test array option")]
	public class TestArrayOptionParams {
		[Option("array", Description = "Array option")]
		public string[] Array { get; set; } = [];

		[UseOption<ReusableArrayOption>]
		public string[] Array2 { get; set; } = [];

		[UseOption<TransformationArrayOption>]
		public int[] Array3 { get; set; } = [];
	}
	public class TestArrayOption : IAsyncCommandHandler {
		public Task<int> InvokeAsync(CancellationToken cancellationToken) {
			return Task.FromResult(0);
		}
	}
}
