using Albatross.CommandLine;
using Albatross.CommandLine.Annotations;
using Sample.CommandLine.Services;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine.ParameterTransformation {
	[DefaultNameAliases("--instrument", "-i")]
	[OptionHandler<InstrumentIdOption, InstrumentIdOptionHandler>]
	public class InstrumentIdOption : Option<int> {
		public InstrumentIdOption(string name, params string[] alias) : base(name, alias) {
			this.Description = "An instrument id";
			this.Required = true;
		}
	}

	public class InstrumentIdOptionHandler : IAsyncOptionHandler<InstrumentIdOption> {
		private readonly ICommandContext context;
		private readonly InstrumentService instrumentProxy;

		public InstrumentIdOptionHandler(ICommandContext context, InstrumentService instrumentProxy) {
			this.context = context;
			this.instrumentProxy = instrumentProxy;
		}

		public async Task InvokeAsync(InstrumentIdOption option, ParseResult result, CancellationToken cancellationToken) {
			var id = result.GetValue(option);
			if (id != 0) {
				var valid = await instrumentProxy.VerifyId(id);
				if(!valid) {
					context.SetInputActionStatus(new OptionHandlerStatus(option.Name, false, $"Instrument id {id} is not valid.", null));
				}
			}
		}
	}
}