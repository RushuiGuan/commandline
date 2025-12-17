using Albatross.CodeAnalysis.Symbols;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Albatross.CommandLine.CodeGen {
	public record class CommandSetup {
		public INamedTypeSymbol OptionClass { get; }
		public AttributeData VerbAttribute { get; }

		public string Key { get; }
		public string Name { get; }
		public ITypeSymbol HandlerClass { get; }
		public string CommandClassName { get; private set; }
		public string CommandClassNamespace => OptionClass.ContainingNamespace.GetFullNamespace();
		public string? Description { get; }
		public string[] Aliases { get; } = Array.Empty<string>();
		public CommandPropertySetup[] Parameters { get; }

		const string Postfix_Options = "Options";

		public CommandSetup(Compilation compilation, INamedTypeSymbol optionClass, AttributeData verbAttribute) {
			this.OptionClass = optionClass;
			this.VerbAttribute = verbAttribute;
			this.CommandClassName = GetCommandClassName(optionClass);

			if (verbAttribute.ConstructorArguments.Length > 0) {
				this.Key = VerbAttribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
			} else {
				this.Key = "MissingCommandKey";
			}
			this.Name = this.Key.Split(' ').Last();
			if (verbAttribute.ConstructorArguments.Length > 1) {
				this.HandlerClass = (verbAttribute.ConstructorArguments[1].Value as ITypeSymbol)
				                    ?? compilation.HelpCommandAction();
			} else {
				this.HandlerClass = compilation.HelpCommandAction();
			}
			if (VerbAttribute.TryGetNamedArgument("Description", out var typedConstant)) {
				this.Description = typedConstant.Value?.ToString();
			}
			if (VerbAttribute.TryGetNamedArgument("Alias", out typedConstant)) {
				this.Aliases = typedConstant.Values.Select(x => x.Value?.ToString() ?? string.Empty).ToArray();
			}
			var useBaseClasssProperties = true;
			if (verbAttribute.TryGetNamedArgument("UseBaseClassProperties", out typedConstant)) {
				useBaseClasssProperties = Convert.ToBoolean(typedConstant.Value);
			}
			this.Parameters = GetCommandParameters(compilation, useBaseClasssProperties).ToArray();
		}

		/// <summary>
		/// Command class name is derived from the options class name by:
		/// 1. Remove the postfix "Options" if exists
		/// 2. Append "Command" if the remaining string does not end with "Command"
		/// </summary>
		public static string GetCommandClassName(INamedTypeSymbol optionClass) {
			string optionsClassName = optionClass.Name;
			if (optionsClassName.EndsWith(Postfix_Options, StringComparison.InvariantCultureIgnoreCase)) {
				optionsClassName = optionsClassName.Substring(0, optionsClassName.Length - Postfix_Options.Length);
			}
			if (!optionsClassName.EndsWith(My.CommandClassName, StringComparison.InvariantCultureIgnoreCase)) {
				optionsClassName = optionsClassName + My.CommandClassName;
			}
			return optionsClassName;
		}

		public void RenameCommandClass(int index) {
			if (index != 0) {
				CommandClassName = $"{GetCommandClassName(this.OptionClass)}{index}";
			}
		}
		public IEnumerable<CommandPropertySetup> GetCommandParameters(Compilation compilation, bool useBaseClassProperties) {
			var propertySymbols = OptionClass.GetDistinctProperties(useBaseClassProperties).ToArray();
			int index = 0;
			foreach (var propertySymbol in propertySymbols) {
				index++;
				if (propertySymbol.TryGetAttribute(compilation.ArgumentAttributeClass(), out AttributeData? attributeData)) {
					yield return new CommandArgumentPropertySetup(compilation, propertySymbol, attributeData!) {
						Index = index,
					};
				} else if (propertySymbol.TryGetAttribute(compilation.OptionAttributeClass(), out attributeData)) {
					yield return new CommandOptionPropertySetup(compilation, propertySymbol, attributeData) {
						Index = index,
					};
				}
			}
		}
	}
}