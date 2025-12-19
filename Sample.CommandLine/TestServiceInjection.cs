using Albatross.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine {
	[Verb<TestServiceInjectionCommandAction>("test service-injection", Description = "This verb demonstrats the use of service injection in command actions")]
	[Verb<TestInvalidServiceInjectionCommandAction>("test invalid-service-injection", Description = "This verb demonstrats the error condition when service is not registered")]
	public record class TestServiceInjectionOptions {
		[Option]
		public required string TextValue { get; init; }
	}
	public class TestServiceInjectionCommandAction : CommandAction<TestServiceInjectionOptions> {
		private readonly IMyService myService;
		public TestServiceInjectionCommandAction(IMyService myService, TestServiceInjectionOptions options) : base(options) {
			this.myService = myService;
		}
		public override async Task<int> Invoke(CancellationToken cancellationToken) {
			var text = await this.myService.DoSomething();
			await this.Writer.WriteLineAsync(text);
			await this.Writer.WriteLineAsync(this.options.ToString());
			return 0;
		}
	}
	
	public interface INotRegisteredService {
		Task<string> DoSomething();
	}
	public class TestInvalidServiceInjectionCommandAction : CommandAction<TestServiceInjectionOptions> {
		private readonly INotRegisteredService myService;
		public TestInvalidServiceInjectionCommandAction(INotRegisteredService myService, TestServiceInjectionOptions options) : base(options) {
			this.myService = myService;
		}
		public override async Task<int> Invoke(CancellationToken cancellationToken) {
			var text = await this.myService.DoSomething();
			await this.Writer.WriteLineAsync(text);
			await this.Writer.WriteLineAsync(this.options.ToString());
			return 0;
		}
	}
}