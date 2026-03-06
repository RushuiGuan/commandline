using Albatross.CommandLine;
using Albatross.CommandLine.Annotations;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine {
	[Verb("test codeanalysis")]
	public class TestCodeAnalysis {
		[Option]
		public string? Test { get; set; }

		[Option]
		public string? test { get; set; }

		[UseOption<AnOptionWithAHandler>]
		public string? AnotherOption { get; set; }

		[Option]
		[Argument]
		public string? BothOptionAndArgument { get; set; }
	}

	// [OptionHandler<Option<int>, MyIntOptionHandler>]
	[OptionHandler<Option<string>, MyStringOptionHandler>]
	public class AnOptionWithAHandler : Option<string> {
		public AnOptionWithAHandler(string name, params string[] aliases) : base(name, aliases) {
		}
	}
	public class MyStringOptionHandler : IAsyncOptionHandler<Option<string>> {
		public Task InvokeAsync(Option<string> symbol, ParseResult result, CancellationToken cancellationToken) {
			throw new NotImplementedException();
		}
	}

	public class MyIntOptionHandler : IAsyncOptionHandler<Option<int>> {
		public Task InvokeAsync(Option<int> symbol, ParseResult result, CancellationToken cancellationToken) {
			throw new NotImplementedException();
		}
	}
}
