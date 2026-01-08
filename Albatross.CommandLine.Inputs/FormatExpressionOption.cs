using Albatross.CommandLine.Annotations;
using Albatross.Expression.Nodes;
using Microsoft.Extensions.Logging;
using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Albatross.CommandLine.Inputs {
	[DefaultNameAliases("--format", "-f")]
	[OptionHandler<FormatExpressionOption, ParseFormatExpression, IExpression>]
	public class FormatExpressionOption : Option<string> {
		public FormatExpressionOption(string name, params string[] aliases) : base(name, aliases) {
			Description = "Specify a format expression to shape the output.  Help@https://github.com/RushuiGuan/text/blob/main/Albatross.Text.CliFormat/cheat-sheet.md";
		}
	}
	public class ParseFormatExpression : IAsyncOptionHandler<FormatExpressionOption, IExpression> {
		private readonly ICommandContext context;
		private readonly ILogger<ParseFormatExpression> logger;

		public ParseFormatExpression(ICommandContext context, ILogger<ParseFormatExpression> logger) {
			this.context = context;
			this.logger = logger;
		}
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