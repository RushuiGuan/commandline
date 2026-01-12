using System.CommandLine;
using System.IO;

namespace Albatross.CommandLine.Inputs {
	/// <summary>
	/// A positional command-line argument for specifying an existing input directory.
	/// Validates that the directory exists before command execution.
	/// </summary>
	public class InputDirectoryArgument : Argument<DirectoryInfo> {
		/// <summary>
		/// Creates a new input directory argument with the specified name.
		/// </summary>
		/// <param name="name">The name of the argument displayed in help text.</param>
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
