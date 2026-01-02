using Albatross.CommandLine.Annotations;
using System.CommandLine;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Albatross.CommandLine.Inputs {
	[DefaultNameAliases("--output-directory", "--out", "-o")]
	public class OutputDirectoryOption : Option<DirectoryInfo> {
		public OutputDirectoryOption(string name, params string[] aliases) : base(name, aliases) {
			Description = "Specify an output directory that will be created if it doesn't exist";
			Action = new AsyncOptionHandler(CreateIfNotExist);
			this.Validators.Add(result => {
				var directory = result.GetValue(this);
				if(directory != null && File.Exists(directory.FullName)) {
					result.AddError($"Invalid directory name since a file of the same name exists");
				}
			});
		}
		
		Task<int> CreateIfNotExist(ParseResult result, CancellationToken cancellationToken) {
			var directory = result.GetValue(this);
			if(directory != null && !directory.Exists) {
				directory.Create();
			}
			return Task.FromResult(0);
		}
	}
}