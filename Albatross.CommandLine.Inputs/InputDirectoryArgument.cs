using System.CommandLine;
using System.IO;

namespace Albatross.CommandLine.Inputs {
	public class InputDirectoryArgument : Argument<DirectoryInfo> {
		public InputDirectoryArgument(string name) : base(name) {
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
