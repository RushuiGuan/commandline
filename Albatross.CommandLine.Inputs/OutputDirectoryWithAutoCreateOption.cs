using Albatross.CommandLine.Annotations;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;

namespace Albatross.CommandLine.Inputs {
	/// <summary>
	/// A command-line option for specifying an output directory that is automatically created if it doesn't exist.
	/// Validates that no file exists with the same name, then creates the directory before command execution.
	/// </summary>
	[DefaultNameAliases("--output-directory", "--out", "-o")]
	public class OutputDirectoryWithAutoCreateOption : Option<DirectoryInfo> {
		private CommandLineAction? optionAction;

		/// <summary>
		/// Creates a new auto-creating output directory option with the specified name and aliases.
		/// </summary>
		/// <param name="name">The primary name of the option.</param>
		/// <param name="aliases">Additional aliases for the option.</param>
		public OutputDirectoryWithAutoCreateOption(string name, params string[] aliases) : base(name, aliases) {
			Description = "Specify an output directory that would be created automatically if it doesn't exist";
			this.optionAction = new SyncOptionAction(this.Invoke);
			this.Validators.Add(result => {
				var directory = result.GetValue(this);
				if (directory != null && File.Exists(directory.FullName)) {
					result.AddError($"Invalid directory name since a file of the same name exists");
				}
			});
		}

		/// <inheritdoc/>
		public override CommandLineAction? Action { get => optionAction; }

		int Invoke(ParseResult result) {
			var directory = result.GetValue(this);
			if (directory != null && !directory.Exists) {
				directory.Create();
			}
			return 0;
		}
	}
}