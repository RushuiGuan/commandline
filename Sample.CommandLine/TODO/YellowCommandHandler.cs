using Albatross.CommandLine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine {
	public class YellowCommandAction : ICommandAction {
		private readonly ILogger<YellowCommandAction> logger;
		private readonly ColorCommandOptions myOptions;

		public YellowCommandAction(ILogger<YellowCommandAction> logger, IOptions<ColorCommandOptions> myOptions) {
			this.logger = logger;
			this.myOptions = myOptions.Value;
		}

		public Task<int> Invoke(CancellationToken token) {
			logger.LogInformation("i am here");
			logger.LogInformation("my options: {myOptions}", this.myOptions);
			logger.LogInformation("file input: {myOptions}", this.myOptions.MyFile);
			throw new InvalidOperationException("i am crazy");
		}
	}
}