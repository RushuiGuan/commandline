using System;
using System.CommandLine;

namespace Sample.CommandLine {
	public class ManualCommand : Command {
		public ManualCommand() : base("manual-command", "This command is created manually") {
			Add(NameOption);
			SetAction(Invoke);
		}

		public Option<string> NameOption { get; } = new Option<string>("--name") { 
			Description = "Name option for the manual command",
			Required = true,
		};

		int Invoke(ParseResult result) {
			var name = result.GetRequiredValue(NameOption);
			Console.Out.WriteLine($"ManualCommand invoked with name: {name}");
			return 0;
		}
	}
}
