using System.CommandLine;

namespace Albatross.CommandLine.Annotations {
	public class UseOptionAttribute<TOption, THandler> : OptionAttribute where TOption : Option where THandler : IAsyncOptionHandler<TOption> {
		public bool UseCustomNameAlias { get; set; }
	}

	public class UseOptionAttribute<TOption> : OptionAttribute where TOption : Option {
		public bool UseCustomNameAlias { get; set; }
	}
}