using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Albatross.CommandLine.Outputs;
using Xunit;

namespace Albatross.CommandLine.Test {
	public class Extensions_ShouldPrintJson {
		public record class Widget(int Id, string Name);

		public static TheoryData<object, bool> Values => new() {
			{ new Widget(1, "a"), true },
			{ new { Id = 1 }, true },
			{ new KeyValuePair<string, int>("a", 1), true },
			{ new[] { 1, 2 }, true },
			{ new List<Widget>(), true },
			{ new Dictionary<string, int> { ["a"] = 1 }, true },
			{ "text", false },
			{ "", false },
			{ 42, false },
			{ 1.5m, false },
			{ true, false },
			{ Guid.Empty, false },
			{ new DateOnly(2026, 11, 16), false },
			{ DayOfWeek.Monday, false },
			// enumerable, but serialized as a base64 string
			{ new byte[] { 1, 2 }, false },
			{ new JsonObject { ["a"] = 1 }, true },
			{ new JsonArray(1, 2), true },
			{ JsonValue.Create(5), false },
			{ JsonDocument.Parse("[1]").RootElement, true },
			{ JsonDocument.Parse("\"text\"").RootElement, false },
			{ JsonDocument.Parse("{}"), true },
		};

		[Theory]
		[MemberData(nameof(Values))]
		public void ContainersAndScalars(object value, bool expected) {
			Assert.Equal(expected, Outputs.Extensions.ShouldPrintJson(value));
		}
	}
}
