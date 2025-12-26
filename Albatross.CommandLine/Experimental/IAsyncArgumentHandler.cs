using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Albatross.CommandLine.Experimental {
	public interface IAsyncArgumentHandler<T> where T:Symbol {
		Task InvokeAsync(T symbol, ParseResult result, CancellationToken cancellationToken);
	}
}