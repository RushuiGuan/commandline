using Albatross.CommandLine.Annotations;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Text;

namespace Albatross.CommandLine.Test.CodeAnalysis {
	[Verb("both-argument-and-option", Description = "Should cause a 'ACL00004' warning")]
	public class BothArgumentAndOptionProperty {
		[Option]
		[Argument]
		public string? BothOptionAndArgument { get; set; }
	}

	[Verb("both-argument-and-option2", Description = "Should cause a 'ACL00004' warning")]
	public class BothArgumentAndOptionProperty2 {
		[UseOption<Inputs.InputFileOption>]
		[Argument]
		public string? BothOptionAndArgument { get; set; }
	}

	[Verb("both-argument-and-option3", Description = "Should cause a 'ACL00004' warning")]
	public class BothArgumentAndOptionProperty3 {
		[UseOption<Inputs.InputFileOption>]
		[UseArgument<Inputs.InputFileArgument>]
		public string? BothOptionAndArgument { get; set; }
	}

	[Verb("both-argument-and-option4", Description = "Should cause a 'ACL00004' warning")]
	public class BothArgumentAndOptionProperty4 {
		[Option]
		[UseArgument<Inputs.InputFileArgument>]
		public string? BothOptionAndArgument { get; set; }
	}

	[Verb("both-argument-and-useArgument", Description = "Should cause a 'ACL00004' warning")]
	public class BothArgumentAndUseArgumentProperty {
		[Argument]
		[UseArgument<Inputs.InputFileArgument>]
		public string? BothOptionAndArgument { get; set; }
	}

	[Verb("both-option-and-useOption", Description = "Should cause a 'ACL00004' warning")]
	public class BothOptionAndUseOptionProperty{
		[Option]
		[UseOption<Inputs.InputFileOption>]
		public string? BothOptionAndArgument { get; set; }
	}
}
