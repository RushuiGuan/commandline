using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace Albatross.CommandLine.CodeAnalysis {
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class OptionHandlerTargetAnalyzer : DiagnosticAnalyzer {
		public const string DiagnosticId = "ACL0003";

		private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			id: DiagnosticId,
			title: "OptionHandlerAttribute TOption must be the attributed class or one of its base classes",
			messageFormat: "The attributed class '{1}' is not assignable to TOption '{0}'",
			category: "Usage",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "The first type argument TOption of OptionHandlerAttribute must be the attributed class itself or one of its base classes.");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context) {
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
		}

		private static void AnalyzeNamedType(SymbolAnalysisContext context) {
			var namedType = (INamedTypeSymbol)context.Symbol;

			foreach (var attribute in namedType.GetAttributes()) {
				var attrClass = attribute.AttributeClass;
				if (attrClass == null || !attrClass.IsGenericType) continue;

				var def = attrClass.OriginalDefinition;
				if (def.ContainingNamespace?.ToDisplayString() != "Albatross.CommandLine.Annotations") continue;
				if (def.MetadataName != "OptionHandlerAttribute`2" && def.MetadataName != "OptionHandlerAttribute`3") continue;

				var tOption = attrClass.TypeArguments[0] as INamedTypeSymbol;
				if (tOption == null) continue;

				if (!IsAssignableTo(namedType, tOption)) {
					var location = attribute.ApplicationSyntaxReference?.GetSyntax()?.GetLocation()
						?? namedType.Locations.FirstOrDefault();
					if (location != null) {
						context.ReportDiagnostic(Diagnostic.Create(Rule, location, tOption.Name, namedType.Name));
					}
				}
			}
		}

		// Returns true if type is the same as target or derives from it (class inheritance only).
		private static bool IsAssignableTo(INamedTypeSymbol type, INamedTypeSymbol target) {
			var current = (INamedTypeSymbol?)type;
			while (current != null) {
				if (SymbolEqualityComparer.Default.Equals(current, target)) return true;
				current = current.BaseType;
			}
			return false;
		}
	}
}
