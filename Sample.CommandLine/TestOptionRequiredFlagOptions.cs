using Albatross.CommandLine;

namespace Sample.CommandLine {
	[Verb<DefaultCommandAction<TestOptionRequiredFlagOptions>>("test option-required-flag", Description = "By default, nullable value, collection and booleans are not required.  But Required flag can be used to overwritten")]
	public record class TestOptionRequiredFlagOptions {
		[Option(Required = true, Description = "Collection with required flag")]
		public required int[] IntValues { get; init; }

		[Option(Description = "Optional collection")]
		public string[] TextValues { get; set; } = [];

		[Option(Description = "Default optional boolean flag")]
		public bool OptionalBoolValue { get; init; }

		[Option(Required = true, Description = "Required boolean flag")]
		public bool RequiredBoolValue { get; init; }

		[Option(Description = "Required text value")]
		public required string RequiredTextValue { get; init; }

		[Option(Description = "Required text value, the requirement comes from nullability of the type, not the required keyword")]
		public string RequiredTextValue2 { get; init; } = string.Empty;

		[Option(Description = "Optional text value")]
		public string? OptionalTextValue { get; init; }

		[Option(Required = false, Description = "Non Nullable struct value with Required = false")]
		public int OptionalIntValue { get; init; }

		[Option(Required = false, Description = "Non Nullable reference value with Required = false.  The generated code will create a compiler warning when creating the instance since the value is nullable")]
		public string NonNullableOptionalTextValue { get; init; } = string.Empty;

		[Option(Required = true, Description = "Nullable reference value with Required = true")]
		public string? NullableRequiredTextValue { get; init; } = string.Empty;
	}
}