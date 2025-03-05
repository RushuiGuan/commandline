using Albatross.CommandLine;
using Microsoft.Extensions.Options;

namespace Sample.CommandLine {
	[Verb("derived-1", UseBaseClassProperties = false, Description = "With the UseBaseProperties flag set to true, the generated command will include base class properties as options")]
	public record class Derived1CommandOptions : BaseCommandOptions {
		public string Value { get; set; } = string.Empty;
	}

	[Verb("derived-2", UseBaseClassProperties = true, Description = "With the UseBaseProperties flag set to false, the generated command will not include base class properties as options")]
	public record class Derived2CommandOptions : BaseCommandOptions {
		public string Value { get; set; } = string.Empty;
	}

	public record class BaseCommandOptions {
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;
	}
}