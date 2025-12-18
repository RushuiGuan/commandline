using Albatross.CommandLine;
using Microsoft.Extensions.Options;
using Sample.CommandLine;
using System.Threading;
using System.Threading.Tasks;


[assembly: Verb<MyGenericCommandHandle<string>, MyOptions>("generic1", Description = "generic1 command")]
[assembly: Verb<MyGenericCommandHandle<int>, MyOptions>("generic2", Description = "generic2 command")]
[assembly: Verb<MyGenericCommandHandle<int>, MyOptions>("generic3",  Description = "generic3 command")]
namespace Sample.CommandLine {
	public record class MyOptions {
		[Argument(Description = "A name")]
		public string Name { get; set; } = string.Empty;
	}

	public class MyGenericCommandHandle<T> : CommandAction<MyOptions> {
		public MyGenericCommandHandle(IOptions<MyOptions> options) : base(options) {
		}

		public override Task<int> Invoke(CancellationToken token) {
			this.Writer.WriteLine($"MyGenericCommandAction<{typeof(T).Name}> is invoked with name of {options.Name}");
			return Task.FromResult(0);
		}
	}
}