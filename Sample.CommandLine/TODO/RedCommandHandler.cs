using Albatross.CommandLine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine {
	public class RedCommandHandler : ICommandHandler {
		private readonly ILogger<RedCommandHandler> logger;
		private readonly ColorCommandOptions myOptions;

		private GlobalOptions GlobalOptions { get; }
		public MyColorPicker Picker { get; }

		public RedCommandHandler(MyColorPicker picker, ILogger<RedCommandHandler> logger, IOptions<GlobalOptions> globalOptions, IOptions<ColorCommandOptions> myOptions) {
			Picker = picker;
			this.logger = logger;
			this.myOptions = myOptions.Value;
			this.GlobalOptions = globalOptions.Value;
		}

		public Task<int> Invoke(CancellationToken token) {
			logger.LogInformation("i am here");
			logger.LogInformation("global options: {global}", this.GlobalOptions);
			logger.LogInformation("my options: {myOptions}", this.myOptions);
			logger.LogInformation("file input: {myOptions}", this.myOptions.MyFile);
			logger.LogInformation("color picker: {@color}", this.Picker);
			return Task.FromResult(0);
		}
	}
}