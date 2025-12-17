using Albatross.CommandLine;
using Microsoft.Extensions.Options;
using Sample.CommandLine;
using System.Threading;
using System.Threading.Tasks;


[assembly: Verb("generic1", typeof(MyGenericCommandHandle<string>), Description = "generic1 command", OptionsClass = typeof(MyOptions))]
[assembly: Verb("generic2", typeof(MyGenericCommandHandle<int>), Description = "generic2 command", OptionsClass = typeof(MyOptions))]
[assembly: Verb("generic3", typeof(MyGenericCommandHandle<int>), Description = "generic3 command")]
namespace Sample.CommandLine {
	public record class MyOptions {
		[Argument(Description = "A name")]
		public string Name { get; set; } = string.Empty;
	}

	public class MyGenericCommandHandle<T> : BaseHandler<MyOptions> {
		public MyGenericCommandHandle(IOptions<MyOptions> options) : base(options) {
		}

		public override Task<int> Invoke(CancellationToken token) {
			this.writer.WriteLine($"MyGenericCommandHandler<{typeof(T).Name}> is invoked with name of {options.Name}");
			return Task.FromResult(0);
		}
	}
}
