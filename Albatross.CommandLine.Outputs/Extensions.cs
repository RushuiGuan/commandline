using Spectre.Console;
using Spectre.Console.Json;
using System.CommandLine;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Albatross.CommandLine.Outputs {
	public static class Extensions {
		/// <summary>
		/// Shared serializer options for CLI output: camelCase property names, enums rendered as their names
		/// rather than numeric values, and null properties omitted.
		/// </summary>
		public static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions {
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
			// output is read by terminals and scripts, not embedded in html
			Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
			Converters = {
				new JsonStringEnumConverter(),
			},
		};

		/// <summary>Spectre console bound to standard error, used when <c>Print</c> writes to stderr.</summary>
		static readonly IAnsiConsole ErrorConsole = AnsiConsole.Create(new AnsiConsoleSettings {
			Out = new AnsiConsoleOutput(Console.Error),
		});

		/// <summary>
		/// Renders <paramref name="value"/> to the console as syntax-highlighted JSON.  A JSON object or array is
		/// printed indented; a scalar result (string, number, boolean) is written raw so it can be piped or
		/// captured by scripts.  Set <paramref name="stderr"/> to write to standard error instead of standard
		/// output.  Color is emitted only to a terminal — Spectre.Console strips ANSI when the stream is
		/// redirected or piped, so a tool/AI consumer still receives plain JSON.
		/// </summary>
		public static void Print<T>(this T value, bool compact, bool stderr = false) {
			if (value == null) {
				return;
			}
			var node = JsonSerializer.SerializeToNode(value, value.GetType(), SerializerOptions);
			if (node == null) {
				return;
			}
			var isContainer = node is JsonObject or JsonArray;
			if (isContainer && !compact) {
				var console = stderr ? ErrorConsole : AnsiConsole.Console;
				var stringStyle = stderr ? Color.Red : Style.Plain;
				console.Write(new JsonText(node.ToJsonString(SerializerOptions)) {
					StringStyle = stringStyle,
					Indentation = "  "
				});
				console.WriteLine();
			} else {
				// Compact: minified single-line JSON for objects/arrays. Scalars are always written raw
				// (already single-line) so they can be captured directly by scripts.
				var json = isContainer ? node.ToJsonString(SerializerOptions) : node.ToString();
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
		public static void PrintSuccess(this ParseResult result, string message) {
			new CommandOutput {
				Command = result.CommandResult.Command.GetCommandKey(),
				ExitCode = 0,
				Message = message,
			}.Print(result.IsCompact());
		}

		public static void PrintSuccessWithData<T>(this ParseResult result, string message, T data) {
			new CommandOutput<T> {
				Command = result.CommandResult.Command.GetCommandKey(),
				ExitCode = 0,
				Message = message,
				Data = data,
			}.Print(result.IsCompact());
		}


		static ErrorOutput[]? BuildCommandHandlerErrorOutput(string? errorMessage, string? errorDetail) {
			if (string.IsNullOrEmpty(errorMessage) && string.IsNullOrEmpty(errorDetail)) {
				return null;
			} else {
				return [
					new ErrorOutput (ErrorSource.CommandHandler, null, errorMessage, errorDetail)
				];
			}
		}

		/// <summary>
		/// Prints a failure <see cref="CommandOutput"/> to stderr for a command that detected an error itself
		/// (rather than throwing). The command key is taken from the parse result.
		/// </summary>
		public static int PrintError(this ParseResult result, string message, string? errorMessage = null, string? errorDetail = null, int exitCode = 1) {
			new CommandOutput {
				Command = result.CommandResult.Command.GetCommandKey(),
				Message = message,
				ExitCode = exitCode,
				Errors = BuildCommandHandlerErrorOutput(errorMessage, errorDetail),
			}.Print(result.IsCompact(), stderr: true);
			return exitCode;
		}

		public static int PrintErrorWithData<T>(this ParseResult result, string message, T data, string? errorMessage = null, string? errorDetail = null, int exitCode = 1) {
			new CommandOutput<T> {
				Command = result.CommandResult.Command.GetCommandKey(),
				ExitCode = exitCode,
				Message = message,
				Data = data,
				Errors = BuildCommandHandlerErrorOutput(errorMessage, errorDetail),
			}.Print(result.IsCompact(), stderr: true);
			return exitCode;
		}
	}
}