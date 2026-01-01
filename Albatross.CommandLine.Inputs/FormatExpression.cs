using Albatross.CommandLine.Annotations;
using Albatross.Expression.Parsing;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Albatross.CommandLine.Inputs {
	/*
	[DefaultOptionHandler(typeof(ParseFormatExpression))]
	public class FormatExpressionOption : Option<string>, IUseContextValue{
		public FormatExpressionOption(string name, params string[] aliases) : base(name, aliases) {
		}
		public FormatExpressionOption() : this("--format", "-f") {
		}
	}
	public class ParseFormatExpression: IAsyncOptionHandler<FormatExpressionOption> {
		private readonly ICommandContext context;
		public ParseFormatExpression(ICommandContext context) {
			this.context = context;
		}
		public Task InvokeAsync(FormatExpressionOption symbol, ParseResult result, CancellationToken cancellationToken) {
			var text = result.GetValue(symbol);
			if (!string.IsNullOrEmpty(text)) {
				var parser = Albatross.Text.CliFormat.Extensions.BuildCustomParser();
				var tree = parser.Build(text);
			}
		}
	}
	*/
}