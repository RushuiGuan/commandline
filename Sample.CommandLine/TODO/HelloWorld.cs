using Albatross.CommandLine;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine {
	[Verb("hello", typeof(HelloWorldCommandAction), Description = "HelloWorld command")]
	public record class HelloWorldOptions {
		[Option("n", Description = "Give me a name")]
		public string Name { get; set; } = string.Empty;

		[Option("d", Description = "Give me an optional date")]
		public DateOnly? Date { get; set; }

		[Option("x", Description = "Give me a number", DefaultToInitializer = true)]
		public int Number { get; set; } = 100;
	}
	public class HelloWorldCommandAction : CommandAction<HelloWorldOptions> {
		public HelloWorldCommandAction(IOptions<HelloWorldOptions> options) : base(options) {
		}

		public override Task<int> Invoke(CancellationToken cancellationToken) {
			throw new NotImplementedException();
		}
	}
}
