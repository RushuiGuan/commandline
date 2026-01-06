using Albatross.CommandLine.Annotations;
using System.CommandLine;
using System.IO;

namespace Albatross.CommandLine.Inputs {
	[DefaultNameAliases("--output-directory", "--out", "-o")]
	public class OutputDirectoryOption : Option<DirectoryInfo> {
		public OutputDirectoryOption(string name, params string[] aliases) : base(name, aliases) {
			Description = "Specify an output directory";
			this.Validators.Add(result => {
				var directory = result.GetValue(this);
				if (directory != null){
					if (File.Exists(directory.FullName)) {
						result.AddError($"Invalid directory name since a file of the same name exists");
					} else {
						if (!directory.Exists) {
							result.AddError($"Output directory {directory.FullName} doesn't exist");
						}
					}
				}
			});
		}
	}
}