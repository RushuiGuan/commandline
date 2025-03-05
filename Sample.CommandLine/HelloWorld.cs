using Albatross.CommandLine;
using Microsoft.Extensions.Options;
using System;

namespace Sample.CommandLine {
	[Verb("hello", typeof(HelloWorldCommandHandler), Description = "HelloWorld command")]
	public record class HelloWorldOptions {
		[Option("n", Description = "Give me a name")]
		public string Name { get; set; } = string.Empty;

		[Option("d", Description = "Give me an optional date")]
		public DateOnly? Date { get; set; }

		[Option("x", Description = "Give me a number", DefaultToInitializer = true)]
		public int Number { get; set; } = 100;
	}
	public class HelloWorldCommandHandler : BaseHandler<HelloWorldOptions> {
		public HelloWorldCommandHandler(IOptions<HelloWorldOptions> options) : base(options) {
		}
	}
}
