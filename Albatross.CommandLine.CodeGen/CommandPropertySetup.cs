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
		public ExpressionSyntax? PropertyInitializer { get; }
		public bool DefaultToInitializer { get; }
		public string Key { get; protected set; }
		public string Type { get; }
		public string? Description { get; }
		public bool Hidden { get; }
		public bool ShouldDefaultToInitializer => DefaultToInitializer && PropertyInitializer != null;
		public abstract string CommandPropertyName { get; }
		
		protected CommandPropertySetup(IPropertySymbol propertySymbol, AttributeData propertyAttribute) {
			this.PropertySymbol = propertySymbol;
			this.propertyAttribute = propertyAttribute;

			this.Key = propertySymbol.Name.Kebaberize();
			this.Type = propertySymbol.Type.GetFullName();

			if (propertyAttribute.TryGetNamedArgument("DefaultToInitializer", out var defaultToInitializer)) {
				this.DefaultToInitializer = Convert.ToBoolean(defaultToInitializer.Value);
				if (this.DefaultToInitializer) {
					this.PropertyInitializer = GetPropertyInitializer(propertySymbol);
				}
			}
			if (propertyAttribute.TryGetNamedArgument("Hidden", out var hidden)) {
				this.Hidden = Convert.ToBoolean(hidden.Value);
			}
			if (propertyAttribute.TryGetNamedArgument("Description", out var descriptionConstant)) {
				this.Description = descriptionConstant.Value?.ToString();
			}
		}

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