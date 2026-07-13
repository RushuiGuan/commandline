using Albatross.CommandLine;
using Albatross.CommandLine.Annotations;
using Microsoft.Extensions.Logging;
using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine.ParameterPreProcessing {
	// A command with three options, each backed by its own async option handler.
	// Two of the handlers throw an exception; the third completes successfully.
	// Used to verify that exceptions thrown inside option handlers are routed to the
	// registered global ICommandErrorHandler instead of being swallowed into an
	// input-action-error status.
	[Verb<TestOptionHandlerErrors>("test option-error", Description = "Runs three option handlers where two throw and one succeeds.")]
	public record class TestOptionHandlerErrorsParams {
		[UseOption<AlphaOption>]
		public string? Alpha { get; init; }

		[UseOption<BravoOption>]
		public string? Bravo { get; init; }

		[UseOption<CharlieOption>]
		public string? Charlie { get; init; }
	}

	public class TestOptionHandlerErrors : BaseHandler<TestOptionHandlerErrorsParams> {
		public TestOptionHandlerErrors(ParseResult result, TestOptionHandlerErrorsParams parameters) : base(result, parameters) {
		}

		public override Task<int> InvokeAsync(CancellationToken cancellationToken) {
			// If option handlers fail the command handler should be skipped.  This line makes
			// it obvious in the output if the command handler is (incorrectly) reached.
			this.Writer.WriteLine("Command handler executed.");
			return Task.FromResult(0);
		}
	}

	[DefaultNameAliases("--alpha", "-a")]
	[OptionHandler<AlphaOption, AlphaOptionHandler>]
	public class AlphaOption : Option<string> {
		public AlphaOption(string name, params string[] alias) : base(name, alias) {
			this.Description = "Its handler throws an exception.";
			this.DefaultValueFactory = _ => "alpha";
		}
	}
	public class AlphaOptionHandler : IAsyncOptionHandler<AlphaOption> {
		private readonly ILogger<AlphaOptionHandler> logger;
		public AlphaOptionHandler(ILogger<AlphaOptionHandler> logger) {
			this.logger = logger;
		}
		public Task InvokeAsync(AlphaOption option, ParseResult result, CancellationToken cancellationToken) {
			logger.LogDebug("Invoking AlphaOptionHandler");
			throw new InvalidOperationException("alpha option handler failed");
		}
	}

	[DefaultNameAliases("--bravo", "-b")]
	[OptionHandler<BravoOption, BravoOptionHandler>]
	public class BravoOption : Option<string> {
		public BravoOption(string name, params string[] alias) : base(name, alias) {
			this.Description = "Its handler runs successfully.";
			this.DefaultValueFactory = _ => "bravo";
		}
	}
	public class BravoOptionHandler : IAsyncOptionHandler<BravoOption> {
		private readonly ILogger<BravoOptionHandler> logger;
		public BravoOptionHandler(ILogger<BravoOptionHandler> logger) {
			this.logger = logger;
		}
		public Task InvokeAsync(BravoOption option, ParseResult result, CancellationToken cancellationToken) {
			logger.LogDebug("Invoking BravoOptionHandler for value {value}", result.GetValue(option));
			return Task.CompletedTask;
		}
	}

	[DefaultNameAliases("--charlie", "-c")]
	[OptionHandler<CharlieOption, CharlieOptionHandler>]
	public class CharlieOption : Option<string> {
		public CharlieOption(string name, params string[] alias) : base(name, alias) {
			this.Description = "Its handler throws an exception.";
			this.DefaultValueFactory = _ => "charlie";
		}
	}
	public class CharlieOptionHandler : IAsyncOptionHandler<CharlieOption> {
		private readonly ILogger<CharlieOptionHandler> logger;
		public CharlieOptionHandler(ILogger<CharlieOptionHandler> logger) {
			this.logger = logger;
		}
		public Task InvokeAsync(CharlieOption option, ParseResult result, CancellationToken cancellationToken) {
			logger.LogDebug("Invoking CharlieOptionHandler");
			throw new InvalidOperationException("charlie option handler failed");
		}
	}
}
