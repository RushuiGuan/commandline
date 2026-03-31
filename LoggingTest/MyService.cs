using Microsoft.Extensions.Logging;

namespace LoggingTest {
	public class MyService : IDisposable{
		private readonly ILogger<MyService> logger;
		public MyService(ILogger<MyService> logger) {
			this.logger = logger;
			this.logger.LogInformation("Starting MyService");
		}
		public void Dispose() {
			logger.LogInformation("Disposing MyService");
		}
	}
}