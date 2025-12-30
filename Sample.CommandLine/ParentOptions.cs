using Albatross.CommandLine;
using Albatross.CommandLine.Annotations;

namespace Sample.CommandLine {
	[Verb("test", Description = "A series of test commands to verify the functionalities")]
	[Verb("example", Description = "A series of examples of varies use cases")]
	public record class ParentParams {
	}
}
