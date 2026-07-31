using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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

		const int ErrorExitCode = 1;
		/// <summary>
		/// The root command's key is an empty string, which reads as a hole in a log or error message.  Messages use
		/// this stand-in instead; <see cref="Error.Symbol"/> still carries the real key.
		/// </summary>
		const string RootCommandDisplayKey = "<root>";
		private readonly Func<IServiceProvider> serviceFactory;

		int HandleError(IServiceProvider services, params IEnumerable<Error> err) {
			var errHandler = services.GetService<ICommandErrorHandler>();
			if (errHandler != null) {
				var errorCode = errHandler.Handle(err);
				if (errorCode != null) {
					return errorCode.Value;
				}
			}
			return ErrorExitCode;
		}

		public async Task<int> InvokeAsync(ParseResult result, CancellationToken cancellationToken) {
			var services = this.serviceFactory();
			var context = services.GetRequiredService<ICommandContext>();
			var logger = services.GetRequiredService<ILogger<GlobalCommandAction>>();
			var displayKey = context.Key == CommandBuilder.RootCommandKey ? RootCommandDisplayKey : context.Key;
			logger.LogInformation("Invoking command [{command}]", displayKey);
			var inputActionErrors = context.InputActionErrors.ToArray();
			if (inputActionErrors.Any()) {
				return HandleError(services, inputActionErrors);
			}
			IAsyncCommandHandler? handler = null;
			try {
				handler = services.GetKeyedService<IAsyncCommandHandler>(context.Key);
			} catch (Exception err) {
				var msg = $"Error creating CommandHandler for command {displayKey}";
				logger.LogError(err, msg);
				return HandleError(services, new Error(ErrorSource.ServiceRegistration, context.Key, msg, err));
			}
			if (handler == null) {
				// if the command is a parent command, simply print the help
				if (result.CommandResult.Command.Subcommands.Any() || result.CommandResult.Command is RootCommand) {
					return new HelpAction().Invoke(result);
				} else {
					var msg = $"No CommandHandler is registered for command {displayKey}";
					logger.LogError(msg);
					return HandleError(services, new Error(ErrorSource.ServiceRegistration, context.Key, msg, null));
				}
			} else {
				// v2 inclueds a System.CommandLine.Invocation.DefaultExceptionHandler that could be used here.
				// but we implement our own here to have more control on the exit code as well as output logging behavior.
				try {
					return await handler.InvokeAsync(cancellationToken);
				} catch (OperationCanceledException) {
					var msg = $"Command {displayKey} was cancelled";
					logger.LogWarning(msg);
					return HandleError(services, new Error(ErrorSource.CommandTaskCancellation, context.Key, msg, null));
				} catch (Exception err) {
					var msg = $"Error invoking command {displayKey}";
					logger.LogError(err, msg);
					return HandleError(services, new Error(ErrorSource.CommandHandler, context.Key, msg, err));
				}
			}
		}
	}
}