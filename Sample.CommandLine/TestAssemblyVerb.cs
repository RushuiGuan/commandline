using Albatross.CommandLine;
using Sample.CommandLine;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// assembly verb is used when both Options class and the CommandAction class are defined in a different assembly.
/// </summary>
[assembly: Verb<TestAssemblyVerbOptions, TestAssemblyVerbAction>("test assembly-verb", Description = "This verb demonstrates how to define verb for options and its handler in an assembly level")]

namespace Sample.CommandLine {
	public record class TestAssemblyVerbOptions{
		[Option(Description = "The name to be greeted")]
		public string Name { get; set; } = "World"; 
	}
	
	public class TestAssemblyVerbAction : CommandAction<TestAssemblyVerbOptions> {
		public TestAssemblyVerbAction(TestAssemblyVerbOptions options) : base(options) { }
		public override async Task<int> Invoke(CancellationToken token) {
			await this.Writer.WriteLineAsync($"Hello, {options.Name}!");
			return 0;
		}
	}
}