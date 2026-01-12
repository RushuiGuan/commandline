using Albatross.CommandLine.Annotations;
using Albatross.Expression.Nodes;
using Microsoft.Extensions.Logging;
using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Albatross.CommandLine.Inputs {
	/// <summary>
	/// A command-line option for specifying a format expression to shape command output.
	/// The expression is parsed and validated before command execution.
	/// </summary>
	[DefaultNameAliases("--format", "-f")]
	[OptionHandler<FormatExpressionOption, ParseFormatExpression, IExpression>]
	public class FormatExpressionOption : Option<string> {
		/// <summary>
		/// Creates a new format expression option with the specified name and aliases.
		/// </summary>
		/// <param name="name">The primary name of the option.</param>
		/// <param name="aliases">Additional aliases for the option.</param>
		public FormatExpressionOption(string name, params string[] aliases) : base(name, aliases) {
			Description = "Specify a format expression to shape the output.  Help@https://github.com/RushuiGuan/text/blob/main/Albatross.Text.CliFormat/cheat-sheet.md";
		}
	}

	/// <summary>
	/// Option handler that parses the format expression string into an <see cref="IExpression"/> and stores it in the command context.
	/// </summary>
	public class ParseFormatExpression : IAsyncOptionHandler<FormatExpressionOption, IExpression> {
		private readonly ICommandContext context;
		private readonly ILogger<ParseFormatExpression> logger;

		/// <summary>
		/// Initializes a new instance of the format expression parser.
		/// </summary>
		public ParseFormatExpression(ICommandContext context, ILogger<ParseFormatExpression> logger) {
			this.context = context;
			this.logger = logger;
		}

		/// <inheritdoc/>
		public Task<OptionHandlerResult<IExpression>> InvokeAsync(FormatExpressionOption symbol, ParseResult parseResult, CancellationToken cancellationToken) {
			var text = parseResult.GetValue(symbol);
			if (!string.IsNullOrEmpty(text)) {
				try {
					var expression = Albatross.Text.CliFormat.Extensions.CreateExpression(text);
					return Task.FromResult(new OptionHandlerResult<IExpression>(expression));
				} catch (Exception err) {
					var msg = $"Invalid Format Expression: {text}; {err.Message}";
					logger.LogError(msg);
					context.SetInputActionStatus(new OptionHandlerStatus(symbol.Name, false, msg, err));
				}
			}
			return Task.FromResult(new OptionHandlerResult<IExpression>());
		}
	}
}