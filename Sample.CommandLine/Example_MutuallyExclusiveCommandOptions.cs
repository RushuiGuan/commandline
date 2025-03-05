using Albatross.CommandLine;
using Microsoft.Extensions.Options;
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
			this.AddValidator(result => {
				var found = result.Children.Where(x => x.Symbol == this.Option_Id || x.Symbol == this.Option_Name).ToList();
				if (found.Count == 0) {
					result.ErrorMessage = "Either Id or Name is required";
				} else if (found.Count > 1) {
					result.ErrorMessage = "Id and Name are mutually exclusive";
				};
			});
		}
	}
}
