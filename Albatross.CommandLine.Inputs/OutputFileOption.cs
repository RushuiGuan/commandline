using Albatross.CommandLine.Annotations;
using System.CommandLine;
using System.IO;

namespace Albatross.CommandLine.Inputs {
	/// <summary>
	/// A command-line option for specifying an output file path.
	/// Validates that no directory exists with the same name as the specified file.
	/// </summary>
	[DefaultNameAliases("--output-file", "--out", "-o")]
	public class OutputFileOption : Option<FileInfo> {
		/// <summary>
		/// Creates a new output file option with the specified name and aliases.
		/// </summary>
		/// <param name="name">The primary name of the option.</param>
		/// <param name="aliases">Additional aliases for the option.</param>
		public OutputFileOption(string name, params string[] aliases) : base(name, aliases) {
			Description = "Specify the path of the output file";
			this.Validators.Add(result => {
				var file = result.GetValue(this);
				if(file != null && Directory.Exists(file.FullName)) {
					result.AddError($"Invalid file name since a directory of the same name exists");
				}
			});
		}
	}
}
