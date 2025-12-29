using Albatross.CodeAnalysis;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Albatross.CommandLine.CodeGen {
	public record class CommandSetup {
		public const string CommandClassPostfix = "Command";
		public string Key { get; }
		public string Name { get; }
		public INamedTypeSymbol OptionsClass { get; }
		public INamedTypeSymbol? BaseOptionsClass { get; }
		public ITypeSymbol? HandlerClass { get; }
		public string CommandClassName { get; private set; }
		public string CommandClassNamespace => OptionsClass.ContainingNamespace.GetFullNamespace();
		public string? Description { get; }
		public string[] Aliases { get; } = Array.Empty<string>();
		public CommandParameterSetup[] Parameters { get; }

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

			if (verbAttribute.TryGetNamedArgument("UseBaseOptionsClass", out var typedConstant)) {
				this.BaseOptionsClass = typedConstant.Value as INamedTypeSymbol;
			}
			if (verbAttribute.TryGetNamedArgument("Description", out typedConstant)) {
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
			if (!optionsClassName.EndsWith(CommandClassPostfix, StringComparison.InvariantCultureIgnoreCase)) {
				optionsClassName = optionsClassName + CommandClassPostfix;
			}
			return optionsClassName;
		}

		public void RenameCommandClass(int index) {
			if (index != 0) {
				CommandClassName = $"{GetCommandClassName(this.OptionsClass)}{index}";
			}
		}

		public IEnumerable<CommandParameterSetup> GetCommandParameters(Compilation compilation) {
			var propertySymbols = OptionsClass.GetDistinctProperties(true).ToArray();
			int index = 0;
			foreach (var propertySymbol in propertySymbols) {
				index++;
				foreach (var attributeData in propertySymbol.GetAttributes()) {
					if (attributeData.AttributeClass.Is(compilation.ArgumentAttributeClass())) {
						yield return new CommandArgumentParameterSetup(compilation, propertySymbol, attributeData) {
							Index = index,
						};
					} else if (attributeData.AttributeClass.Is(compilation.OptionAttributeClass())) {
						yield return new CommandOptionParameterSetup(compilation, propertySymbol, attributeData) {
							Index = index,
						};
					} else if ((attributeData.AttributeClass.Is(compilation.UseOptionAttributeClassGeneric1())
					            || attributeData.AttributeClass.Is(compilation.UseArgumentAttributeClassGeneric1()))
					           && attributeData.AttributeClass?.TypeArguments.Length == 1) {
						//TODO: provide a warning if the symbol has incorrect base class
						var symbol = attributeData.AttributeClass!.TypeArguments[0] as INamedTypeSymbol;
						symbol!.TryGetAttribute(compilation.DefaultActionHandlerAttributeClass(), out var handlerAttribute);
						yield return new CommandOptionParameterSetup(compilation, propertySymbol, attributeData!) {
							Index = index,
							ExplicitParameterClass = symbol,
							ExplicitParameterHandlerClass = handlerAttribute?.AttributeClass,
						};
					} else if ((attributeData.AttributeClass.Is(compilation.UseOptionAttributeClassGeneric2())
					            || attributeData.AttributeClass.Is(compilation.UseArgumentAttributeClassGeneric2()))
					           && attributeData.AttributeClass?.TypeArguments.Length == 2) {
						//TODO: provide a warning if the symbol has incorrect base class
						var symbol = attributeData.AttributeClass!.TypeArguments[0] as INamedTypeSymbol;
						var handler = attributeData.AttributeClass!.TypeArguments[1] as INamedTypeSymbol;
						yield return new CommandOptionParameterSetup(compilation, propertySymbol, attributeData!) {
							Index = index,
							ExplicitParameterClass = symbol,
							ExplicitParameterHandlerClass = handler,
						};
					} else {
						continue;
					}
					break;
				}
			}
		}
	}
}