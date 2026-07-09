using Albatross.CommandLine.Annotations;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Text;

namespace Albatross.CommandLine.Test.CodeAnalysis {
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
