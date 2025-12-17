using Albatross.CommandLine;
using Microsoft.Extensions.Options;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine {
	// this declaration is not required.  The system will auto generate the parent command with a HelpCommandAction if it is not declared
	[Verb("search", typeof(HelpCommandAction))]
	public record class SearchOptions { }

	[Verb("search id", typeof(SearchCommandAction))]
	public class SearchByIdOptions {
		[Option]
		public int Id { get; set; }
	}
	[Verb("search name", typeof(SearchCommandAction))]
	public class SearchByNameOptions {
		[Option]
		public string Name { get; set; } = string.Empty;
	}
	public class SearchCommandAction : ICommandAction {
		private readonly ParseResult result;
		private readonly IOptions<SearchByIdOptions> searchByIdOptions;
		private readonly IOptions<SearchByNameOptions> searchByNameOptions;

		public SearchCommandAction(ParseResult result, IOptions<SearchByIdOptions> searchByIdOptions, IOptions<SearchByNameOptions> searchByNameOptions) {
			this.result = result;
			this.searchByIdOptions = searchByIdOptions;
			this.searchByNameOptions = searchByNameOptions;
		}

		public Task<int> Invoke(CancellationToken token) {
			Console.WriteLine($"Command: {result.CommandResult.Command.Name} has been invoked");
			Console.WriteLine($"search by id: {searchByIdOptions.Value.Id}");
			Console.WriteLine($"search by name: {searchByNameOptions.Value.Name}");
			return Task.FromResult(0);
		}
	}
}
