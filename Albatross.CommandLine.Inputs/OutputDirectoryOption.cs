using Albatross.CommandLine.Annotations;
using System.CommandLine;
using System.IO;

namespace Albatross.CommandLine.Inputs {
	/// <summary>
	/// A command-line option for specifying an existing output directory.
	/// Validates that the directory exists and is not a file before command execution.
	/// </summary>
	[DefaultNameAliases("--output-directory", "--out", "-o")]
	public class OutputDirectoryOption : Option<DirectoryInfo> {
		/// <summary>
		/// Creates a new output directory option with the specified name and aliases.
		/// </summary>
		/// <param name="name">The primary name of the option.</param>
		/// <param name="aliases">Additional aliases for the option.</param>
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