using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace Albatross.CommandLine.CodeAnalysis {
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class DuplicateOptionNameAnalyzer : DiagnosticAnalyzer {
		public const string DiagnosticId = "ACL0001";

		private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			id: DiagnosticId,
			title: "Duplicate option name (case-insensitive)",
			messageFormat: "Property '{0}' in class '{1}' has the same option name as '{2}' (case-insensitive)",
			category: "Usage",
			defaultSeverity: DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			description: "Multiple properties annotated with [Option] in a [Verb] class have names that are equal when compared case-insensitively, which produces duplicate CLI options.");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context) {
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
		}

		private static void AnalyzeNamedType(SymbolAnalysisContext context) {
			var namedType = (INamedTypeSymbol)context.Symbol;

			if (!HasVerbAttribute(namedType)) {
				return;
			}

			var optionProperties = namedType.GetMembers()
				.OfType<IPropertySymbol>()
				.Where(p => p.DeclaredAccessibility == Accessibility.Public && HasOptionAttribute(p))
				.ToList();

			// group by case-insensitive property name; any group with more than one entry is a conflict
			var conflicts = optionProperties
				.GroupBy(p => p.Name.ToUpperInvariant())
				.Where(g => g.Count() > 1);

			foreach (var group in conflicts) {
				var ordered = group.OrderBy(p => p.Name).ToList();
				// the first (alphabetically) is the "original"; report all others against it
				var first = ordered[0];
				foreach (var duplicate in ordered.Skip(1)) {
					var location = duplicate.Locations.FirstOrDefault();
					if (location != null) {
						context.ReportDiagnostic(Diagnostic.Create(Rule, location, duplicate.Name, namedType.Name, first.Name));
					}
				}
			}
		}

		private static bool HasVerbAttribute(INamedTypeSymbol type) {
			foreach (var attribute in type.GetAttributes()) {
				var attrClass = attribute.AttributeClass;
				if (attrClass == null) continue;
				var def = attrClass.IsGenericType ? attrClass.OriginalDefinition : attrClass;
				if (def.ContainingNamespace?.ToDisplayString() != "Albatross.CommandLine.Annotations") continue;
				// MetadataName uses backtick format: VerbAttribute, VerbAttribute`1, VerbAttribute`2
				if (def.MetadataName == "VerbAttribute" || def.MetadataName == "VerbAttribute`1" || def.MetadataName == "VerbAttribute`2") {
					return true;
				}
			}
			return false;
		}

		private static bool HasOptionAttribute(IPropertySymbol property) {
			foreach (var attribute in property.GetAttributes()) {
				var attrClass = attribute.AttributeClass;
				if (attrClass != null
					&& attrClass.ContainingNamespace?.ToDisplayString() == "Albatross.CommandLine.Annotations"
					&& attrClass.MetadataName == "OptionAttribute") {
					return true;
				}
			}
			return false;
		}
	}
}
