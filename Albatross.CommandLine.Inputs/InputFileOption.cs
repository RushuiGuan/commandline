using Albatross.CommandLine.Annotations;
using System.CommandLine;
using System.IO;

namespace Albatross.CommandLine.Inputs {
	/// <summary>
	/// A command-line option for specifying an existing input file.
	/// Validates that the file exists before command execution.
	/// </summary>
	[DefaultNameAliases("--input-file", "--in", "-i")]
	public class InputFileOption : Option<FileInfo> {
		/// <summary>
		/// Creates a new input file option with the specified name and aliases.
		/// </summary>
		/// <param name="name">The primary name of the option.</param>
		/// <param name="aliases">Additional aliases for the option.</param>
		public InputFileOption(string name, params string[] aliases) : base(name, aliases) {
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
