using Albatross.CommandLine.Annotations;
using System.CommandLine;

namespace Albatross.CommandLine.Inputs {
	[DefaultNameAliases("--confirm", "-c")]
	public class ConfirmOption : Option<bool> {
		// System.CommandLine bug: [default: True] is not printed for bool options using DefaultValueFactory; fixed in v3
		static readonly bool AppendDefault = typeof(Option<bool>).Assembly.GetName().Version?.Major < 3;

		public ConfirmOption(string name, params string[] aliases) : base(name, aliases) {
			Description = AppendDefault
				? "Prompt for confirmation before proceeding [default: true]"
				: "Prompt for confirmation before proceeding";
			DefaultValueFactory = _ => true;
			Arity = ArgumentArity.ZeroOrOne;
		}
	}
}