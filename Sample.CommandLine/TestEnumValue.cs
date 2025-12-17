using Albatross.CommandLine;
using Sample.CommandLine.Core;

namespace Sample.CommandLine {
	[Verb("test enum", Description ="Test use of enum value as arguments and options")]
	public class TestEnumValueOptions {
		[Argument(Description = "First required shade of gray")]
		public ShadesOfGray Gray1 { get; init; }


		[Option(Description = "Second required shade of gray")]
		public ShadesOfGray Gray2 { get; init; }

		[Option(Description = "Third optional shade of gray")]
		public ShadesOfGray? Gray3 { get; init; }
	}
}
