using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Albatross.CommandLine.Experimental {
	/// <summary>
	/// This is used to set Option and Argument Action.  They are run after parsing as a PreAction
	/// </summary>
	internal sealed class AsyncActionHandler<T> : AsynchronousCommandLineAction where T : Symbol {
		private readonly Func<IServiceProvider> serviceLocator;
		private readonly Func<ParseResult, CancellationToken, Task> func;
		public override bool Terminating => false;
		public T Symbol { get; }

		public AsyncActionHandler(T symbol, Func<IServiceProvider> serviceLocator, Func<ParseResult, CancellationToken, Task> func) {
			this.Symbol = symbol;
			this.serviceLocator = serviceLocator;
			this.func = func;
		}


		public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken) {
			if (!parseResult.Errors.Any()) {
				var logger = serviceLocator().GetRequiredService<ILogger<AsyncActionHandler<T>>>();
				try {
					logger.LogInformation("Invoking AsyncActionHandler for [ {CommandName} [ {SymbolName} ] ]", parseResult.CommandResult.Command.GetCommandKey(), Symbol.Name);
					await func(parseResult, cancellationToken);
				} catch (Exception err) {
					logger.LogError();
				}
			}
			// this return code has no impact
			return 0;
		}
	}
}