using Albatross.Collections;
using Albatross.CommandLine;
using Albatross.CommandLine.Annotations;
using Albatross.CommandLine.Inputs;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine {
	[Verb<TestLongRunningJobWithTracking>("test tracking", Description = "Test long running job with tracking")]
	public class TestLongRunningJobWithTrackingParams {
		[UseArgument<InputDirectoryArgument>]
		public required DirectoryInfo Directory { get; init; }

		[UseOption<TrackerOption>]
		public Tracker? Tracker { get; init; }

		[Option(DefaultToInitializer = true)]
		public int BatchSize { get; init; } = 100;
	}

	public class TestLongRunningJobWithTracking : IAsyncCommandHandler {
		private readonly TestLongRunningJobWithTrackingParams parameters;
		private readonly ILogger<TestLongRunningJobWithTracking> logger;

		public TestLongRunningJobWithTracking(TestLongRunningJobWithTrackingParams parameters, ILogger<TestLongRunningJobWithTracking> logger) {
			this.parameters = parameters;
			this.logger = logger;
		}

		public async Task<int> InvokeAsync(CancellationToken cancellationToken) {
			var stopWatch = new System.Diagnostics.Stopwatch();
			stopWatch.Start();
			var tracker = parameters.Tracker ?? new Tracker(null);
			var files = parameters.Directory.GetFiles("*.*", SearchOption.AllDirectories);
			foreach (var batch in files.Batch(parameters.BatchSize)) {
				var tasks = new List<Task>();
				foreach (var file in batch) {
					tasks.Add(tracker.ProcessIfNew(file.FullName, (ct) => Process(file, ct), logger, cancellationToken));
				}
				await Task.WhenAll(tasks);
			}
			stopWatch.Stop();
			System.Console.WriteLine($"Processing completed in {stopWatch.Elapsed}");
			return 0;
		}

		async Task Process(FileInfo file, CancellationToken cancellationToken) {
			logger.LogInformation("Processing file {File}", file.FullName);
			await Task.Delay(100, cancellationToken);
		}
	}
}
