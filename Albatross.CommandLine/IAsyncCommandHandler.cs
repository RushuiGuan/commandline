using System.Threading;
using System.Threading.Tasks;

namespace Albatross.CommandLine {
	public interface IAsyncCommandHandler {
		Task<int> InvokeAsync(CancellationToken cancellationToken);
	}
}