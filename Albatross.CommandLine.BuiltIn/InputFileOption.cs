using System.CommandLine;
using System.IO;

namespace Albatross.CommandLine.BuiltIn {
	public class InputFileOption : Option<FileInfo> {
		public InputFileOption() : this("--input-file", "-i") {
		}
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
