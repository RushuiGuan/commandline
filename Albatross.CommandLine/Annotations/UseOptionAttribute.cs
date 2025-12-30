using System.CommandLine;

namespace Albatross.CommandLine.Annotations {
	public class UseOptionAttribute<TOption, THandler> : OptionAttribute where TOption : Option where THandler : IAsyncOptionHandler<TOption> {
	}
	public class UseOptionAttribute<TOption> : OptionAttribute where TOption : Option {
	}
}