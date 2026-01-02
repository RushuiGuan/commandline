using System.CommandLine;

namespace Albatross.CommandLine.Annotations {
	public class UseOptionAttribute<TOption> : OptionAttribute where TOption : Option {
		public UseOptionAttribute(params string[] aliases) : base(aliases) { }
		public bool UseCustomName { get; set; }
	}
}