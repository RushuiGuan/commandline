using System;
using System.Globalization;
using System.Text;

namespace Albatross.CommandLine.CodeGen;

public static partial class StringExtensions {
	/// <summary>
	/// Converts a string to kebab-case using conservative identifier splitting.
	/// </summary>
	/// <param name="input">The string to transform. Must not be null.</param>
	/// <returns>A lowercase string with words separated by a single hyphen.</returns>
	/// <remarks>
	/// Spaces, underscores, and hyphens are normalized to a single hyphen.
	/// Word boundaries are inserted for lower-to-upper transitions such as <c>someProperty</c> to <c>some-property</c>.
	/// Uppercase runs are preserved as a single token unless the final uppercase letter starts a PascalCase word,
	/// such as <c>CUSIPCode</c> to <c>cusip-code</c> and <c>BBYellowKey</c> to <c>bb-yellow-key</c>.
	/// </remarks>
	/// <example>
	/// <code>
	/// "SomePropertyName".Kebaberize() => "some-property-name"
	/// "some_property_name".Kebaberize() => "some-property-name"
	/// "CUSIPCode".Kebaberize() => "cusip-code"
	/// "BBYellowKey".Kebaberize() => "bb-yellow-key"
	/// </code>
	/// </example>
	public static string Kebaberize(this string input) {
		var textInfo = CultureInfo.InvariantCulture.TextInfo;
		var builder = new StringBuilder(Math.Max(1, input.Length * 2));
		var precedingSeparator = false;
		var priorUpper = false;

		for (var i = 0; i < input.Length; i++) {
			var c = input[i];
			if (c == '-' || c == '_' || char.IsWhiteSpace(c)) {
				if (!precedingSeparator) {
					builder.Append('-');
				}
				precedingSeparator = true;
				continue;
			}

			if (char.IsUpper(c)) {
				if (!precedingSeparator && i != 0 && (!priorUpper || input.IsLower(i + 1))) {
					builder.Append("-");
					precedingSeparator = true;
				}
				builder.Append(textInfo.ToLower(c));
				priorUpper = true;
				precedingSeparator = false;
			} else {
				precedingSeparator = false;
				builder.Append(textInfo.ToLower(c));
				priorUpper = false;
			}
		}

		return builder.ToString();
	}
	static bool IsLower(this string input, int index) {
		if (input.Length <= index) {
			return false;
		} else {
			return char.IsLower(input, index);
		}
	}
}
