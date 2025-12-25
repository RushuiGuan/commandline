using System.Threading;
using System.Threading.Tasks;

namespace Albatross.CommandLine {
	public interface ICommandHandler {
		Task<int> InvokeAsync(CancellationToken cancellationToken);
	}
}