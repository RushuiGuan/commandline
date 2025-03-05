using Albatross.CommandLine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace Sample.CommandLine {
	[Verb("argument-test", typeof(ArgumentTestCommandHandler))]
	public class ArgumentTestOptions {
		public string Name { get; set; } = string.Empty;

		[Ignore]
		public DateOnly DeadLine { get; set; }//  = new DateOnly(2024, 1, 1);

		[Ignore]
		public int[] Id { get; set; } = Array.Empty<int>();
	}
	public partial class ArgumentTestCommand : IRequireInitialization {
		public void Init() {
			Argument argument = new Argument<DateOnly>("dead-line", "the deadline");
			argument.SetDefaultValue(new DateOnly(2024, 1,1));
			this.AddArgument(argument);

			argument = new Argument<int[]>("id", "The id of the item") {
				IsHidden = false,
			};
			this.AddArgument(argument);
		}
	}
	public class ArgumentTestCommandHandler : BaseHandler<ArgumentTestOptions> {
		private readonly ILogger logger;

		public ArgumentTestCommandHandler(ILogger logger, IOptions<ArgumentTestOptions> options) : base(options) {
			this.logger = logger;
		}
		public override int Invoke(InvocationContext context) {
			logger.LogInformation("{@options}", this.options);
			return 0;
		}
	}
}
