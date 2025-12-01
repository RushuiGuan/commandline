using System.Collections.Generic;
using System.CommandLine;

namespace Albatross.CommandLine {
	public static class Extensions {
		public static string[] GetCommandNames(this Command command) {
			var stack = new Stack<string>();
			do {
				stack.Push(command.Name);
			} while (command is not RootCommand);
			return stack.ToArray();
		}
	}
}