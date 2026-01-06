using Albatross.CommandLine.Annotations;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;

namespace Albatross.CommandLine.Inputs {
	[DefaultNameAliases("--output-directory", "--out", "-o")]
	public class OutputDirectoryWithAutoCreateOption : Option<DirectoryInfo> {
		private CommandLineAction? optionAction;

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