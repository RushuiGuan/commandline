using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.CommandLine;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Albatross.CommandLine {
	public class GlobalCommandHandler {
		private readonly IHost host;

		public GlobalCommandHandler(IHost host) {
			this.host = host;
		}

		public async Task<int> InvokeAsync(ParseResult result, CancellationToken cancellationToken) {
			var commandKey = result.GetCommandKey();
			var provider = this.host.Services;
			var globalOptions = provider.GetRequiredService<IOptions<GlobalOptions>>().Value;
			var logger = provider.GetRequiredService<ILogger<GlobalCommandHandler>>();
			ICommandHandler? handler = null;
			try {
				handler = provider.GetKeyedService<ICommandHandler>(commandKey);
			} catch (Exception err) {
				if (globalOptions.ShowStack) {
					logger.LogError(err, "Error creating CommandHandler for Command {command}", commandKey);
				} else {
					logger.LogError("Error creating CommandHandler for Command {command}: {msg}", commandKey, err.Message);
				}
				return 9999;
			}
			if (handler == null) {
				logger.LogError("No CommandHandler is registered for Command {command}", commandKey);
				return 9998;
			} else {
				Stopwatch? stopwatch;
				if (globalOptions.Benchmark) {
					stopwatch = Stopwatch.StartNew();
				} else {
					stopwatch = null;
				}
				try {
					return await handler.InvokeAsync(cancellationToken);
				} catch (Exception err) {
					return HandleCommandException(commandKey, err, logger, globalOptions);
				} finally {
					if (stopwatch != null) {
						stopwatch.Stop();
						logger.LogInformation("Command {command} took {time:#,#0} ms", commandKey, stopwatch.ElapsedMilliseconds);
					}
				}
			}
		}
		public int HandleCommandException(string commandKey, Exception err, ILogger logger, GlobalOptions globalOptions) {
			if (globalOptions.ShowStack) {
				logger.LogError(err, "Error invoking Command {command}", commandKey);
			} else {
				logger.LogError("Error invoking Command {command}: {message}", commandKey, err.Message);
			}
			return 10000;
		}
	}
}