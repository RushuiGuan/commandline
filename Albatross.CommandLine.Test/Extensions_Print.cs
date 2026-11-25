using System.IO;
using System.Text.Json.Nodes;
using Albatross.CommandLine.Outputs;
using Xunit;

namespace Albatross.CommandLine.Test {
	// redirects the process-wide Console.Out, so it must not run in parallel with other console tests
	[Collection("ConsoleOutput")]
	public class Extensions_Print {
		public record class Widget(int Id, string Name, DayOfWeek Day);

		static string Capture(object? value, bool compact) {
			var original = Console.Out;
			var writer = new StringWriter();
			Console.SetOut(writer);
			try {
				value.Print(compact);
			} finally {
				Console.SetOut(original);
			}
			return writer.ToString().TrimEnd('\r', '\n');
		}

		public static TheoryData<object, string> Values => new() {
			{ new Widget(1, "a<b", DayOfWeek.Monday), "{\"id\":1,\"name\":\"a<b\",\"day\":\"Monday\"}" },
			{ new[] { 1, 2 }, "[1,2]" },
			{ new JsonObject { ["a"] = 1 }, "{\"a\":1}" },
			{ "plain text", "plain text" },
			{ "", "" },
			{ 42, "42" },
			{ 1.50m, "1.50" },
			{ true, "true" },
			{ DayOfWeek.Monday, "Monday" },
			{ new DateOnly(2026, 11, 16), "2026-11-16" },
			{ new byte[] { 1, 2, 3 }, "AQID" },
			{ JsonValue.Create("José"), "José" },
		};

		[Theory]
		[MemberData(nameof(Values))]
		public void Compact_ContainersAsJsonScalarsRaw(object value, string expected) {
			Assert.Equal(expected, Capture(value, compact: true));
		}

		[Theory]
		[InlineData("plain text")]
		[InlineData(42)]
		public void Scalar_NotFormattedEvenWhenNotCompact(object value) {
			Assert.Equal(value.ToString(), Capture(value, compact: false));
		}

		[Fact]
		public void Redirected_LongStringStaysValidJson() {
			Assert.True(Console.IsOutputRedirected, "the test host is expected to redirect stdout");
			var description = new string('x', 200);
			var output = Capture(new Widget(1, description, DayOfWeek.Monday), compact: false);
			Assert.Equal(description, JsonNode.Parse(output)!["name"]!.GetValue<string>());
			Assert.Contains("\n", output);
		}

		[Fact]
		public void NullValue_PrintsNothing() {
			Assert.Equal("", Capture(null, compact: true));
		}
	}
}
