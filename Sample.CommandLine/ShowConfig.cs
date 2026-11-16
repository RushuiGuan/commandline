using Albatross.CommandLine;
using Albatross.CommandLine.Annotations;
using Albatross.CommandLine.Outputs;
using Albatross.Config;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine {
	[Verb<ShowConfigHandler>("config", Description = "Show application config")]
	public record class ShowConfigParams {
		[UseOption<CompactOption>]
		public bool Compact { get; init; }
	}

	public class ShowConfigHandler : BaseHandler<ShowConfigParams> {
		private readonly IApplicationPath applicationPath;

		public ShowConfigHandler(ParseResult result, ShowConfigParams parameters, IApplicationPath applicationPath) : base(result, parameters) {
			this.applicationPath = applicationPath;
		}

		public override async Task<int> InvokeAsync(CancellationToken cancellationToken) {
			this.applicationPath.Print(parameters.Compact);
			return 0;
		}
	}
}