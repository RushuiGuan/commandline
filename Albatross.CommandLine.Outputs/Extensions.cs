using DevLab.JmesPath.Expressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Spectre.Console;
using Spectre.Console.Json;
using System.CommandLine;

namespace Albatross.CommandLine.Outputs {
	public static class Extensions {
		/// <summary>
		/// Shared serializer for CLI output: camelCase property names and enums rendered as their names rather
		/// than numeric values.
		/// </summary>
		public static readonly JsonSerializer Serializer = JsonSerializer.Create(new JsonSerializerSettings {
			ContractResolver = new CamelCasePropertyNamesContractResolver(),
			Converters = {
				new StringEnumConverter(),
			},
			NullValueHandling = NullValueHandling.Ignore,
		});

		/// <summary>Spectre console bound to standard error, used when <c>Print</c> writes to stderr.</summary>
		static readonly IAnsiConsole ErrorConsole = AnsiConsole.Create(new AnsiConsoleSettings {
			Out = new AnsiConsoleOutput(Console.Error),
		});

		/// <summary>
		/// Renders <paramref name="value"/> to the console as syntax-highlighted JSON.  When
		/// <paramref name="expression"/> is supplied the value is first transformed by the JmesPath expression.
		/// A JSON object or array is printed indented; a scalar result (string, number, boolean) is written raw
		/// so it can be piped or captured by scripts.  Set <paramref name="stderr"/> to write to standard error
		/// instead of standard output.  Color is emitted only to a terminal — Spectre.Console strips ANSI when
		/// the stream is redirected or piped, so a tool/AI consumer still receives plain JSON.
		/// </summary>
		public static void Print<T>(this T value, JmesPathExpression? expression, bool compact, bool stderr = false) {
			if(value == null) {
				return;
			}
			JToken token = JToken.FromObject(value, Serializer);
			if (expression != null) {
				token = expression.Transform(token).AsJToken();
			}
			if (token is JContainer && !compact) {
				var console = stderr ? ErrorConsole : AnsiConsole.Console;
				var stringStyle = stderr ? Color.Red : Style.Plain;
				console.Write(new JsonText(token.ToString(Formatting.None)) {
					StringStyle = stringStyle,
					Indentation = "  "
				});
				console.WriteLine();
			} else {
				// Compact: minified single-line JSON for objects/arrays. Scalars are always written raw
				// (already single-line) so they can be captured directly by scripts.
				var json = token is JContainer ? token.ToString(Formatting.None) : token.ToString();
				(stderr ? Console.Error : Console.Out).WriteLine(json);
			}
		}

		/// <summary>
		/// Returns whether compact output was requested. Safe for commands that do not declare the
		/// <c>--compact</c> option: looks up the option instance and returns false when it is absent
		/// (reading a missing symbol by name throws in System.CommandLine).
		/// </summary>
		public static bool IsCompact(this ParseResult result) {
			var option = result.CommandResult.Command.Options.OfType<CompactOption>().FirstOrDefault();
			return option != null && result.GetValue(option);
		}

		/// <summary>
		/// Prints a success <see cref="CommandOutput"/> (exit code 0) to stdout for a command that completed
		/// successfully but has no data to return. The command key is taken from the parse result.
		/// </summary>
		public static void PrintSuccess(this ParseResult result, string? message = null) {
			new CommandOutput {
				Command = result.CommandResult.Command.GetCommandKey(),
				ExitCode = 0,
				Message = message,
			}.Print(null, result.IsCompact());
		}

		/// <summary>
		/// Prints a failure <see cref="CommandOutput"/> to stderr for a command that detected an error itself
		/// (rather than throwing). The command key is taken from the parse result.
		/// </summary>
		public static void PrintError(this ParseResult result, string error, string? detail = null, string? message = null, int exitCode = 1) {
			new CommandOutput {
				Command = result.CommandResult.Command.GetCommandKey(),
				ExitCode = exitCode,
				Error = error,
				ErrorDetail = detail,
				Message = message,
			}.Print(null, result.IsCompact(), stderr: true);
		}
	}
}
