using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.CommandLine;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Albatross.CommandLine {
	public class GlobalCommandHandler : ICommandHandler {
		protected readonly Command command;
		protected readonly string key;

		public GlobalCommandHandler(Command command) {
			this.command = command;
			this.key = command.GetKey();
		}

		public int Invoke(InvocationContext context) => Invoke(context, false).Result;
		public Task<int> InvokeAsync(InvocationContext context) => Invoke(context, true);

		public virtual int HandleCommandException(Exception err, ILogger logger, GlobalOptions globalOptions) {
			if (globalOptions.ShowStack) {
				logger.LogError(err, "Error invoking Command {command}", key);
			} else {
				logger.LogError("Error invoking Command {command}: {message}", key, err.Message);
			}
			return 10000;
		}

		public async Task<int> Invoke(InvocationContext context, bool async) {
			var provider = context.GetHost().Services;
			var globalOptions = provider.GetRequiredService<IOptions<GlobalOptions>>().Value;
			var logger = provider.GetRequiredService<ILogger<GlobalCommandHandler>>();
			ICommandHandler? handler = null;
			try {
				handler = provider.GetKeyedService<ICommandHandler>(key);
			} catch (Exception err) {
				if (globalOptions.ShowStack) {
					logger.LogError(err, "Error creating CommandHandler for Command {command}", key);
				} else {
					logger.LogError("Error creating CommandHandler for Command {command}: {msg}", key, err.Message);
				}
				return 9999;
			}
			if (handler == null) {
				logger.LogError("No CommandHandler is registered for Command {command}", key);
				return 9998;
			} else {
				Stopwatch? stopwatch;
				if (globalOptions.Benchmark) {
					stopwatch = Stopwatch.StartNew();
				} else {
					stopwatch = null;
				}
				try {
					if (async) {
						return await handler.InvokeAsync(context);
					} else {
						return handler.Invoke(context);
					}
				} catch (Exception err) {
					return HandleCommandException(err, logger, globalOptions);
				} finally {
					if (stopwatch != null) {
						stopwatch.Stop();
						logger.LogInformation("Command {command} took {time:#,#0} ms", command.Name, stopwatch.ElapsedMilliseconds);
					}
				}
			}
		}
	}
}