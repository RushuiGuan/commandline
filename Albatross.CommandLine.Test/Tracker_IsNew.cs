using System.IO;
using Albatross.CommandLine.Inputs;
using Xunit;

namespace Albatross.CommandLine.Test {
	public class Tracker_IsNew {
		[Theory]
		[InlineData("existing-item-1", false)]
		[InlineData("existing-item-2", false)]
		[InlineData("new-item", true)]
		public void ReflectsItemsLoadedFromExistingFile(string item, bool expectedIsNew) {
			var tempFile = Path.GetTempFileName();
			try {
				File.WriteAllLines(tempFile, new[] { "existing-item-1", "existing-item-2" });
				using var tracker = new Tracker(new FileInfo(tempFile));
				Assert.Equal(expectedIsNew, tracker.IsNew(item));
			} finally {
				if (File.Exists(tempFile)) {
					File.Delete(tempFile);
				}
			}
		}
	}
}
