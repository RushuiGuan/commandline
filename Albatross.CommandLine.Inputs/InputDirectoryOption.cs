using Albatross.CommandLine.Annotations;
using System.CommandLine;
using System.IO;

namespace Albatross.CommandLine.Inputs {
	/// <summary>
	/// A command-line option for specifying an existing input directory.
	/// Validates that the directory exists before command execution.
	/// </summary>
	[DefaultNameAliases("--input-directory", "--in", "-i")]
	public class InputDirectoryOption : Option<DirectoryInfo> {
		/// <summary>
		/// Creates a new input directory option with the specified name and aliases.
		/// </summary>
		/// <param name="name">The primary name of the option.</param>
		/// <param name="aliases">Additional aliases for the option.</param>
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
