using Albatross.CodeAnalysis;
using Humanizer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace Albatross.CommandLine.CodeGen.IR {
	public abstract record class CommandParameterSetup {
		private readonly Compilation compilation;
		public IPropertySymbol PropertySymbol { get; }
		public ExpressionSyntax? PropertyInitializer { get; }
		public bool DefaultToInitializer { get; }
		public string Key { get; protected set; }
		public string? Description { get; }
		public bool Hidden { get; }
		public bool ShouldDefaultToInitializer => DefaultToInitializer && PropertyInitializer != null;
		public abstract string CommandPropertyName { get; }

		public INamedTypeSymbol ParameterClass => ExplicitParameterClass ?? DefaultParameterClass;
		public INamedTypeSymbol? ExplicitParameterClass { get; init; }
		public abstract INamedTypeSymbol DefaultParameterClass { get; }
		public bool UseContextValue => ExplicitParameterClass != null && ExplicitParameterClass.HasInterface(compilation.IUseContextValueInterface());

		protected CommandParameterSetup(Compilation compilation, IPropertySymbol propertySymbol, AttributeData propertyAttribute) {
			this.compilation = compilation;
			this.PropertySymbol = propertySymbol;
			this.Key = propertySymbol.Name.Kebaberize();

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