using Albatross.CommandLine;
using Albatross.CommandLine.Annotations;
using Sample.CommandLine;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// assembly verb is used when both Params class and the CommandHandler class are defined in a different assembly.
/// </summary>
[assembly: Verb<TestAssemblyVerbParams, TestAssemblyVerbHandler>("test assembly-verb one", Description = "This verb demonstrates how to define verb for parameters and its handler in an assembly level")]
[assembly: Verb<TestAssemblyVerbParams, TestAssemblyVerbHandler>("test assembly-verb two", Description = "This verb demonstrates how to define verb for parameters and its handler in an assembly level")]

namespace Sample.CommandLine {
	public record class TestAssemblyVerbParams{
		[Option(Description = "The name to be greeted")]
		public string Name { get; set; } = "World"; 
	}
	
	public class TestAssemblyVerbHandler : BaseHandler<TestAssemblyVerbParams> {
		public TestAssemblyVerbHandler(ParseResult result,TestAssemblyVerbParams parameters) : base(result, parameters) { }
		public override async Task<int> InvokeAsync(CancellationToken token) {
			await this.Writer.WriteLineAsync($"Hello, {parameters.Name}!");
			return 0;
		}
	}
}