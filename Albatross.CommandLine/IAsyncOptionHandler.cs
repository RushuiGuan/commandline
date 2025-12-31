using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Albatross.CommandLine {
	/// <summary>
	/// This handler is for command option or argument action.  If the handler has a return value, inject ICommandContext to set the value into the context.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface IAsyncOptionHandler<in T> where T:Option {
		Task InvokeAsync(T symbol, ParseResult result, CancellationToken cancellationToken);
	}
}