using Albatross.Config;
using System;
using System.IO;

namespace Albatross.CommandLine.Defaults {
	public class DefaultApplicationPath : IApplicationPath {
		public bool IsSystemPath => true;
		public string DataRoot => Path.Join(AppContext.BaseDirectory, "data");
		public string ConfigRoot => AppContext.BaseDirectory;	
		public string LogRoot => Path.Join(AppContext.BaseDirectory, "log");
		public void Init() {
			this.EnsureDirectory(this.DataRoot);
			this.EnsureDirectory(this.LogRoot);
		}

		private void EnsureDirectory(string path) {
			try {
				Directory.CreateDirectory(path);
			} catch (UnauthorizedAccessException ex) {
				throw new UnauthorizedAccessException($"'{path}' is not accessible", ex);
			}
		}
	}
}
