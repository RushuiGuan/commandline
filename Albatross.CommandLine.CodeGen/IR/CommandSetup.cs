using Albatross.CodeAnalysis;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Albatross.CommandLine.CodeGen.IR {
	public record class CommandSetup {
		public const string CommandClassPostfix = "Command";
		public string Key { get; }
		public string Name { get; }
		public INamedTypeSymbol ParamsClass { get; }
		public INamedTypeSymbol? BaseParamsClass { get; }
		public ITypeSymbol? HandlerClass { get; }
		public string CommandClassName { get; private set; }
		public string CommandClassNamespace => ParamsClass.ContainingNamespace.GetFullNamespace();
		public string? Description { get; }
		public string[] Aliases { get; } = Array.Empty<string>();
		public CommandParameterSetup[] Parameters { get; }

		const string Postfix_Params = "Params";

		public CommandSetup(Compilation compilation, INamedTypeSymbol parametersClass, AttributeData verbAttribute)
			: this(compilation, parametersClass, null, verbAttribute) {
		}

		public CommandSetup(Compilation compilation, INamedTypeSymbol parametersClass, ITypeSymbol? handlerClass, AttributeData verbAttribute) {
			this.ParamsClass = parametersClass;
			this.CommandClassName = GetCommandClassName(parametersClass);
			this.HandlerClass = handlerClass;

			if (verbAttribute.ConstructorArguments.Length > 0) {
				this.Key = verbAttribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
			} else {
				this.Key = "MissingCommandKey";
			}
			this.Name = this.Key.Split(' ').Last();

			if (verbAttribute.TryGetNamedArgument("BaseParamsClass", out var typedConstant)) {
				this.BaseParamsClass = typedConstant.Value as INamedTypeSymbol;
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
		/// Command class name is derived from the parameters class name by:
		/// 1. Remove the postfix "Params" if exists
		/// 2. Append "Command" if the remaining string does not end with "Command"
		/// </summary>
		public static string GetCommandClassName(INamedTypeSymbol optionClass) {
			string parametersClassName = optionClass.Name;
			if (parametersClassName.EndsWith(Postfix_Params, StringComparison.InvariantCultureIgnoreCase)) {
				parametersClassName = parametersClassName.Substring(0, parametersClassName.Length - Postfix_Params.Length);
			}
			if (!parametersClassName.EndsWith(CommandClassPostfix, StringComparison.InvariantCultureIgnoreCase)) {
				parametersClassName = parametersClassName + CommandClassPostfix;
			}
			return parametersClassName;
		}

		public void RenameCommandClass(int index) {
			if (index != 0) {
				CommandClassName = $"{GetCommandClassName(this.ParamsClass)}{index}";
			}
		}


		public IEnumerable<CommandParameterSetup> GetCommandParameters(Compilation compilation) {
			var propertySymbols = ParamsClass.GetDistinctProperties(true).ToArray();
			int index = 0;
			foreach (var propertySymbol in propertySymbols) {
				index++;
				foreach (var attributeData in propertySymbol.GetAttributes()) {
					var attributeClass = attributeData.AttributeClass;
					if (attributeClass == null) {
						continue;
					}
					if (attributeClass.Is(compilation.ArgumentAttributeClass())) {
						yield return new CommandArgumentParameterSetup(compilation, propertySymbol, attributeData);
					} else if (attributeClass.Is(compilation.OptionAttributeClass())) {
						yield return new CommandOptionParameterSetup(compilation, propertySymbol, attributeData, null, null, false);
					} else if (Extensions.Is(attributeClass?.OriginalDefinition, compilation.UseOptionAttributeClassGeneric())) {
						var symbol = attributeClass!.TypeArguments[0] as INamedTypeSymbol;
						var useCustomName = attributeData.TryGetNamedArgument("UseCustomName", out var constant) && Convert.ToBoolean(constant.Value);
						yield return new CommandOptionParameterSetup(compilation, propertySymbol, attributeData!, symbol, useCustomName);
					} else if (Extensions.Is(attributeData.AttributeClass?.OriginalDefinition, compilation.UseArgumentAttributeClassGeneric())) {
						// argument symbol does not have Action
						var symbol = attributeData.AttributeClass!.TypeArguments[0] as INamedTypeSymbol;
						yield return new CommandArgumentParameterSetup(compilation, propertySymbol, attributeData!) {
							ExplicitParameterClass = symbol,
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