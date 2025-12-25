using Albatross.CommandLine;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine {
	[Verb<ExampleProjectCommandHandler>("example project echo", UseBaseOptionsClass = typeof(SharedProjectOptions), Description = "This demonstrates the use of mutually exclusive commands using inheritance.")]
	public record class ProjectEchoOptions : SharedProjectOptions {
		[Option]
		public required int Echo { get; init; }
	}

	[Verb<ExampleProjectCommandHandler>("example project fubar", UseBaseOptionsClass = typeof(SharedProjectOptions), Description = "This demonstrates the use of mutually exclusive commands using inheritance.")]
	public record class ProjectFubarOptions : SharedProjectOptions {
		[Option]
		public required int Fubar { get; init; }
	}

	public record class SharedProjectOptions {
		[Option]
		public required int Id { get; init; }
	}

	public class ExampleProjectCommandHandler : CommandHandler<SharedProjectOptions> {
		public ExampleProjectCommandHandler(SharedProjectOptions options) : base(options) {
		}

		public override Task<int> InvokeAsync(CancellationToken cancellationToken) {
			if (options is ProjectEchoOptions echoOptions) {
				this.Writer.WriteLine($"Invoked project echo: {echoOptions}");
			} else if (options is ProjectFubarOptions fubarOptions) {
				this.Writer.WriteLine($"Invoked project fubar: {fubarOptions}");
			} else {
				throw new NotSupportedException($"Unsupported options: {options}");
			}
			return Task.FromResult(0);
		}
	}
}