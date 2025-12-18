using Albatross.CommandLine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine {
	public class RedCommandAction : ICommandAction {
		private readonly ILogger<RedCommandAction> logger;
		private readonly ColorCommandOptions myOptions;

		public MyColorPicker Picker { get; }

		public RedCommandAction(MyColorPicker picker, ILogger<RedCommandAction> logger,  IOptions<ColorCommandOptions> myOptions) {
			Picker = picker;
			this.logger = logger;
			this.myOptions = myOptions.Value;
		}

		public Task<int> Invoke(CancellationToken token) {
			logger.LogInformation("i am here");
			logger.LogInformation("my options: {myOptions}", this.myOptions);
			logger.LogInformation("file input: {myOptions}", this.myOptions.MyFile);
			logger.LogInformation("color picker: {@color}", this.Picker);
			return Task.FromResult(0);
		}
	}
}