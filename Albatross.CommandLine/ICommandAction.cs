using System.Threading;
using System.Threading.Tasks;

namespace Albatross.CommandLine {
	public interface ICommandAction {
		Task<int> Invoke(CancellationToken cancellationToken);
	}
}