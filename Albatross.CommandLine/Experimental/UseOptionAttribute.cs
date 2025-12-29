using System.CommandLine;

namespace Albatross.CommandLine {
	public class UseOptionAttribute<TOption, THandler> : OptionAttribute where TOption : Option where THandler : IAsyncCommandParameterHandler<TOption> {
	}
	public class UseOptionAttribute<TOption> : OptionAttribute where TOption : Option {
	}
}