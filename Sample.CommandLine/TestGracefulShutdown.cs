using Albatross.CommandLine;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine {
	[Verb<TestGracefulShutdown>("test graceful-shutdown", Description = "Use this command to verify graceful shutdown behavior")]
	public record class TestGracefulShutdownParams {
	}

	public class TestGracefulShutdown : BaseHandler<TestGracefulShutdownParams> {
		private readonly IMyService myService;

		public TestGracefulShutdown(IMyService myService,ParseResult result, TestGracefulShutdownParams parameters) : base(result, parameters) {
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