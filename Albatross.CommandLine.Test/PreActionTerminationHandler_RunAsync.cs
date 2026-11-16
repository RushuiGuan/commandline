using System.CommandLine;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Albatross.CommandLine.Test {
	public class PreActionTerminationHandler_RunAsync {
		static readonly TimeSpan Timeout = TimeSpan.FromMilliseconds(200);

		[Fact]
		public async Task NotTerminated_ReturnsResult() {
			using var handler = new PreActionTerminationHandler(CancellationToken.None, Timeout);
			Assert.Equal(42, await handler.RunAsync(async token => { await Task.Delay(10, token); return 42; }));
		}

		[Fact]
		public async Task Terminated_CooperativeAction_ObservesCancellation() {
			using var handler = new PreActionTerminationHandler(CancellationToken.None, TimeSpan.FromSeconds(30));
			var observed = false;
			var task = handler.RunAsync<int>(async token => {
				try {
					await Task.Delay(TimeSpan.FromSeconds(30), token);
				} catch (OperationCanceledException) {
					observed = true;
					throw;
				}
				return 0;
			});
			handler.Terminate();
			var sw = Stopwatch.StartNew();
			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
			Assert.True(observed);
			// well inside the 30s timeout: the action stopped on its own
			Assert.True(sw.Elapsed < TimeSpan.FromSeconds(5), $"took {sw.Elapsed}");
		}

		[Fact]
		public async Task Terminated_ActionIgnoresToken_AbandonedAfterTimeout() {
			using var handler = new PreActionTerminationHandler(CancellationToken.None, Timeout);
			var task = handler.RunAsync<int>(async _ => { await Task.Delay(TimeSpan.FromSeconds(30)); return 0; });
			var sw = Stopwatch.StartNew();
			handler.Terminate();
			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
			Assert.True(sw.Elapsed >= Timeout - TimeSpan.FromMilliseconds(50), $"took {sw.Elapsed}");
			Assert.True(sw.Elapsed < TimeSpan.FromSeconds(5), $"took {sw.Elapsed}");
		}

		[Fact]
		public async Task OuterTokenCancelled_CancelsAction() {
			using var outer = new CancellationTokenSource();
			using var handler = new PreActionTerminationHandler(outer.Token, Timeout);
			var task = handler.RunAsync(async token => { await Task.Delay(TimeSpan.FromSeconds(30), token); return 0; });
			outer.Cancel();
			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
		}

		[Fact]
		public async Task NoTerminationTimeout_RunsWithOriginalToken() {
			var parseResult = new RootCommand().Parse("");
			parseResult.InvocationConfiguration.ProcessTerminationTimeout = null;
			using var outer = new CancellationTokenSource();
			CancellationToken received = default;
			await PreActionTerminationHandler.RunAsync(parseResult, outer.Token, token => { received = token; return Task.FromResult(0); });
			Assert.Equal(outer.Token, received);
		}

		[Fact]
		public async Task TerminationTimeoutConfigured_RunsWithLinkedToken() {
			var parseResult = new RootCommand().Parse("");
			parseResult.InvocationConfiguration.ProcessTerminationTimeout = Timeout;
			using var outer = new CancellationTokenSource();
			CancellationToken received = default;
			await PreActionTerminationHandler.RunAsync(parseResult, outer.Token, token => { received = token; return Task.FromResult(0); });
			Assert.NotEqual(outer.Token, received);
		}
	}
}
