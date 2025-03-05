using Albatross.Hosting.Utility;
using CommandLine;

namespace Sample.Hosting.Utility {
	public enum MyEnum {
		One, Two, Three
	}

	[Verb("hello-world")]
	public class HelloWorldOptions {
		[Option("mu", Required = true)]
		public string Mu { get; set; } = string.Empty;
	}
	public class HelloWorldCommandHandler : UtilityBase<HelloWorldOptions> {
		public HelloWorldCommandHandler(HelloWorldOptions option) : base(option) {
		}
	}
}