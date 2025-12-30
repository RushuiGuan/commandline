using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.CommandLine;
using System.CommandLine.Help;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Albatross.CommandLine {
	internal sealed class GlobalCommandAction {
		public GlobalCommandAction(Func<IServiceProvider> serviceFactory) {
			this.serviceFactory = serviceFactory;
		}

		/// <summary>
		/// Windows exit code is int but unix is only byte, so we use 255 or less so that it will have the same value on both platforms.
		/// </summary>
		const int ErrorExitCode = 255;
		const int CancelledExitCode = 254;
		const int InputActionErrorExitCode = 253;

		private readonly Func<IServiceProvider> serviceFactory;

		public async Task<int> InvokeAsync(ParseResult result, CancellationToken cancellationToken) {
			var services = this.serviceFactory();
			var context = services.GetRequiredService<ICommandContext>();
			if (context.HasInputActionError) {
				return InputActionErrorExitCode;
			}
			var logger = services.GetRequiredService<ILogger<GlobalCommandAction>>();
			logger.LogInformation("Executing command '{command}'", context.Key);
			IAsyncCommandHandler? handler = null;
			try {
				handler = services.GetKeyedService<IAsyncCommandHandler>(context.Key);
			} catch (Exception err) {
				logger.LogError(err, "Error creating CommandHandler for command {command}", context.Key);
				return ErrorExitCode;
			}
			if (handler == null) {
				// if the command is a parent command, simply print the help
				if (result.CommandResult.Command.Subcommands.Any()) {
					return new HelpAction().Invoke(result);
				} else {
					logger.LogError("No CommandHandler is registered for command {command}", context.Key);
					return ErrorExitCode;
				}
			} else {
				// v2 inclueds a System.CommandLine.Invocation.DefaultExceptionHandler that could be used here.
				// but we implement our own here to have more control on the exit code as well as output logging behavior.
				try {
					return await handler.InvokeAsync(cancellationToken);
				} catch (OperationCanceledException) {
					logger.LogWarning("Command {command} was cancelled", context.Key);
					return CancelledExitCode;
				} catch (Exception err) {
					logger.LogError(err, "Error invoking command {command}", context.Key);
					return ErrorExitCode;
				}
			}
		}
	}
}