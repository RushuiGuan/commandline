using System.CommandLine;

namespace Sample.CommandLine.SelfContainedOptions {
	public class InstrumentOption : Option<string> {
		public InstrumentOption() : base("--instrument", "-i") {
			this.Description = "The security instrument identifier (e.g., ticker symbol, CUSIP, ISIN)";
			this.Required = true;
		}
		
		public InstrumentSummary Summary { get; set; }
	}
}