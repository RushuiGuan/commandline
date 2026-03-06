using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace Albatross.CommandLine.CodeAnalysis {
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class VerbBaseParamsClassAnalyzer : DiagnosticAnalyzer {
		public const string DiagnosticId = "ACL0002";

		private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			id: DiagnosticId,
			title: "Params class does not derive from VerbAttribute.BaseParamsClass",
			messageFormat: "'{0}' must derive from '{1}' as required by VerbAttribute.BaseParamsClass",
			category: "Usage",
			defaultSeverity: DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			description: "When VerbAttribute.BaseParamsClass is set, the params class must derive from the specified base class.",
			customTags: WellKnownDiagnosticTags.CompilationEnd);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context) {
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
			context.RegisterCompilationAction(AnalyzeAssembly);
		}

		private static void AnalyzeNamedType(SymbolAnalysisContext context) {
			var namedType = (INamedTypeSymbol)context.Symbol;

			foreach (var attribute in namedType.GetAttributes()) {
				if (!IsVerbAttribute(attribute, out var typeArgCount)) continue;
				// VerbAttribute<TParams, THandler> targets assembly — skip at class level
				if (typeArgCount == 2) continue;

				var baseParamsClass = GetBaseParamsClass(attribute);
				if (baseParamsClass == null) continue;

				if (!IsDerivedFrom(namedType, baseParamsClass)) {
					var location = attribute.ApplicationSyntaxReference?.GetSyntax()?.GetLocation()
						?? namedType.Locations.FirstOrDefault();
					if (location != null) {
						context.ReportDiagnostic(Diagnostic.Create(Rule, location, namedType.Name, baseParamsClass.Name));
					}
				}
			}
		}

		private static void AnalyzeAssembly(CompilationAnalysisContext context) {
			var assembly = context.Compilation.Assembly;

			foreach (var attribute in assembly.GetAttributes()) {
				if (!IsVerbAttribute(attribute, out var typeArgCount)) continue;
				// Only VerbAttribute<TParams, THandler> (2 type args) is valid at assembly level
				if (typeArgCount != 2) continue;

				var attrClass = attribute.AttributeClass!;
				var paramsClass = attrClass.TypeArguments[0] as INamedTypeSymbol;
				if (paramsClass == null) continue;

				var baseParamsClass = GetBaseParamsClass(attribute);
				if (baseParamsClass == null) continue;

				if (!IsDerivedFrom(paramsClass, baseParamsClass)) {
					var location = attribute.ApplicationSyntaxReference?.GetSyntax()?.GetLocation()
						?? assembly.Locations.FirstOrDefault();
					if (location != null) {
						context.ReportDiagnostic(Diagnostic.Create(Rule, location, paramsClass.Name, baseParamsClass.Name));
					}
				}
			}
		}

		private static bool IsVerbAttribute(AttributeData attribute, out int typeArgCount) {
			typeArgCount = 0;
			var attrClass = attribute.AttributeClass;
			if (attrClass == null) return false;
			var def = attrClass.IsGenericType ? attrClass.OriginalDefinition : attrClass;
			if (def.ContainingNamespace?.ToDisplayString() != "Albatross.CommandLine.Annotations") return false;
			// MetadataName uses backtick format: VerbAttribute, VerbAttribute`1, VerbAttribute`2
			if (def.MetadataName != "VerbAttribute" && def.MetadataName != "VerbAttribute`1" && def.MetadataName != "VerbAttribute`2") return false;
			typeArgCount = attrClass.TypeArguments.Length;
			return true;
		}

		private static INamedTypeSymbol? GetBaseParamsClass(AttributeData attribute) {
			foreach (var namedArg in attribute.NamedArguments) {
				if (namedArg.Key == "BaseParamsClass" && namedArg.Value.Value is INamedTypeSymbol type) {
					return type;
				}
			}
			return null;
		}

		private static bool IsDerivedFrom(INamedTypeSymbol type, INamedTypeSymbol baseType) {
			var current = type.BaseType;
			while (current != null) {
				if (SymbolEqualityComparer.Default.Equals(current, baseType)) {
					return true;
				}
				current = current.BaseType;
			}
			return false;
		}
	}
}
