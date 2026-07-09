using System.IO;
using Albatross.CommandLine.Inputs;
using Xunit;

namespace Albatross.CommandLine.Test {
	public class Tracker_Constructor {
		[Fact]
		public void OpenForWriting_AllowsConcurrentReader() {
			var tempFile = Path.GetTempFileName();
			try {
				using var tracker = new Tracker(new FileInfo(tempFile));
				tracker.Add("test-item-1");

				// The tracker opens the file with FileShare.Read, so another reader can open it concurrently.
				using var readStream = new FileStream(tempFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				using var reader = new StreamReader(readStream);
				Assert.Contains("test-item-1", reader.ReadToEnd());
			} finally {
				if (File.Exists(tempFile)) {
					File.Delete(tempFile);
				}
			}
		}

		[Fact]
		public void OpenForWriting_PreventsExclusiveWrite() {
			var tempFile = Path.GetTempFileName();
			try {
				using var tracker = new Tracker(new FileInfo(tempFile));
				// The tracker holds the file open for writing, so an exclusive write handle must fail.
				Assert.Throws<IOException>(() => {
					using var writeStream = new FileStream(tempFile, FileMode.Append, FileAccess.Write, FileShare.None);
				});
			} finally {
				if (File.Exists(tempFile)) {
					File.Delete(tempFile);
				}
			}
		}
	}
}
