using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace Albatross.CommandLine.CodeAnalysis {
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class ConflictingOptionArgumentAnalyzer : DiagnosticAnalyzer {
		public const string DiagnosticId = "ACL0004";

		private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			id: DiagnosticId,
			title: "Property cannot have both OptionAttribute and ArgumentAttribute",
			messageFormat: "Property '{0}' has both [Option] and [Argument] applied, which is not allowed",
			category: "Usage",
			defaultSeverity: DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			description: "A property cannot be marked as both a command-line option and a positional argument.");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context) {
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSymbolAction(AnalyzeProperty, SymbolKind.Property);
		}

		private static void AnalyzeProperty(SymbolAnalysisContext context) {
			var property = (IPropertySymbol)context.Symbol;
			var attributes = property.GetAttributes();

			bool hasOption = attributes.Any(a =>
				a.AttributeClass?.ContainingNamespace?.ToDisplayString() == "Albatross.CommandLine.Annotations"
				&& a.AttributeClass.MetadataName == "OptionAttribute");

			bool hasArgument = attributes.Any(a =>
				a.AttributeClass?.ContainingNamespace?.ToDisplayString() == "Albatross.CommandLine.Annotations"
				&& a.AttributeClass.MetadataName == "ArgumentAttribute");

			if (hasOption && hasArgument) {
				var location = property.Locations.FirstOrDefault();
				if (location != null) {
					context.ReportDiagnostic(Diagnostic.Create(Rule, location, property.Name));
				}
			}
		}
	}
}
