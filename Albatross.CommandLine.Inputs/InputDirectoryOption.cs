using Albatross.CommandLine.Annotations;
using System.CommandLine;
using System.IO;

namespace Albatross.CommandLine.Inputs {
	[DefaultNameAliases("--input-directory", "--in", "-i")]
	public class InputDirectoryOption : Option<DirectoryInfo> {
		public InputDirectoryOption(string name, params string[] aliases) : base(name, aliases) {
			Description = "Specify an existing input directory";
			this.Validators.Add(result => {
				var directory = result.GetValue(this);
				if (directory != null && !directory.Exists) {
					result.AddError($"Input directory {directory.FullName} doesn't exist");
				}
			});
		}
	}
}
