using Albatross.CommandLine;
using Sample.CommandLine.Core;
using System;

namespace Sample.CommandLine {
	[Verb<DefaultAsyncCommandHandler<TestDefaultValuesOptions>>("test defaults", Description = "Test setting of default values")]
	public record class TestDefaultValuesOptions {
		[Option(DefaultToInitializer = true)]
		public int IntValue { get; init; } = 42;

		[Option(DefaultToInitializer = true)]
		public string StringValue { get; init; } = "Hello, World!";

		[Option(DefaultToInitializer = true)]
		public bool BoolValue { get; init; } = true;

		[Option(DefaultToInitializer = true)]
		public DateOnly Date { get; init; } = DateOnly.FromDateTime(DateTime.Today);

		[Option(DefaultToInitializer = true)]
		public ShadesOfGray ShadesOfGray { get; init; } = ShadesOfGray.LightGray;
	}
}