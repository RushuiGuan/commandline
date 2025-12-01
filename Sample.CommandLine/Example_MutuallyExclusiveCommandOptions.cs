using Albatross.CommandLine;
using Microsoft.Extensions.Options;
using System.CommandLine.Parsing;
using System.Linq;

namespace Sample.CommandLine {
	[Verb("mutually-exclusive-command", Description = "This demonstrates the creation of mutually exclusive command through a custom validation logic")]
	public class MutuallyExclusiveCommandOptions {
		[Option(Required = false, Description = "Describe your option requirement here")]
		public int Id { get; set; }

		[Option(Required = false, Description = "Describe your option requirement here")]
		public string Name { get; set; } = string.Empty;
	}

	public partial class MutuallyExclusiveCommand : IRequireInitialization {
		public void Init() {
			this.Validators.Add(result => {
				var found = result.Children.Where(x => x is OptionResult optionResult && (optionResult.Option == this.Option_Id || optionResult.Option == this.Option_Name)).ToList();
				if (found.Count == 0) {
					result.AddError("Either Id or Name is required");
				} else if (found.Count > 1) {
					result.AddError("Id and Name are mutually exclusive");
				};
			});
		}
	}
}
