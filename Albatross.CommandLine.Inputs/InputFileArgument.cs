using System.CommandLine;
using System.IO;

namespace Albatross.CommandLine.Inputs {
	/// <summary>
	/// A positional command-line argument for specifying an existing input file.
	/// Validates that the file exists before command execution.
	/// </summary>
	public class InputFileArgument : Argument<FileInfo> {
		/// <summary>
		/// Creates a new input file argument with the specified name.
		/// </summary>
		/// <param name="name">The name of the argument displayed in help text.</param>
		public InputFileArgument(string name) : base(name) {
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
