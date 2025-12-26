using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine {
	public interface IAsyncActionHandler<TSymbol, TResult> where TSymbol : Symbol {
		Task<TResult> InvokeAsync(ParseResult result, TSymbol symbol, CancellationToken cancellationToken);
	}

	
	public class SymbolFactory {
		private IServiceProvider serviceProvider;
		public SymbolFactory(IServiceProvider serviceProvider) {
			this.serviceProvider = serviceProvider;
		}
		public SecurityInstrumentOption CreateSecurityInstrumentOption() {
			var option = new SecurityInstrumentOption();
			var handle = serviceProvider.GetRequiredService<SecurityInstrumentOptionAction>();
			option.Action = handle;
			return option;
		}
	}

	public record class InstrumentSummary {
		public required int Id { get; init; }
		public required string Name { get; init; }
	}

	public class SecurityInstrumentOptionAction : AsynchronousCommandLineAction {
		private readonly InstrumentProxy instrumentProxy;

		public SecurityInstrumentOptionAction(InstrumentProxy instrumentProxy) {
			this.instrumentProxy = instrumentProxy;
		}

		public override async Task<int> InvokeAsync(ParseResult result, CancellationToken cancellationToken = new CancellationToken()) {
			if (result.Errors.Count == 0) {
				try {
					const string name = "--instrument";
					var option = result.CommandResult.Command.Options.FirstOrDefault(x => x.Name == name) as SecurityInstrumentOption ?? throw new InvalidOperationException();
					var text = result.GetRequiredValue(option);
					var summary = await instrumentProxy.GetInstrumentSummary(text);
					option.Summary = summary;
				} catch (Exception err) {
					result.CommandResult.AddError($"Error getting instrument summary using value: {err.Message}");
				}
			}
			return 0;
		}
	}

	public class InstrumentProxy {
		public async Task<InstrumentSummary> GetInstrumentSummary(string text) {
			await Task.Delay(10);
			if (text == "1") {
				return new InstrumentSummary { Id = 1, Name = "Test Instrument" };
			} else {
				throw new ArgumentException($"Instrument '{text}' not found");
			}
		}
	}

	public class SecurityInstrumentOption : Option<string> {
		public SecurityInstrumentOption() : base("--instrument", "-i") {
			this.Description = "The security instrument identifier (e.g., ticker symbol, CUSIP, ISIN)";
			this.Required = true;
		}
		public InstrumentSummary Summary { get; set; }
	}
}