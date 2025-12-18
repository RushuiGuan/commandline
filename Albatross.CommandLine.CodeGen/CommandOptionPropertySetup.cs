using Albatross.CodeAnalysis.Symbols;
using Humanizer;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace Albatross.CommandLine.CodeGen {
	public record class CommandOptionPropertySetup : CommandPropertySetup {
		public string[] Aliases { get; }
		public bool Required { get; }
		public override string CommandPropertyName => $"Option_{this.PropertySymbol.Name}";

		public CommandOptionPropertySetup(Compilation compilation, IPropertySymbol propertySymbol, AttributeData propertyAttribute)
			: base(propertySymbol, propertyAttribute) {
			this.Key = $"--{this.Key}";
			if (propertyAttribute.ConstructorArguments.Any()) {
				this.Aliases = propertyAttribute.ConstructorArguments[0].Values.Select(x => x.Value?.ToString() ?? string.Empty)
					.Where(x => !string.IsNullOrEmpty(x)).Select(CreateAlias)
					.ToArray();
			} else {
				this.Aliases = Array.Empty<string>();
			}
			if (propertyAttribute.TryGetNamedArgument("Required", out var required)) {
				this.Required = Convert.ToBoolean(required.Value);
			} else {
				this.Required = propertySymbol.Type.SpecialType != SpecialType.System_Boolean
				                && !propertySymbol.Type.IsNullable(compilation)
				                && !propertySymbol.Type.IsCollection(compilation)
				                && !ShouldDefaultToInitializer;
			}
		}

		string CreateAlias(string text) {
			if (text.StartsWith("-")) {
				return text;
			} else if (text.Length == 1) {
				return $"-{text}";
			} else {
				return $"--{text}";
			}
		}
	}
}