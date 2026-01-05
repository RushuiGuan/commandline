using Albatross.CommandLine.Annotations;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Text;
using Xunit;

namespace Albatross.CommandLine.Test.CodeGen {
	[Verb("backup")]
	public class BackupParams {
		[Option]
		public required string FileName { get; init; }

		[Argument]
		public required string Source { get; init; }
	}

	[Verb("test alias-dedup")]
	public class AliasDedupBehaviorParams {
		[Option("n", "a")]
		public required string Name { get; init; }

		[Option("a", "p")]
		public required string Apple { get; init; }

		[Option("e", "p")]
		public required string People { get; init; }
	}



	public class TestAliasDedupBehavior {
		AliasDedupBehaviorCommand BuildCommand() {
			var host = new CommandHost("_");
			host.AddCommands();
			host.CommandBuilder.BuildTree(host.GetServiceProvider);
			host.CommandBuilder.TryGetCommand("test alias-dedup", out Command cmd);
			return (AliasDedupBehaviorCommand)cmd;
		}

		[Fact]
		public void RunTest() {
			var cmd = BuildCommand();
			Assert.Equal(["-n", "-a"], cmd.Option_Name.Aliases);
			Assert.Equal(["-p"], cmd.Option_Apple.Aliases);
			Assert.Equal(["-e"], cmd.Option_People.Aliases);
		}
	}
}
