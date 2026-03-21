using Albatross.CommandLine;
using Albatross.CommandLine.Annotations;
using Albatross.CommandLine.Inputs;
using System;
using System.CommandLine;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine {
	[Verb<TestReusable>("test reusable")]
	public class TestReusableParams {
		[UseArgument<OutputDirectoryArgument>]
		public required DirectoryInfo OutputDirectory1 { get; init; }
		
		[UseOption<OutputDirectoryOption>(UseCustomName = true)]
		public required DirectoryInfo OutputDirectory2 { get; init; }
	}

	public class TestReusable : BaseHandler<TestReusableParams> {
		public TestReusable(ParseResult result, TestReusableParams parameters) : base(result, parameters) {
		}
		public override Task<int> InvokeAsync(CancellationToken cancellationToken) {
			Console.WriteLine(parameters.OutputDirectory1.FullName);
			Console.WriteLine(parameters.OutputDirectory2.FullName);
			return Task.FromResult(0);
		}
	}
}