using System.CommandLine;
using System.IO;

namespace Albatross.CommandLine.Inputs {
	/// <summary>
	/// A positional command-line argument for specifying an existing output directory.
	/// Validates that the directory exists and is not a file before command execution.
	/// </summary>
	public class OutputDirectoryArgument : Argument<DirectoryInfo> {
		/// <summary>
		/// Creates a new output directory argument with the specified name.
		/// </summary>
		/// <param name="name">The name of the argument displayed in help text.</param>
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