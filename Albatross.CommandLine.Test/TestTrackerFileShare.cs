using Albatross.CommandLine.Inputs;
using System;
using System.IO;
using System.Threading;
using Xunit;

namespace Albatross.CommandLine.Test {
	public class TestTrackerFileShare {
		[Fact]
		public void TestTrackerAllowsSharedRead() {
			var tempFile = Path.GetTempFileName();
			try {
				// Create a tracker which will hold the file open for writing with shared read
				using var tracker = new Tracker(new FileInfo(tempFile));
				
				// Add an item to ensure the file is being written to
				tracker.Add("test-item-1");
				
				// Try to open the file for reading from another process/thread
				// This should succeed because we're using FileShare.Read
				using var readStream = new FileStream(tempFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				using var reader = new StreamReader(readStream);
				var content = reader.ReadToEnd();
				
				// Verify we can read the content
				Assert.Contains("test-item-1", content);
			} finally {
				if (File.Exists(tempFile)) {
					File.Delete(tempFile);
				}
			}
		}
		
		[Fact]
		public void TestTrackerPreventsExclusiveWrite() {
			var tempFile = Path.GetTempFileName();
			try {
				// Create a tracker which will hold the file open for writing
				using var tracker = new Tracker(new FileInfo(tempFile));
				
				// Try to open the file for exclusive write access
				// This should fail because the tracker has it open for writing
				Assert.Throws<IOException>(() => {
					using var writeStream = new FileStream(tempFile, FileMode.Append, FileAccess.Write, FileShare.None);
				});
			} finally {
				if (File.Exists(tempFile)) {
					File.Delete(tempFile);
				}
			}
		}
		
		[Fact]
		public void TestTrackerCanReadExistingFile() {
			var tempFile = Path.GetTempFileName();
			try {
				// Write some initial data
				File.WriteAllLines(tempFile, new[] { "existing-item-1", "existing-item-2" });
				
				// Create tracker which should read the existing items
				using var tracker = new Tracker(new FileInfo(tempFile));
				
				// Verify existing items are not considered new
				Assert.False(tracker.IsNew("existing-item-1"));
				Assert.False(tracker.IsNew("existing-item-2"));
				
				// Verify new items are still considered new
				Assert.True(tracker.IsNew("new-item"));
			} finally {
				if (File.Exists(tempFile)) {
					File.Delete(tempFile);
				}
			}
		}
	}
}
