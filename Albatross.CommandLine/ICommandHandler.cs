using System.CommandLine;

namespace Albatross.CommandLine {
	public interface ICommandHandler {
		int Invoke(ParseResult result);
	}
}