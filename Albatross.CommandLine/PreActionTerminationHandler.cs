using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;
#if NET
using System.Runtime.InteropServices;
#endif

namespace Albatross.CommandLine {
	/// <summary>
	/// Gives an async option handler the Ctrl+C handling that System.CommandLine installs only around the command
	/// action (dotnet/command-line-api#2836): SIGINT/SIGTERM cancel the handler's token instead of killing the
	/// process, and a handler that does not stop within <c>ProcessTerminationTimeout</c> is abandoned. Mirrors
	/// System.CommandLine's own ProcessTerminationHandler; remove once a fixed System.CommandLine ships.
	/// </summary>
	internal sealed class PreActionTerminationHandler : IDisposable {
		readonly CancellationTokenSource cancellation;
		readonly TimeSpan timeout;
		readonly TaskCompletionSource<bool> timedOut = new(TaskCreationOptions.RunContinuationsAsynchronously);
#if NET
		readonly PosixSignalRegistration? sigInt, sigTerm;
#endif

		public PreActionTerminationHandler(CancellationToken cancellationToken, TimeSpan timeout) {
			this.cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			this.timeout = timeout;
#if NET
			if (!OperatingSystem.IsAndroid() && !OperatingSystem.IsIOS() && !OperatingSystem.IsTvOS() && !OperatingSystem.IsBrowser()) {
				sigInt = PosixSignalRegistration.Create(PosixSignal.SIGINT, OnPosixSignal);
				sigTerm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, OnPosixSignal);
				return;
			}
#endif
			Console.CancelKeyPress += OnCancelKeyPress;
			AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
		}

		public CancellationToken Token => cancellation.Token;

		/// <summary>Throws <see cref="OperationCanceledException"/> if the action is still running a timeout after termination.</summary>
		public async Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> action) {
			var task = action(Token);
			if (await Task.WhenAny(task, timedOut.Task).ConfigureAwait(false) != task) {
				throw new OperationCanceledException(Token);
			}
			return await task.ConfigureAwait(false);
		}

		internal void Terminate() {
			cancellation.Cancel();
			_ = Task.Delay(timeout).ContinueWith(_ => timedOut.TrySetResult(true), TaskScheduler.Default);
		}

#if NET
		void OnPosixSignal(PosixSignalContext context) {
			context.Cancel = true;
			Terminate();
		}
#endif

		void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e) {
			e.Cancel = true;
			Terminate();
		}

		void OnProcessExit(object? sender, EventArgs e) => Terminate();

		public void Dispose() {
#if NET
			if (sigInt != null) {
				sigInt.Dispose();
				sigTerm!.Dispose();
			} else {
				Console.CancelKeyPress -= OnCancelKeyPress;
				AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
			}
#else
			Console.CancelKeyPress -= OnCancelKeyPress;
			AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
#endif
			cancellation.Dispose();
		}

		/// <summary>
		/// Runs an option handler under termination handling when the invocation configures a
		/// <c>ProcessTerminationTimeout</c>, as System.CommandLine does for the command action; otherwise runs it as is.
		/// </summary>
		public static async Task<T> RunAsync<T>(ParseResult parseResult, CancellationToken cancellationToken, Func<CancellationToken, Task<T>> action) {
			var timeout = parseResult.InvocationConfiguration.ProcessTerminationTimeout;
			if (timeout == null) {
				return await action(cancellationToken).ConfigureAwait(false);
			}
			using var handler = new PreActionTerminationHandler(cancellationToken, timeout.Value);
			return await handler.RunAsync(action).ConfigureAwait(false);
		}
	}
}
