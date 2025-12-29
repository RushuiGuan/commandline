using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;

namespace Albatross.CommandLine {
	/// <summary>
	/// This is used to set Option and Argument Action.  They are run after parsing as a PreAction
	/// This is marked as internal since it is intended to be used by system only
	/// </summary>
	internal sealed class AsyncCommandParameterHandler<T> : AsynchronousCommandLineAction where T : Symbol {
		private readonly Func<IServiceProvider> serviceFactory;
		private readonly Func<T, ParseResult, CancellationToken, Task> func;
		public override bool Terminating => false;
		public T Symbol { get; }

		public AsyncCommandParameterHandler(T symbol, Func<IServiceProvider> serviceFactory, Func<T, ParseResult, CancellationToken, Task> func) {
			this.Symbol = symbol;
			this.serviceFactory = serviceFactory;
			this.func = func;
		}


		public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken) {
			var serviceProvider = serviceFactory();
			var context = serviceProvider.GetRequiredService<ICommandContext>();
			if (!context.HasParsingError) {
				var logger = serviceProvider.GetRequiredService<ILogger<AsyncCommandParameterHandler<T>>>();
				try {
					logger.LogInformation("Invoking AsyncActionHandler for [ {CommandName} [ {SymbolName} ] ]", parseResult.CommandResult.Command.GetCommandKey(), Symbol.Name);
					await func(this.Symbol, parseResult, cancellationToken);
				} catch (OperationCanceledException err) {
					var msg = $"Operation cancelled while processing argument or option [ {Symbol.Name} ]";
					logger.LogWarning(msg);
					context.SetInputActionStatus(new InputActionStatus(Symbol.Name, false, msg, err));
				} catch (Exception err) {
					var msg = $"Error occurred while processing argument or option [ {Symbol.Name} ]";
					logger.LogError(err, msg);
					context.SetInputActionStatus(new InputActionStatus(Symbol.Name, false, msg, err));
				}
			}
			// this return code has no impact
			return 0;
		}
	}
}