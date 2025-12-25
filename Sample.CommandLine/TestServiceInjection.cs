using Albatross.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine {
	[Verb<TestServiceInjectionCommandHandler>("test service-injection", Description = "This verb demonstrats the use of service injection in command actions")]
	[Verb<TestInvalidServiceInjectionCommandHandler>("test invalid-service-injection", Description = "This verb demonstrats the error condition when service is not registered")]
	public record class TestServiceInjectionOptions {
		[Option]
		public required string TextValue { get; init; }
	}
	public class TestServiceInjectionCommandHandler : CommandHandler<TestServiceInjectionOptions> {
		private readonly IMyService myService;
		public TestServiceInjectionCommandHandler(IMyService myService, TestServiceInjectionOptions options) : base(options) {
			this.myService = myService;
		}
		public override async Task<int> InvokeAsync(CancellationToken cancellationToken) {
			var text = await this.myService.DoSomething();
			await this.Writer.WriteLineAsync(text);
			await this.Writer.WriteLineAsync(this.options.ToString());
			return 0;
		}
	}
	
	public interface INotRegisteredService {
		Task<string> DoSomething();
	}
	public class TestInvalidServiceInjectionCommandHandler : CommandHandler<TestServiceInjectionOptions> {
		private readonly INotRegisteredService myService;
		public TestInvalidServiceInjectionCommandHandler(INotRegisteredService myService, TestServiceInjectionOptions options) : base(options) {
			this.myService = myService;
		}
		public override async Task<int> InvokeAsync(CancellationToken cancellationToken) {
			var text = await this.myService.DoSomething();
			await this.Writer.WriteLineAsync(text);
			await this.Writer.WriteLineAsync(this.options.ToString());
			return 0;
		}
	}
}