using Albatross.CodeAnalysis;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace Albatross.CommandLine.CodeGen.IR {
	public record class CommandOptionParameterSetup : CommandParameterSetup {
		public string[] Aliases { get; }
		public bool Required { get; }
		public override string CommandPropertyName => $"Option_{this.PropertySymbol.Name}";
		public override INamedTypeSymbol DefaultParameterClass { get; }
		public INamedTypeSymbol? ExplicitParameterHandlerClass { get; init; }
		public bool AllowMultipleArgumentsPerToken { get; init; }

		public CommandOptionParameterSetup(Compilation compilation, IPropertySymbol propertySymbol, AttributeData propertyAttribute)
			: base(propertySymbol, propertyAttribute) {
			this.Key = $"--{this.Key}";
			if (propertyAttribute.ConstructorArguments.Any()) {
				this.Aliases = propertyAttribute.ConstructorArguments[0].Values.Select(x => x.Value?.ToString() ?? string.Empty)
					.Where(x => !string.IsNullOrEmpty(x)).Select(CreateAlias)
					.ToArray();
			} else {
				this.Aliases = Array.Empty<string>();
			}
			if(propertyAttribute.TryGetNamedArgument("AllowMultipleArgumentsPerToken", out var allowMultiple)) {
				this.AllowMultipleArgumentsPerToken = Convert.ToBoolean(allowMultiple.Value);
			}
			if (propertyAttribute.TryGetNamedArgument("Required", out var required)) {
				this.Required = Convert.ToBoolean(required.Value);
			} else {
				this.Required = propertySymbol.Type.SpecialType != SpecialType.System_Boolean
				                && !propertySymbol.Type.IsNullable(compilation)
				                && !propertySymbol.Type.IsCollection(compilation)
				                && !ShouldDefaultToInitializer;
			}
			this.DefaultParameterClass = compilation.OptionGenericClass().Construct(propertySymbol.Type);
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