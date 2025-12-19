using Albatross.CommandLine;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine {
	[Verb<TestServiceInjectionCommandAction>("test service-injection", Description = "This verb demonstrats the use of service injection in command actions")]
	[Verb<TestInvalidServiceInjectionCommandAction>("test invalid-service-injection", Description = "This verb demonstrats the error condition when service is not registered")]
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
	
	public interface INotRegisteredService {
		Task<string> DoSomething();
	}
	public class TestInvalidServiceInjectionCommandAction : CommandAction<TestServiceInjectionOptions> {
		private readonly INotRegisteredService myService;
		public TestInvalidServiceInjectionCommandAction(INotRegisteredService myService, IOptions<TestServiceInjectionOptions> options) : base(options) {
			this.myService = myService;
		}
		public override async Task<int> Invoke(CancellationToken cancellationToken) {
			var text = await this.myService.DoSomething();
			this.Writer.WriteLine(text);
			return 0;
		}
	}
}