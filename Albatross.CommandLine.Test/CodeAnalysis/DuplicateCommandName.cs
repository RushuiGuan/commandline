using Albatross.CommandLine.Annotations;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Text;

namespace Albatross.CommandLine.Test.CodeGen {
	[Verb("dup-command-name1")]
	public class DuplicateCommandName1AParams {
	}
	[Verb("dup-command-name1", Description = "Should cause a 'ACL00005' warning")]
	public class DuplicateCommandName1BParams {
	}

	[Verb("dup-command-name2")]
	public class DuplicateCommandName2AParams {
	}
	[Verb("dup-command-name2 sub-command", Description = "Should not cause a 'ACL00005' warning")]
	public class DuplicateCommandName2BParams {
	}

	[Verb("dup-command-name2 sub-command", Description = "Should cause a 'ACL00005' warning")]
	public class DuplicateCommandName2CParams {
	}
}
