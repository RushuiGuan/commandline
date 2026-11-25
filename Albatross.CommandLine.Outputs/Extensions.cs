using Spectre.Console;
using Spectre.Console.Json;
using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Xml.Linq;

namespace Albatross.CommandLine.Outputs {
	public static class Extensions {
		internal const string ReflectionSerializationMessage = "Albatross.CommandLine.Outputs serializes with reflection-based System.Text.Json, which Native AOT and trimmed apps do not support.";

		/// <summary>
		/// Shared serializer options for CLI output: camelCase property names, enums rendered as their names
		/// rather than numeric values, and null properties omitted. Both instances are read-only.
		/// </summary>
		public static readonly JsonSerializerOptions CompactSerializerOptions = CreateSerializerOptions(writeIndented: false);
		public static readonly JsonSerializerOptions FormattedSerializerOptions = CreateSerializerOptions(writeIndented: true);

		static JsonSerializerOptions CreateSerializerOptions(bool writeIndented) {
			var options = new JsonSerializerOptions {
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
				DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
				// output is read by terminals and scripts, not embedded in html
				Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
				WriteIndented = writeIndented,
				Converters = {
					new JsonStringEnumConverter(),
				},
			};
			options.MakeReadOnly(populateMissingResolver: true);
			return options;
		}

		/// <summary>Spectre console bound to standard error, used when <c>Print</c> writes to stderr.</summary>
		static readonly IAnsiConsole ErrorConsole = AnsiConsole.Create(new AnsiConsoleSettings {
			Out = new AnsiConsoleOutput(Console.Error),
		});

		/// <summary>
		/// Renders <paramref name="value"/> to the console as syntax-highlighted JSON.  A JSON object or array is
		/// printed indented; a scalar result (string, number, boolean) is written raw so it can be piped or
		/// captured by scripts.  Set <paramref name="stderr"/> to write to standard error instead of standard
		/// output.  Color is emitted only to a terminal; redirected or piped output is written as plain indented
		/// JSON, so a tool/AI consumer receives valid JSON.
		/// </summary>
		[RequiresUnreferencedCode(ReflectionSerializationMessage)]
		[RequiresDynamicCode(ReflectionSerializationMessage)]
		public static void Print<T>(this T value, bool compact, bool stderr = false) {
			if (value == null) {
				return;
			}
			var type = value.GetType();
			var writer = stderr ? Console.Error : Console.Out;
			if (!ShouldPrintJson(value)) {
				writer.WriteLine(SerializeScalar(value, type));
			} else if (compact) {
				writer.WriteLine(JsonSerializer.Serialize(value, type, CompactSerializerOptions));
			} else if (stderr ? Console.IsErrorRedirected : Console.IsOutputRedirected) {
				// Spectre wraps at an 80-column fallback width when redirected, splitting long strings across lines
				writer.WriteLine(JsonSerializer.Serialize(value, type, FormattedSerializerOptions));
			} else {
				var console = stderr ? ErrorConsole : AnsiConsole.Console;
				console.Write(new JsonText(JsonSerializer.Serialize(value, type, CompactSerializerOptions)) {
					StringStyle = stderr ? Color.Red : Style.Plain,
					Indentation = "  "
				});
				console.WriteLine();
			}
		}

		[RequiresUnreferencedCode(ReflectionSerializationMessage)]
		[RequiresDynamicCode(ReflectionSerializationMessage)]
		static string SerializeScalar(object value, Type type) {
			var element = JsonSerializer.SerializeToElement(value, type, CompactSerializerOptions);
			return element.ValueKind == JsonValueKind.String ? element.GetString()! : element.GetRawText();
		}

		/// <summary>
		/// Returns true when <paramref name="value"/> serializes to a JSON object or array, which <see cref="Print"/>
		/// renders as formatted JSON; false for a scalar, which it writes raw. Decided from the runtime type, without
		/// serializing, except for the JSON DOM types whose shape is only known from their content. A type with a
		/// custom converter that writes an object is reported as false.
		/// </summary>
		[RequiresUnreferencedCode(ReflectionSerializationMessage)]
		[RequiresDynamicCode(ReflectionSerializationMessage)]
		public static bool ShouldPrintJson(object value) => value switch {
			JsonNode node => node is JsonObject or JsonArray,
			JsonElement element => element.ValueKind is JsonValueKind.Object or JsonValueKind.Array,
			JsonDocument document => document.RootElement.ValueKind is JsonValueKind.Object or JsonValueKind.Array,
			_ => CompactSerializerOptions.GetTypeInfo(value.GetType()).Kind != JsonTypeInfoKind.None,
		};

		[RequiresUnreferencedCode(ReflectionSerializationMessage)]
		[RequiresDynamicCode(ReflectionSerializationMessage)]
		public static async Task PrintAsync<T>(this T value, bool compact, CancellationToken cancellationToken, bool stderr = false) {
			if (value == null) {
				return;
			}
			var type = value.GetType();
			var text = (ShouldPrintJson(value)
				? JsonSerializer.Serialize(value, type, compact ? CompactSerializerOptions : FormattedSerializerOptions)
				: SerializeScalar(value, type)).AsMemory();
			var writer = stderr ? Console.Error : Console.Out;
			// Console's async writes run synchronously and only check the token up front, so the text is written in
			// slices with a check between them; that bounds how long Ctrl+C waits to one slice.
			for (int i = 0; i < text.Length; i += PrintChunkSize) {
				cancellationToken.ThrowIfCancellationRequested();
				await writer.WriteAsync(text.Slice(i, Math.Min(PrintChunkSize, text.Length - i)), cancellationToken);
			}
			await writer.WriteLineAsync();
		}

		const int PrintChunkSize = 16 * 1024;

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
		[RequiresUnreferencedCode(ReflectionSerializationMessage)]
		[RequiresDynamicCode(ReflectionSerializationMessage)]
		public static void PrintSuccess(this ParseResult result, string message) {
			new CommandOutput {
				Command = result.CommandResult.Command.GetCommandKey(),
				ExitCode = 0,
				Message = message,
			}.Print(result.IsCompact());
		}

		[RequiresUnreferencedCode(ReflectionSerializationMessage)]
		[RequiresDynamicCode(ReflectionSerializationMessage)]
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
		[RequiresUnreferencedCode(ReflectionSerializationMessage)]
		[RequiresDynamicCode(ReflectionSerializationMessage)]
		public static int PrintError(this ParseResult result, string message, string? errorMessage = null, string? errorDetail = null, int exitCode = 1) {
			new CommandOutput {
				Command = result.CommandResult.Command.GetCommandKey(),
				Message = message,
				ExitCode = exitCode,
				Errors = BuildCommandHandlerErrorOutput(errorMessage, errorDetail),
			}.Print(result.IsCompact(), stderr: true);
			return exitCode;
		}

		[RequiresUnreferencedCode(ReflectionSerializationMessage)]
		[RequiresDynamicCode(ReflectionSerializationMessage)]
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