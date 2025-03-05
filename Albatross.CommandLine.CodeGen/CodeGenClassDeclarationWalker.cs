using Albatross.CodeAnalysis.Symbols;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Albatross.CommandLine.CodeGen {
	public class CodeGenClassDeclarationWalker : CSharpSyntaxWalker {
		private readonly SemanticModel semanticModel;
		public List<INamedTypeSymbol> CommandOptionClasses { get; } = new List<INamedTypeSymbol>();
		public List<INamedTypeSymbol> CommandHandlerClasses { get; } = new List<INamedTypeSymbol>();
		public INamedTypeSymbol? SetupClass { get; private set; }

		public CodeGenClassDeclarationWalker(SemanticModel semanticModel) {
			this.semanticModel = semanticModel;
		}
		public override void VisitClassDeclaration(ClassDeclarationSyntax node) {
			FindTargets(node);
			base.VisitClassDeclaration(node);
		}
		public override void VisitRecordDeclaration(RecordDeclarationSyntax node) {
			FindTargets(node);
			base.VisitRecordDeclaration(node);
		}

		void FindTargets(TypeDeclarationSyntax node) {
			if (node.Modifiers.Any(SyntaxKind.PublicKeyword)) {
				var classSymbol = semanticModel.GetDeclaredSymbol(node);
				if (classSymbol != null && classSymbol.IsConcreteClass()) {
					if (classSymbol.TryGetAttribute(My.VerbAttributeClass, out _)) {
						CommandOptionClasses.Add(classSymbol);
					} else if (classSymbol.Interfaces.Any(x => x.GetFullName() == My.ICommandHandler_InterfaceName)) {
						CommandHandlerClasses.Add(classSymbol);
					} else if (classSymbol.IsDerivedFrom(My.SetupClassName)) {
						SetupClass = classSymbol;
					}
				}
			}
		}
	}
}