using Albatross.CommandLine.Annotations;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;

namespace Albatross.CommandLine.Inputs {
	[DefaultNameAliases("--output-directory", "--out", "-o")]
	public class OutputDirectoryOption : Option<DirectoryInfo> {
		public bool CreateIfNotExist { get; set; }
		private CommandLineAction? optionAction;

		public OutputDirectoryOption(string name, params string[] aliases) : base(name, aliases) {
			Description = "Specify an output directory";
			if (CreateIfNotExist) {
				Description += ".  The directory will be created if it doesn't exist";
			}

			this.optionAction = new SyncOptionAction(this.Invoke);
			this.Validators.Add(result => {
				var directory = result.GetValue(this);
				if (directory != null){
					if (File.Exists(directory.FullName)) {
						result.AddError($"Invalid directory name since a file of the same name exists");
					} else {
						if (!directory.Exists && !CreateIfNotExist) {
							result.AddError($"Output directory {directory.FullName} doesn't exist");
						}
					}
				}
			});
		}
		public override CommandLineAction? Action { get => optionAction; set => optionAction = value; }

		int Invoke(ParseResult result) {
			var directory = result.GetValue(this);
			if (directory != null && !directory.Exists) {
				directory.Create();
			}
			return 0;
		}
	}
}