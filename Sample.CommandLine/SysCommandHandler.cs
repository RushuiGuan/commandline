using Albatross.CommandLine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine {
	[Verb("sys-command", typeof(SysCommandHandler), Alias = ["t"])]
	public record class SysCommandOptions {
		[Option]
		public int Id { get; set; }
		[Option]
		public string Name { get; set; } = string.Empty;

		[Option(Required = false)]
		public decimal Price { get; set; }

		public int ShouldIgnore { get; set; }
		
		[Option]
		public int ShouldNotIgnore { get; set; }

		[Option(Required = true)]
		public int? ForceRequired { get; set; }

		[Option]
		public ICollection<string> Items { get; set; } = new List<string>();

		[Option(Required = true)]
		public IDictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
	}
	public class SysCommandHandler : ICommandHandler {
		private readonly ILogger<SysCommandHandler> logger;
		private readonly SysCommandOptions myOptions;

		public SysCommandHandler(ILogger<SysCommandHandler> logger, IOptions<SysCommandOptions> myOptions) {
			this.logger = logger;
			this.myOptions = myOptions.Value;
		}

		public Task<int> Invoke(CancellationToken token) {
			logger.LogInformation("i am here");
			logger.LogInformation("my options: {myOptions}", this.myOptions);
			return Task.FromResult(0);
		}
	}
}