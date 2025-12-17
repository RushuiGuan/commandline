using Albatross.CommandLine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sample.CommandLine {
	[Verb("backup", Description ="The Option class BackupCommandOptions and BackupOptions will lead to the same command name")]
	public class BackupCommandOptions {
	}
	[Verb("backup-2", Description ="System will handle this by postfixing a sequence number on duplicated command names")]
	public class BackupOptions {
		public string? DirectoryName { get; set; }
	}
}