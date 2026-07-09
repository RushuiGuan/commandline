using Albatross.CommandLine.Annotations;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Text;

namespace Albatross.CommandLine.CodeAnalysisTest {
	[DefaultNameAliases("--my-test")]
	public class MyTest1Option : Option<string> {
		public MyTest1Option(string name, params string[] aliases) : base(name, aliases) {
		}
	}

	[DefaultNameAliases("--my-test")]
	public class MyTest2Option : Option<string> {
		public MyTest2Option(string name, params string[] aliases) : base(name, aliases) {
		}
	}
	[Verb("dup-option-name", Description ="Should cause a 'ACL00001' warning")]
	public class DuplicateOptionNameParams {
		/// <summary>
		/// option name is --my-test
		/// </summary>
		[Option]
		public string? MyTest { get; init; }

		/// <summary>
		/// option name is --my-test, which is a dup
		/// </summary>
		[Option]
		public string? myTest { get; init; }
	}
	[Verb("dup-option-name2", Description = "Should cause a 'ACL00001' warning")]
	public class DuplicateOptionName2Params {
		/// <summary>
		/// option name is --my-test
		/// </summary>
		[Option]
		public string? MyTest { get; init; }

		[UseOption<MyTest1Option>]
		public string? MyTest1 { get; init; }
	}
	[Verb("dup-option-name3", Description = "Should cause a 'ACL00001' warning")]
	public class DuplicateOptionName3Params {
		/// <summary>
		/// option name is --my-test
		/// </summary>
		[UseOption<MyTest1Option>]
		public string? MyTest1 { get; init; }

		[UseOption<MyTest2Option>]
		public string? MyTest2 { get; init; }
	}
	[Verb("dup-option-name4", Description = "Should not cause a 'ACL00001' warning")]
	public class DuplicateOptionName4Params {
		/// <summary>
		/// option name is --my-test
		/// </summary>
		[Option]
		public string? MyTest { get; init; }

		/// <summary>
		/// this one should not cause a warning.  because the option name is --mytest
		/// </summary>
		[Option]
		public string? mytest { get; init; }
	}
}
