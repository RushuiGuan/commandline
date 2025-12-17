using Albatross.CommandLine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine {
	public class YellowCommandHandler : ICommandHandler {
		private readonly ILogger<YellowCommandHandler> logger;
		private readonly ColorCommandOptions myOptions;

		private GlobalOptions GlobalOptions { get; }

		public YellowCommandHandler(ILogger<YellowCommandHandler> logger, IOptions<GlobalOptions> globalOptions, IOptions<ColorCommandOptions> myOptions) {
			this.logger = logger;
			this.myOptions = myOptions.Value;
			this.GlobalOptions = globalOptions.Value;
		}

		public Task<int> Invoke(CancellationToken token) {
			logger.LogInformation("i am here");
			logger.LogInformation("global options: {global}", this.GlobalOptions);
			logger.LogInformation("my options: {myOptions}", this.myOptions);
			logger.LogInformation("file input: {myOptions}", this.myOptions.MyFile);
			throw new InvalidOperationException("i am crazy");
		}
	}
}