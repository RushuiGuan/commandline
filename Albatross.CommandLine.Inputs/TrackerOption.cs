using Albatross.CommandLine.Annotations;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Albatross.CommandLine.Inputs {
	[DefaultNameAliases("--tracker", "--tf")]
	[OptionHandler<TrackerOption, TrackerHandler, Tracker>]
	public class TrackerOption : OutputFileOption {
		public TrackerOption(string name, params string[] aliases) : base(name, aliases) {
			Description = "Specify a tracker file that can be used to keep track of items.  This option is tracking using case insensitive comparison.";
		}
	}

	[DefaultNameAliases("--tracker", "--tf")]
	[OptionHandler<CaseSensitiveTrackerOption, TrackerHandler, Tracker>]
	public class CaseSensitiveTrackerOption : TrackerOption {
		public CaseSensitiveTrackerOption(string name, params string[] aliases) : base(name, aliases) {
			Description = "Specify a tracker file that can be used to keep track of items.  This option is tracking using case sensitive comparison.";
		}
	}

	public class Tracker : IDisposable {
		private readonly HashSet<string> items;
		private readonly object sync = new object();
		private StreamWriter? writer;

		public Tracker(FileInfo? file) : this(file, StringComparer.OrdinalIgnoreCase) { }

		public Tracker(FileInfo? file, StringComparer stringComparer) {
			items = new HashSet<string>(stringComparer);
			if (file != null) {
				if (file.Exists) {
					using var stream = file.OpenRead();
					using var reader = new StreamReader(stream);
					for (var line = reader.ReadLine(); line != null; line = reader.ReadLine()) {
						line = line.Trim();
						if (line.Length > 0) {
							items.Add(line);
						}
					}
				}
				var fileStream = new FileStream(file.FullName, FileMode.Append, FileAccess.Write, FileShare.Read);
				writer = new StreamWriter(fileStream);
			}
		}

		public bool IsNew(string item) {
			if (writer != null) {
				lock (sync) {
					return !items.Contains(item);
				}
			}
			return true;
		}
		public async Task ProcessIfNew(string item, Func<CancellationToken, Task> func, ILogger logger, CancellationToken cancellationToken) {
			if (IsNew(item)) {
				try {
					await func(cancellationToken);
					Add(item);
				} catch (OperationCanceledException) {
					throw;
				} catch (Exception ex) {
					logger.LogError(ex, "Error processing item {Item}", item);
				}
			}
		}
		public void Add(string item) {
			if (writer != null) {
				lock (sync) {
					if (items.Add(item)) {
						writer.WriteLine(item);
						writer.Flush();
					}
				}
			}
		}

		public void Dispose() => writer?.Dispose();
	}

	public class TrackerHandler : IAsyncOptionHandler<TrackerOption, Tracker> {
		public Task<OptionHandlerResult<Tracker>> InvokeAsync(TrackerOption symbol, ParseResult result, CancellationToken cancellationToken) {
			var file = result.GetValue(symbol);
			if (file != null) {
				var tracker = new Tracker(file, symbol is CaseSensitiveTrackerOption ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);
				return Task.FromResult(new OptionHandlerResult<Tracker>(tracker));
			} else {
				return Task.FromResult(new OptionHandlerResult<Tracker>());
			}
		}
	}
}
