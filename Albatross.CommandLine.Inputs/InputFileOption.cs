using Albatross.CommandLine.Annotations;
using System.CommandLine;
using System.IO;

namespace Albatross.CommandLine.Inputs {
	[DefaultNameAliases("--input-file", "--in", "-i")]
	public class InputFileOption : Option<FileInfo> {
		public InputFileOption(string name, params string[] aliases) : base(name, aliases) {
			Description = "Specify an existing input File";
			this.Validators.Add(result => {
				var File = result.GetValue(this);
				if (File != null && !File.Exists) {
					result.AddError($"Input File {File.FullName} doesn't exist");
				}
			});
		}
	}
}
