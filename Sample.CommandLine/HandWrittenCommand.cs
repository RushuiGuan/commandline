using Albatross.CommandLine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine {
	[Verb("argument-test", typeof(ArgumentTestCommandHandler))]
	public class ArgumentTestOptions {
		[Option]
		public string Name { get; set; } = string.Empty;

		public DateOnly DeadLine { get; set; }//  = new DateOnly(2024, 1, 1);

		public int[] Id { get; set; } = Array.Empty<int>();
	}
	public partial class ArgumentTestCommand  {
		partial void Initialize() {
			Argument argument = new Argument<DateOnly>("dead-line") {
				Description = "the deadline",
				DefaultValueFactory = _ => new DateOnly(2024, 1, 1)
			};
			this.Add(argument);

			argument = new Argument<int[]>("id"){
				Description = "The id of the item",
				Hidden = false,
			};
			this.Add(argument);
		}
	}
	public class ArgumentTestCommandHandler : BaseHandler<ArgumentTestOptions> {
		private readonly ILogger logger;

		public ArgumentTestCommandHandler(ILogger logger, IOptions<ArgumentTestOptions> options) : base(options) {
			this.logger = logger;
		}
		public override Task<int> Invoke(CancellationToken token) {
			logger.LogInformation("{@options}", this.options);
			return Task.FromResult(0);
		}
	}
}
