using System.CommandLine;
using System.IO;

namespace Albatross.CommandLine.Inputs {
	public class OutputDirectoryArgument : Argument<DirectoryInfo> {
		public OutputDirectoryArgument(string name) : base(name) {
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