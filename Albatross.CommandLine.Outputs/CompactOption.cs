using Albatross.CommandLine.Annotations;
using System.CommandLine;

namespace Albatross.CommandLine.Outputs {
	[DefaultNameAliases("compact")]
	public class CompactOption : Option<bool> {
		public CompactOption(string name, params string[] aliases) : base(name, aliases) {
			Description = "Print output as compact single-line JSON with no indentation or color";
		}
	}
}
