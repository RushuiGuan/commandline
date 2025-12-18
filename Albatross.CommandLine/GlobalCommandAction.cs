using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.CommandLine;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Albatross.CommandLine {
	public class GlobalCommandAction {
		public GlobalCommandAction(Func<IHost> hostFactory) {
			this.hostFactory = hostFactory;
		}

		/// <summary>
		/// Windows exit code is int but unix is only byte, so we use 255 so that it will have the same value on both platforms.
		/// </summary>
		const int ErrorExitCode = 255;
		private readonly Func<IHost> hostFactory;

		public async Task<int> InvokeAsync(ParseResult result, CancellationToken cancellationToken) {
			var host = this.hostFactory();
			var logger = host.Services.GetRequiredService<ILogger<GlobalCommandAction>>();
			var commandNames = result.CommandResult.Command.GetCommandNames();
			var key = string.Join(' ', commandNames);
			logger.LogInformation("Executing command '{command}'", key);
			ICommandAction? handler = null;
			try {
				handler = host.Services.GetKeyedService<ICommandAction>(key);
			} catch (Exception err) {
				logger.LogError(err, "Error creating CommandAction for command {command}", key);
				return ErrorExitCode;
			}
			if (handler == null) {
				// if the command is a parent command, simply print the help
				if (result.CommandResult.Command.Subcommands.Any()) {
					return HelpCommandAction.Invoke(result);
				} else {
					logger.LogError("No CommandAction is registered for command {command}", key);
					return ErrorExitCode;
				}
			} else {
				try {
					return await handler.Invoke(cancellationToken);
				} catch (Exception err) {
					logger.LogError(err, "Error invoking command {command}", key);
					return ErrorExitCode;
				}
			}
		}
	}
}