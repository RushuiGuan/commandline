using Albatross.CodeAnalysis.Symbols;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace Albatross.CommandLine.CodeGen {
	public abstract class CommandPropertySetup {
		public CommandPropertySetup(IPropertySymbol property, AttributeData? propertyAttribute, string name) {
			this.Property = property;
			this.PropertyInitializer = GetPropertyInitializer(property);
			this.PropertyAttribute = propertyAttribute;
			this.Name = name;
			this.Type = property.Type.ToDisplayString();
			this.Hidden = false;
			this.Description = null;
			if (propertyAttribute != null) {
				if(propertyAttribute.TryGetNamedArgument("Order", out var order)) {
					this.Order = Convert.ToInt32(order.Value);
				}
				if (propertyAttribute.TryGetNamedArgument("DefaultToInitializer", out var defaultToInitializer)) {
					this.DefaultToInitializer = Convert.ToBoolean(defaultToInitializer.Value);
				}
				if (propertyAttribute.TryGetNamedArgument("Hidden", out var hidden)) {
					this.Hidden = Convert.ToBoolean(hidden.Value);
				}
				if (propertyAttribute.TryGetNamedArgument("Description", out var descriptionConstant)) {
					this.Description = descriptionConstant.Value?.ToString();
				}
			}
		}

		public int Index { get; set; }
		public int Order { get; set; }
		public IPropertySymbol Property { get; }
		public ExpressionSyntax? PropertyInitializer { get; }
		public bool DefaultToInitializer { get; }
		public AttributeData? PropertyAttribute { get; }
		public string Name { get; }
		public string Type { get; private set; }
		public string? Description { get; private set; }
		public bool Hidden { get; set; }
		public bool ShouldDefaultToInitializer => DefaultToInitializer && PropertyInitializer != null;
		public abstract string CommandPropertyName { get; }

		protected static ExpressionSyntax? GetPropertyInitializer(IPropertySymbol propertySymbol) {
			foreach (var syntaxReference in propertySymbol.DeclaringSyntaxReferences) {
				var syntaxNode = syntaxReference.GetSyntax();
				if (syntaxNode is PropertyDeclarationSyntax propertyDeclaration) {
					return propertyDeclaration.Initializer?.Value;
				}
			}
			return null;
		}
	}
}