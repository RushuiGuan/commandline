using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Sample.CommandLine {
	public interface IMyService {
		Task<string> DoSomething();
	}
	public class MyService : IMyService, IDisposable {
		private readonly ILogger<MyService> logger;

		public MyService(ILogger<MyService> logger) {
			this.logger = logger;
			logger.LogInformation("MyService instantiated.");
		}
			
		public Task<string> DoSomething() {
			return Task.FromResult("Hello World!");
		}

		public void Dispose() {
			logger.LogInformation("MyService disposed.");
		}
	}
}