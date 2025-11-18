using System.CommandLine;

namespace TestConsole;

// Simple command handlers that demonstrate basic functionality
public class GreetCommandHandler {
	public int Invoke(ParseResult parseResult) {
		System.Console.Out.WriteLine("greet");
		return 0;
	}
}
