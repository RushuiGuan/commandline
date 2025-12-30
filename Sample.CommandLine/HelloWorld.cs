using Albatross.CommandLine;
using Albatross.CommandLine.Annotations;
using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine {
	[Verb<HelloWorldBaseHandler>("hello", Description = "The HelloWorld command")]
	public record class HelloWorldParams {
		[Argument(Description = "The order of declaration determines the position of the argument")]
		public required string Argument1 { get; init; }

		[Argument(Description = "Optional arguments should be put after the required ones")]
		public string? Argument2 { get; init; }

		[Option(Description = "By default, nullability of the property is used to determine if the option is required")]
		public required string Name { get; init; }

		[Option(Description = "Same goes for the value types")]
		public required int Value { get; init; }

		[Option(Description = "But Required flag can be set to override the behavior", Required = false)]
		public decimal NumericValue { get; init; }

		[Option(DefaultToInitializer = true, Description = "Set DefautlToInitializer to true to use the property initializer as the default value.  If true, the option is not required.")]
		public DateOnly Date { get; init; } = DateOnly.FromDateTime(DateTime.Today);
	}

	/// <summary>
	/// Customize the command behavior by implementing the partial method Initialize
	/// </summary>
	public partial class HelloWorldCommand {
		partial void Initialize() {
			this.Option_Date.Validators.Add(r => {
				var value = r.GetValue(this.Option_Date);
				if(value < DateOnly.FromDateTime(DateTime.Today)) {
					r.AddError($"Invalid value {value:yyyy-MM-dd} for Date.  It cannot be in the past");
				}
			});
		}
	}

	public class HelloWorldBaseHandler : BaseHandler<HelloWorldParams> {
		public HelloWorldBaseHandler(ParseResult result, HelloWorldParams parameters) : base(result, parameters) {
		}

		public override async Task<int> InvokeAsync(CancellationToken cancellationToken) {
			await this.Writer.WriteLineAsync(parameters.ToString());
			return 0;
		}
	}
}