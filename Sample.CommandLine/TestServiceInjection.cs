using Albatross.CommandLine;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine {
	[Verb<TestServiceInjectionCommandAction>("test service-injection", Description = "This verb demonstrats the use of service injection in command actions")]
	public class TestServiceInjectionOptions {
	}
	public class TestServiceInjectionCommandAction : CommandAction<TestServiceInjectionOptions> {
		private readonly IMyService myService;
		public TestServiceInjectionCommandAction(IMyService myService, IOptions<TestServiceInjectionOptions> options) : base(options) {
			this.myService = myService;
		}
		public override async Task<int> Invoke(CancellationToken cancellationToken) {
			var text = await this.myService.DoSomething();
			this.Writer.WriteLine(text);
			return 0;
		}
	}
}