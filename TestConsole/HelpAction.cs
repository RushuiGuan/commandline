using System.CommandLine;

namespace TestConsole {
	public class DefaultAction{
		public int Invoke(ParseResult parseResult) {
			parseResult.InvocationConfiguration.Output.WriteLine(parseResult.ToString());
			return 0;
		}
	}
}