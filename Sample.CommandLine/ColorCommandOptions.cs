using Albatross.CommandLine;
using System.CommandLine;
using System.IO;

namespace Sample.CommandLine {
	[Verb("yellow-command", typeof(YellowCommandHandler), Description = "a sample yellow command", Alias = ["y"])]
	[Verb("red-command", typeof(RedCommandHandler), Description = "a sample red command", Alias = ["r"])]
	public record class ColorCommandOptions {
		[Option("n", "-name", Description = "use the name of the test")]
		public string Name { get; set; } = string.Empty;

		public int Data { get; set; }

		[Option("s", "second", Description = "second file name")]
		public FileInfo SecondFile { get; set; } = null!;


		[Option("x", "myfile", Description = "input file name")]
		public FileInfo MyFile { get; set; } = null!;
	}

	public sealed partial class ColorCommand : IRequireInitialization {
		public void Init() {
			this.Option_Data.AddCompletions("1", "2", "3");
		}
	}
}