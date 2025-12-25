using Albatross.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine {
	[Verb<TestGracefulShutdown>("test graceful-shutdown", Description = "Use this command to verify graceful shutdown behavior")]
	public record class TestGracefulShutdownOptions {
	}

	public class TestGracefulShutdown : CommandHandler<TestGracefulShutdownOptions> {
		private readonly IMyService myService;

		public TestGracefulShutdown(IMyService myService, TestGracefulShutdownOptions options) : base(options) {
			this.myService = myService;
		}

		public override async Task<int> InvokeAsync(CancellationToken cancellationToken) {
			await myService.DoSomething();
			await this.Writer.WriteLineAsync("TestGracefulShutdown started. Press Ctrl+C to trigger cancellation.");
			// Simulate long-running work
			for (int i = 0; i < 10; i++) {
				cancellationToken.ThrowIfCancellationRequested();
				await this.Writer.WriteLineAsync($"Working... {i + 1}/10");
				await Task.Delay(1000, cancellationToken);
			}
			await this.Writer.WriteLineAsync("TestGracefulShutdown completed successfully.");
			return 0;
		}
	}
}