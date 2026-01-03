using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;

namespace Albatross.CommandLine {
	/// <summary>
	/// This is used to set Option Action.  They are run after parsing as a PreAction
	/// This is marked as internal since it is intended to be used by system only
	/// </summary>
	internal sealed class AsyncOptionAction<TOption, THandler> : AsynchronousCommandLineAction
		where TOption : Option
		where THandler : IAsyncOptionHandler<TOption> {
		private readonly Func<IServiceProvider> serviceFactory;
		public override bool Terminating => false;
		public TOption Symbol { get; }

		public AsyncOptionAction(TOption symbol, Func<IServiceProvider> serviceFactory) {
			this.Symbol = symbol;
			this.serviceFactory = serviceFactory;
		}

		public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken) {
			var serviceProvider = serviceFactory();
			var context = serviceProvider.GetRequiredService<ICommandContext>();
			if (!context.HasParsingError && !context.HasShortCircuitOptions) {
				var logger = serviceProvider.GetRequiredService<ILogger<AsyncOptionAction<TOption, THandler>>>();
				try {
					logger.LogInformation("Invoking AsyncActionHandler for [ {CommandName} [ {SymbolName} ] ]", context.Key, Symbol.Name);
					var handler = serviceProvider.GetRequiredService<THandler>();
					await handler.InvokeAsync(this.Symbol, parseResult, cancellationToken);
				} catch (OperationCanceledException err) {
					var msg = $"Operation cancelled while processing argument or option [ {Symbol.Name} ]";
					logger.LogWarning(msg);
					context.SetInputActionStatus(new OptionHandlerStatus(Symbol.Name, false, msg, err));
				} catch (Exception err) {
					var msg = $"Error occurred while processing argument or option [ {Symbol.Name} ]";
					logger.LogError(err, msg);
					context.SetInputActionStatus(new OptionHandlerStatus(Symbol.Name, false, msg, err));
				}
			}
			// this return code has no impact
			return 0;
		}
	}

	internal sealed class AsyncOptionAction<TOption, THandler, TContextValue> : AsynchronousCommandLineAction
		where TOption : Option
		where THandler : IAsyncOptionHandler<TOption, TContextValue>
		where TContextValue : notnull {
		private readonly Func<IServiceProvider> serviceFactory;
		public override bool Terminating => false;
		public TOption Symbol { get; }

		public AsyncOptionAction(TOption symbol, Func<IServiceProvider> serviceFactory) {
			this.Symbol = symbol;
			this.serviceFactory = serviceFactory;
		}

		public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken) {
			var serviceProvider = serviceFactory();
			var context = serviceProvider.GetRequiredService<ICommandContext>();
			if (!context.HasParsingError && !context.HasShortCircuitOptions) {
				var logger = serviceProvider.GetRequiredService<ILogger<AsyncOptionAction<TOption, THandler, TContextValue>>>();
				try {
					logger.LogInformation("Invoking AsyncActionHandler for [ {CommandName} [ {SymbolName} ] ]", context.Key, Symbol.Name);
					var handler = serviceProvider.GetRequiredService<THandler>();
					var result = await handler.InvokeAsync(this.Symbol, parseResult, cancellationToken);
					if (result.HasValue) {
						context.SetValue<TContextValue>(this.Symbol.Name, result.Value);
					}
				} catch (OperationCanceledException err) {
					var msg = $"Operation cancelled while processing argument or option [ {Symbol.Name} ]";
					logger.LogWarning(msg);
					context.SetInputActionStatus(new OptionHandlerStatus(Symbol.Name, false, msg, err));
				} catch (Exception err) {
					var msg = $"Error occurred while processing argument or option [ {Symbol.Name} ]";
					logger.LogError(err, msg);
					context.SetInputActionStatus(new OptionHandlerStatus(Symbol.Name, false, msg, err));
				}
			}
			// this return code has no impact
			return 0;
		}
	}

	public sealed class AsyncOptionAction : AsynchronousCommandLineAction {
		public override bool Terminating => false;
		private readonly Func<ParseResult, CancellationToken, Task<int>> handler;

		public AsyncOptionAction(Func<ParseResult, CancellationToken, Task<int>> handler) {
			this.handler = handler;
		}

		public override Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken = default) {
			return handler(parseResult, cancellationToken);
		}
	}

	public sealed class SyncOptionAction : SynchronousCommandLineAction {
		public override bool Terminating => false;
		private readonly Func<ParseResult, int> handler;
		public SyncOptionAction(Func<ParseResult, int> handler) {
			this.handler = handler;
		}
		public override int Invoke(ParseResult parseResult) {
			return handler(parseResult);
		}
	}
}