using Albatross.CodeAnalysis.Symbols;
using Humanizer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;

namespace Albatross.CommandLine.CodeGen {
	public abstract record class CommandPropertySetup {
		public IPropertySymbol PropertySymbol { get; }
		protected AttributeData propertyAttribute;

		public int Index { get; init; }
		public int Order { get; }
		public object? PropertyInitializer { get; }
		public bool DefaultToInitializer { get; }
		public string Name { get; }
		public string Type { get; }
		public string? Description { get; }
		public bool Hidden { get; }
		public bool ShouldDefaultToInitializer => DefaultToInitializer && PropertyInitializer != null;
		public abstract string CommandPropertyName { get; }

		protected CommandPropertySetup(Compilation compilation, IPropertySymbol propertySymbol, AttributeData propertyAttribute) {
			this.PropertySymbol = propertySymbol;
			this.propertyAttribute = propertyAttribute;

			this.Name = $"--{propertySymbol.Name.Kebaberize()}";
			this.Type = propertySymbol.Type.GetFullName();

			if (propertyAttribute.TryGetNamedArgument("Order", out var order)) {
				this.Order = Convert.ToInt32(order.Value);
			}
			if (propertyAttribute.TryGetNamedArgument("DefaultToInitializer", out var defaultToInitializer)) {
				this.DefaultToInitializer = Convert.ToBoolean(defaultToInitializer.Value);
				if (this.DefaultToInitializer) {
					this.PropertyInitializer = GetPropertyInitializer(compilation, propertySymbol);
				}
			}
			if (propertyAttribute.TryGetNamedArgument("Hidden", out var hidden)) {
				this.Hidden = Convert.ToBoolean(hidden.Value);
			}
			if (propertyAttribute.TryGetNamedArgument("Description", out var descriptionConstant)) {
				this.Description = descriptionConstant.Value?.ToString();
			}
		}

		protected static object? GetPropertyInitializer(Compilation compilation, IPropertySymbol propertySymbol) {
			var syntax = propertySymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
			if (syntax is PropertyDeclarationSyntax { Initializer: not null } propertyDeclarationSyntax) {
				var semanticModel = compilation.GetSemanticModel(syntax.SyntaxTree);
				var constant = semanticModel.GetConstantValue(propertyDeclarationSyntax.Initializer);
				if (constant.HasValue) {
					return constant.Value;
				}
			}
			return null;
		}
	}
}