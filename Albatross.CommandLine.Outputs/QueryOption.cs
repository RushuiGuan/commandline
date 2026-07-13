using Albatross.CommandLine.Annotations;
using DevLab.JmesPath;
using DevLab.JmesPath.Expressions;
using Microsoft.Extensions.Logging;
using System.CommandLine;

namespace Albatross.CommandLine.Outputs {
	[DefaultNameAliases("--query", "-q")]
	[OptionHandler<QueryOption, ParseQueryExpression, JmesPathExpression>]
	public class QueryOption : Option<string> {
		public QueryOption(string name, params string[] aliases) : base(name, aliases) {
			Description = "JmesPath expression.  Find documentation@https://jmespath.org/";
		}
	}

	public class ParseQueryExpression : IAsyncOptionHandler<QueryOption, JmesPathExpression> {
		private readonly ICommandContext context;
		private readonly ILogger<ParseQueryExpression> logger;

		/// <summary>
		/// Initializes a new instance of the format expression parser.
		/// </summary>
		public ParseQueryExpression(ICommandContext context, ILogger<ParseQueryExpression> logger) {
			this.context = context;
			this.logger = logger;
		}

		/// <inheritdoc/>
		public Task<OptionHandlerResult<JmesPathExpression>> InvokeAsync(QueryOption symbol, ParseResult parseResult, CancellationToken cancellationToken) {
			var text = parseResult.GetValue(symbol);
			if (!string.IsNullOrEmpty(text)) {
				try {
					var expression = new JmesPath().Parse(text); // throws on invalid syntax
					return Task.FromResult(new OptionHandlerResult<JmesPathExpression>(expression));
				} catch (Exception err) {
					var msg = $"Invalid Jmes Query Expression: {text}";
					logger.LogError(msg, err);
					context.SetInputActionError(new Error(ErrorSource.OptionHandler, symbol.Name, msg, err));
				}
			}
			return Task.FromResult(new OptionHandlerResult<JmesPathExpression>());
		}
	}
}