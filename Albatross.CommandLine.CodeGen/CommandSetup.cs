using Albatross.CodeAnalysis.Symbols;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Albatross.CommandLine.CodeGen {
	public record class CommandSetup {
		public string Key { get; }
		public string Name { get; }
		public INamedTypeSymbol OptionsClass { get; }
		public ITypeSymbol? HandlerClass { get; }
		public string CommandClassName { get; private set; }
		public string CommandClassNamespace => OptionsClass.ContainingNamespace.GetFullNamespace();
		public string? Description { get; }
		public string[] Aliases { get; } = Array.Empty<string>();
		public CommandPropertySetup[] Parameters { get; }

		const string Postfix_Options = "Options";

		public CommandSetup(Compilation compilation, INamedTypeSymbol optionsClass, AttributeData verbAttribute)
			: this(compilation, optionsClass, null, verbAttribute) {
		}

		public CommandSetup(Compilation compilation, INamedTypeSymbol optionsClass, ITypeSymbol? handlerClass, AttributeData verbAttribute) {
			this.OptionsClass = optionsClass;
			this.CommandClassName = GetCommandClassName(optionsClass);
			this.HandlerClass = handlerClass;

			if (verbAttribute.ConstructorArguments.Length > 0) {
				this.Key = verbAttribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
			} else {
				this.Key = "MissingCommandKey";
			}
			this.Name = this.Key.Split(' ').Last();

			if (verbAttribute.TryGetNamedArgument("Description", out var typedConstant)) {
				this.Description = typedConstant.Value?.ToString();
			}
			if (verbAttribute.TryGetNamedArgument("Alias", out typedConstant)) {
				this.Aliases = typedConstant.Values.Select(x => x.Value?.ToString() ?? string.Empty).ToArray();
			}
			this.Parameters = GetCommandParameters(compilation).ToArray();
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
				CommandClassName = $"{GetCommandClassName(this.OptionsClass)}{index}";
			}
		}

		public IEnumerable<CommandPropertySetup> GetCommandParameters(Compilation compilation) {
			var propertySymbols = OptionsClass.GetDistinctProperties(true).ToArray();
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