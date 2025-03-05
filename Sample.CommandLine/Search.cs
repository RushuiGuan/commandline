using Albatross.CommandLine;
using Microsoft.Extensions.Options;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace Sample.CommandLine {
	// this declaration is not required.  The system will auto generate the parent command with a HelpCommandHandler if it is not declared
	[Verb("search", typeof(HelpCommandHandler))]
	public record class SearchOptions { }

	[Verb("search id", typeof(SearchCommandHandler))]
	public class SearchByIdOptions {
		public int Id { get; set; }
	}
	[Verb("search name", typeof(SearchCommandHandler))]
	public class SearchByNameOptions {
		public string Name { get; set; } = string.Empty;
	}
	public class SearchCommandHandler : ICommandHandler {
		private readonly IOptions<SearchByIdOptions> searchByIdOptions;
		private readonly IOptions<SearchByNameOptions> searchByNameOptions;

		public SearchCommandHandler(IOptions<SearchByIdOptions> searchByIdOptions, IOptions<SearchByNameOptions> searchByNameOptions) {
			this.searchByIdOptions = searchByIdOptions;
			this.searchByNameOptions = searchByNameOptions;
		}

		public int Invoke(InvocationContext context) {
			context.Console.WriteLine($"Command: {context.ParseResult.CommandResult.Command.Name} has been invoked");
			context.Console.WriteLine($"search by id: {searchByIdOptions.Value.Id}");
			context.Console.WriteLine($"search by name: {searchByNameOptions.Value.Name}");
			return 0;
		}

		public Task<int> InvokeAsync(InvocationContext context) {
			return Task.FromResult(Invoke(context));
		}
	}
}
