using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Albatross.CommandLine {
	public interface IAsyncCommandHandler {
		Task<int> InvokeAsync(ParseResult result, CancellationToken cancellationToken);
	}
}