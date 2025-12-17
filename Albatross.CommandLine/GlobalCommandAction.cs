using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.CommandLine;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Albatross.CommandLine {
	public class GlobalCommandAction {
		public GlobalCommandAction(Func<IHost> hostFactory) {
			this.hostFactory = hostFactory;
		}
		const int ErrorExitCode = -1;
		private readonly Func<IHost> hostFactory;

		public async Task<int> InvokeAsync(ParseResult result, CancellationToken cancellationToken) {
			var host = this.hostFactory();
			var commandNames = result.CommandResult.Command.GetCommandNames();
			var key = string.Join(' ', commandNames);
			var benchmark = result.GetValue<bool>(CommandBuilder.BenchmarkOptionName);
			var showStack = result.GetValue<bool>(CommandBuilder.ShowStackOptionName);
			var logger = host.Services.GetRequiredService<ILogger<GlobalCommandAction>>();
			ICommandAction? handler = null;
			try {
				handler = host.Services.GetKeyedService<ICommandAction>(key);
			} catch (Exception err) {
				if (showStack == true) {
					logger.LogError(err, "Error creating CommandAction for command {command}", key);
				} else {
					logger.LogError("Error creating CommandAction for command {command}: {msg}", key, err.Message);
				}
				return ErrorExitCode;
			}
			if (handler == null) {
				logger.LogError("No CommandAction is registered for command {command}", key);
				return ErrorExitCode;
			} else {
				Stopwatch? stopwatch;
				if (benchmark == true) {
					stopwatch = Stopwatch.StartNew();
				} else {
					stopwatch = null;
				}
				try {
					return await handler.Invoke(cancellationToken);
				} catch (Exception err) {
					if (showStack == true) {
						logger.LogError(err, "Error invoking command {command}", key);
					} else {
						logger.LogError("Error invoking command {command}: {message}", key, err.Message);
					}
					return ErrorExitCode;
				} finally {
					if (stopwatch != null) {
						stopwatch.Stop();
						logger.LogInformation("Command {command} took {time:#,#0} ms", key, stopwatch.ElapsedMilliseconds);
					}
				}
			}
		}
	}
}