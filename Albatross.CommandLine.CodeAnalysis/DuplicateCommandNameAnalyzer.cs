using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Albatross.CommandLine.CodeAnalysis {
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class DuplicateCommandNameAnalyzer : DiagnosticAnalyzer {
		public const string DiagnosticId = "ACL0005";

		private const string AnnotationsNamespace = "Albatross.CommandLine.Annotations";

		private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			id: DiagnosticId,
			title: "Duplicate command name",
			messageFormat: "Command name '{0}' is already declared at {1}; command names must be unique or AddCommands() throws at runtime",
			category: "Usage",
			defaultSeverity: DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			description: "Two or more [Verb] attributes declare the same command name (across any classes or assembly-level verbs). The generated AddCommands() adds each command to the builder by its name, so a duplicate throws ArgumentException (\"The command '...' has already been added\") at runtime.");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context) {
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			// Plain per-symbol action (same shape as DuplicateOptionNameAnalyzer) so the diagnostic shows during
			// live IDE analysis, not just at build.  Command names collide across types, so - unlike the
			// single-class ACL0001 - the check consults the whole compilation for an earlier declaration.
			context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
		}

		private static void AnalyzeNamedType(SymbolAnalysisContext context) {
			var type = (INamedTypeSymbol)context.Symbol;
			foreach (var attribute in type.GetAttributes()) {
				// class-level verbs only ([Verb] / [Verb<THandler>]); assembly-level verbs are still consulted
				// as potential earlier declarations below.
				if (!IsVerbAttribute(attribute, out var typeArgCount) || typeArgCount == 2) {
					continue;
				}
				var key = GetKey(attribute);
				var location = GetLocation(attribute);
				if (key == null || location == null) {
					continue;
				}
				// Report only when an earlier declaration of the same name exists (all-but-first), pointing at it.
				var original = FindEarlierDeclaration(context.Compilation, key, location);
				if (original != null) {
					// original is passed as an additional location so IDEs link back to it.
					context.ReportDiagnostic(Diagnostic.Create(Rule, location, new[] { original }, key, FormatPosition(original)));
				}
			}
		}

		/// <summary>
		/// Returns the earliest declaration of <paramref name="key"/> in the compilation when it is different from
		/// <paramref name="self"/> (i.e. self is a duplicate); returns null when self is itself the first declaration.
		/// </summary>
		private static Location? FindEarlierDeclaration(Compilation compilation, string key, Location self) {
			Location? earliest = null;
			foreach (var location in EnumerateVerbLocations(compilation, key)) {
				if (earliest == null || CompareByPosition(location, earliest) < 0) {
					earliest = location;
				}
			}
			return earliest != null && !SamePosition(earliest, self) ? earliest : null;
		}

		private static IEnumerable<Location> EnumerateVerbLocations(Compilation compilation, string key) {
			// Assembly.GlobalNamespace scopes to this compilation's own source (not referenced assemblies).
			foreach (var type in EnumerateTypes(compilation.Assembly.GlobalNamespace)) {
				foreach (var attribute in type.GetAttributes()) {
					if (IsVerbAttribute(attribute, out var typeArgCount) && typeArgCount != 2 && GetKey(attribute) == key) {
						var location = GetLocation(attribute);
						if (location != null) {
							yield return location;
						}
					}
				}
			}
			foreach (var attribute in compilation.Assembly.GetAttributes()) {
				if (IsVerbAttribute(attribute, out var typeArgCount) && typeArgCount == 2 && GetKey(attribute) == key) {
					var location = GetLocation(attribute);
					if (location != null) {
						yield return location;
					}
				}
			}
		}

		private static IEnumerable<INamedTypeSymbol> EnumerateTypes(INamespaceOrTypeSymbol container) {
			foreach (var member in container.GetMembers()) {
				if (member is INamespaceSymbol childNamespace) {
					foreach (var type in EnumerateTypes(childNamespace)) {
						yield return type;
					}
				} else if (member is INamedTypeSymbol type) {
					yield return type;
					foreach (var nested in EnumerateTypes(type)) {
						yield return nested;
					}
				}
			}
		}

		private static string? GetKey(AttributeData attribute) =>
			attribute.ConstructorArguments.Length > 0
				&& attribute.ConstructorArguments[0].Value is string key
				&& !string.IsNullOrEmpty(key)
			? key : null;

		private static Location? GetLocation(AttributeData attribute) =>
			attribute.ApplicationSyntaxReference?.GetSyntax()?.GetLocation();

		private static int CompareByPosition(Location a, Location b) {
			var byPath = string.CompareOrdinal(a.SourceTree?.FilePath, b.SourceTree?.FilePath);
			return byPath != 0 ? byPath : a.SourceSpan.Start.CompareTo(b.SourceSpan.Start);
		}

		private static bool SamePosition(Location a, Location b) =>
			string.Equals(a.SourceTree?.FilePath, b.SourceTree?.FilePath, StringComparison.Ordinal)
			&& a.SourceSpan.Start == b.SourceSpan.Start;

		private static string FormatPosition(Location location) {
			var span = location.GetLineSpan();
			// MSBuild-style "path(line,col)" so the conflicting declaration is identifiable from the message.
			return $"{span.Path}({span.StartLinePosition.Line + 1},{span.StartLinePosition.Character + 1})";
		}

		private static bool IsVerbAttribute(AttributeData attribute, out int typeArgCount) {
			typeArgCount = 0;
			var attrClass = attribute.AttributeClass;
			if (attrClass == null) return false;
			var def = attrClass.IsGenericType ? attrClass.OriginalDefinition : attrClass;
			if (def.ContainingNamespace?.ToDisplayString() != AnnotationsNamespace) return false;
			// MetadataName uses backtick format: VerbAttribute, VerbAttribute`1, VerbAttribute`2
			if (def.MetadataName != "VerbAttribute" && def.MetadataName != "VerbAttribute`1" && def.MetadataName != "VerbAttribute`2") return false;
			typeArgCount = attrClass.TypeArguments.Length;
			return true;
		}
	}
}
