using Albatross.CodeAnalysis;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace Albatross.CommandLine.CodeGen.IR {
	public record class CommandOptionParameterSetup : CommandParameterSetup {
		private readonly Compilation compilation;
		public string[] Aliases { get; } = Array.Empty<string>();
		private bool? required;
		public bool Required => required ?? PropertySymbol.Type.SpecialType != SpecialType.System_Boolean
			&& !PropertySymbol.Type.IsNullable(compilation)
			&& !PropertySymbol.Type.IsCollection(compilation)
			&& !ShouldDefaultToInitializer;

		public override string CommandPropertyName => $"Option_{this.PropertySymbol.Name}";
		public override INamedTypeSymbol DefaultParameterClass { get; }
		public INamedTypeSymbol? ExplicitParameterHandlerClass { get; init; }
		public bool AllowMultipleArgumentsPerToken { get; init; }

		public CommandOptionParameterSetup(Compilation compilation, IPropertySymbol propertySymbol, AttributeData propertyAttribute, INamedTypeSymbol? explicitParamClass, INamedTypeSymbol? explicitParamHandlerClass, bool useCustomName)
			: base(compilation, propertySymbol, propertyAttribute) {
			this.compilation = compilation;
			this.ExplicitParameterClass = explicitParamClass;
			this.ExplicitParameterHandlerClass = explicitParamHandlerClass;

			if (explicitParamClass != null && !useCustomName && explicitParamClass.TryGetAttribute(compilation.DefaultNameAliasesAttribute(), out var attributeData) && attributeData.ConstructorArguments.Length > 0) {
				this.Key = Convert.ToString(attributeData.ConstructorArguments[0].Value);
				if (attributeData.ConstructorArguments.Length > 1) {
					this.Aliases = attributeData.ConstructorArguments[1].Values.Select(x => x.Value?.ToString() ?? string.Empty)
						.Where(x => !string.IsNullOrEmpty(x)).Select(CreateAlias).ToArray();
				}
			} else {
				this.Key = $"--{this.Key}";
			}
			if (propertyAttribute.ConstructorArguments.Any()) {
				var aliases = propertyAttribute.ConstructorArguments[0].Values.Select(x => x.Value?.ToString() ?? string.Empty)
					.Where(x => !string.IsNullOrEmpty(x)).Select(CreateAlias)
					.ToArray();
				if (aliases.Length > 0) {
					this.Aliases = aliases;
				}
			}
			if (propertyAttribute.TryGetNamedArgument("AllowMultipleArgumentsPerToken", out var allowMultiple)) {
				this.AllowMultipleArgumentsPerToken = Convert.ToBoolean(allowMultiple.Value);
			}
			if (propertyAttribute.TryGetNamedArgument("Required", out var required)) {
				this.required = Convert.ToBoolean(required.Value);
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