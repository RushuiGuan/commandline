using System.IO;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Albatross.CommandLine.Outputs;
using Xunit;

namespace Albatross.CommandLine.Test {
	// redirects the process-wide Console.Out, so it must not run in parallel with other console tests
	[Collection("ConsoleOutput")]
	public class Extensions_PrintAsync {
		public record class Widget(int Id, string Name, DayOfWeek Day);
		public record class Named(string Name);
		public record class Tagged(string Name, string Tag) : Named(Name);

		static async Task<string> Capture<T>(T value, bool compact, CancellationToken cancellationToken = default) {
			var original = Console.Out;
			var writer = new StringWriter();
			Console.SetOut(writer);
			try {
				await value.PrintAsync(compact, cancellationToken);
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
		public async Task Compact_ContainersAsJsonScalarsRaw(object value, string expected) {
			Assert.Equal(expected, await Capture(value, compact: true));
		}

		[Fact]
		public async Task Container_IndentedWhenNotCompact() {
			var output = await Capture(new[] { 1, 2 }, compact: false);
			Assert.Equal("[\n  1,\n  2\n]", output.ReplaceLineEndings("\n"));
		}

		[Theory]
		[InlineData("plain text")]
		[InlineData(42)]
		public async Task Scalar_NotFormattedEvenWhenNotCompact(object value) {
			Assert.Equal(value.ToString(), await Capture(value, compact: false));
		}

		[Fact]
		public async Task NullValue_PrintsNothing() {
			Assert.Equal("", await Capture<object?>(null, compact: true));
		}

		[Fact]
		public async Task DeclaredAsBaseType_PrintsRuntimeType() {
			Named value = new Tagged("a", "t");
			Assert.Equal("{\"tag\":\"t\",\"name\":\"a\"}", await Capture(value, compact: true));
		}

		[Fact]
		public async Task CancelledToken_ThrowsBeforeWriting() {
			var original = Console.Out;
			var writer = new StringWriter();
			Console.SetOut(writer);
			try {
				await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new[] { 1, 2 }.PrintAsync(true, new CancellationToken(canceled: true)));
			} finally {
				Console.SetOut(original);
			}
			Assert.Equal("", writer.ToString());
		}
	}
}
