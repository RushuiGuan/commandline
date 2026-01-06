using System.CommandLine;
using System.IO;

namespace Albatross.CommandLine.Inputs {
	public class InputFileArgument : Argument<FileInfo> {
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
