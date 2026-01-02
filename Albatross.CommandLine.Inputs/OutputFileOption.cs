using Albatross.CommandLine.Annotations;
using System.CommandLine;
using System.IO;

namespace Albatross.CommandLine.Inputs {
	[DefaultNameAliases("--output-file", "--out", "-o")]
	public class OutputFileOption : Option<FileInfo> {
		public OutputFileOption(string name, params string[] aliases) : base(name, aliases) {
			Description = "Specify the path of the output file";
			this.Validators.Add(result => {
				var file = result.GetValue(this);
				if(file != null && Directory.Exists(file.FullName)) {
					result.AddError($"Invalid file name since a directory of the same name exists");
				}
			});
		}
	}
}
