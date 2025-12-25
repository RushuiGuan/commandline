using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.CommandLine;
using System.CommandLine.Help;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Albatross.CommandLine {
	public class GlobalCommandHandler {
		public GlobalCommandHandler(Func<IHost> hostFactory) {
			this.hostFactory = hostFactory;
		}

		/// <summary>
		/// Windows exit code is int but unix is only byte, so we use 255 or less so that it will have the same value on both platforms.
		/// </summary>
		const int ErrorExitCode = 255;
		const int CancelledExitCode = 254;

		private readonly Func<IHost> hostFactory;

		public async Task<int> InvokeAsync(ParseResult result, CancellationToken cancellationToken) {
			var host = this.hostFactory();
			var logger = host.Services.GetRequiredService<ILogger<GlobalCommandHandler>>();
			var commandNames = result.CommandResult.Command.GetCommandNames();
			var key = string.Join(' ', commandNames);
			logger.LogInformation("Executing command '{command}'", key);
			ICommandHandler? handler = null;
			try {
				handler = host.Services.GetKeyedService<ICommandHandler>(key);
			} catch (Exception err) {
				logger.LogError(err, "Error creating CommandHandler for command {command}", key);
				return ErrorExitCode;
			}
			if (handler == null) {
				// if the command is a parent command, simply print the help
				if (result.CommandResult.Command.Subcommands.Any()) {
					return new HelpAction().Invoke(result);
				} else {
					logger.LogError("No CommandHandler is registered for command {command}", key);
					return ErrorExitCode;
				}
			} else {
				try {
					return await handler.InvokeAsync(cancellationToken);
				} catch (OperationCanceledException) {
					logger.LogWarning("Command {command} was cancelled", key);
					return CancelledExitCode;
				} catch (Exception err) {
					logger.LogError(err, "Error invoking command {command}", key);
					return ErrorExitCode;
				}
			}
		}
	}
}