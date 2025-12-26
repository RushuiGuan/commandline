using Albatross.CommandLine;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine {
	[Verb<TestServiceInjectionBaseHandler>("test service-injection", Description = "This verb demonstrats the use of service injection in command actions")]
	[Verb<TestInvalidServiceInjectionBaseHandler>("test invalid-service-injection", Description = "This verb demonstrats the error condition when service is not registered")]
	public record class TestServiceInjectionOptions {
		[Option]
		public required string TextValue { get; init; }
	}

	public class TestServiceInjectionBaseHandler : BaseHandler<TestServiceInjectionOptions> {
		private readonly IMyService myService;

		public TestServiceInjectionBaseHandler(IMyService myService, ParseResult result, TestServiceInjectionOptions options) : base(result, options) {
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

	public class TestInvalidServiceInjectionBaseHandler : BaseHandler<TestServiceInjectionOptions> {
		private readonly INotRegisteredService myService;

		public TestInvalidServiceInjectionBaseHandler(INotRegisteredService myService, ParseResult result, TestServiceInjectionOptions options) : base(result, options) {
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