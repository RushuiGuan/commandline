using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;

namespace Albatross.CommandLine.CodeAnalysis {
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class DuplicateOptionNameAnalyzer : DiagnosticAnalyzer {
		public const string DiagnosticId = "ACL0001";

		private const string AnnotationsNamespace = "Albatross.CommandLine.Annotations";

		private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			id: DiagnosticId,
			title: "Duplicate option name",
			messageFormat: "Property '{0}' resolves to option name '{1}', which is already used by property '{2}' in class '{3}'",
			category: "Usage",
			defaultSeverity: DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			description: "Two or more options on a [Verb] params class resolve to the same CLI option name. Option names are derived from the kebab-cased property name, or - for [UseOption<T>] - from [DefaultNameAliases] on the option type. Duplicate names compile and generate cleanly but throw at runtime in System.CommandLine (\"more than one child named ...\").");

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

			// Resolve each option property to the effective CLI name the code generator will emit, then group by
			// that name.  Comparing effective names (not property names) is what makes this correct for both
			// [UseOption<T>] reuse and property names that differ only by case (e.g. MyTest -> --my-test vs
			// mytest -> --mytest, which do NOT collide).
			var options = new List<(IPropertySymbol Property, string Name)>();
			foreach (var member in namedType.GetMembers()) {
				if (member is IPropertySymbol property
					&& property.DeclaredAccessibility == Accessibility.Public
					&& TryGetEffectiveOptionName(property, out var optionName)) {
					options.Add((property, optionName));
				}
			}

			// CLI names are matched case-sensitively (ordinal) by System.CommandLine.
			foreach (var group in options.GroupBy(x => x.Name, StringComparer.Ordinal).Where(g => g.Count() > 1)) {
				var ordered = group.OrderBy(x => x.Property.Name, StringComparer.Ordinal).ToList();
				var first = ordered[0];
				foreach (var duplicate in ordered.Skip(1)) {
					var location = duplicate.Property.Locations.FirstOrDefault();
					if (location != null) {
						context.ReportDiagnostic(Diagnostic.Create(Rule, location,
							duplicate.Property.Name, group.Key, first.Property.Name, namedType.Name));
					}
				}
			}
		}

		/// <summary>
		/// Mirrors the option-name resolution in the code generator (CommandOptionParameterSetup /
		/// CommandParameterSetup): the default name is the kebab-cased property name; a [UseOption&lt;T&gt;] whose
		/// option type carries [DefaultNameAliases] uses that name unless UseCustomName is set.  Aliases are not
		/// considered here (this catches primary-name collisions only).
		/// </summary>
		private static bool TryGetEffectiveOptionName(IPropertySymbol property, out string name) {
			name = string.Empty;
			foreach (var attribute in property.GetAttributes()) {
				var attrClass = attribute.AttributeClass;
				if (attrClass == null) {
					continue;
				}
				var def = attrClass.IsGenericType ? attrClass.OriginalDefinition : attrClass;
				if (def.ContainingNamespace?.ToDisplayString() != AnnotationsNamespace) {
					continue;
				}

				// [Option(...)] - constructor args are aliases, not the primary name.
				if (def.MetadataName == "OptionAttribute") {
					name = Prefix(Kebaberize(property.Name));
					return true;
				}

				// [UseOption<TOption>] - the name may come from [DefaultNameAliases] on TOption.
				if (def.MetadataName == "UseOptionAttribute`1") {
					var useCustomName = TryGetNamedBool(attribute, "UseCustomName");
					string? nameFromOptionType = null;
					if (!useCustomName && attrClass.TypeArguments.Length == 1
						&& attrClass.TypeArguments[0] is INamedTypeSymbol optionType) {
						nameFromOptionType = GetDefaultNameAliasesName(optionType);
					}
					name = Prefix(nameFromOptionType ?? Kebaberize(property.Name));
					return true;
				}
			}
			return false;
		}

		private static string? GetDefaultNameAliasesName(INamedTypeSymbol optionType) {
			foreach (var attribute in optionType.GetAttributes()) {
				var attrClass = attribute.AttributeClass;
				if (attrClass?.ContainingNamespace?.ToDisplayString() == AnnotationsNamespace
					&& attrClass.MetadataName == "DefaultNameAliasesAttribute"
					&& attribute.ConstructorArguments.Length > 0) {
					return attribute.ConstructorArguments[0].Value as string;
				}
			}
			return null;
		}

		private static bool TryGetNamedBool(AttributeData attribute, string name) {
			foreach (var namedArg in attribute.NamedArguments) {
				if (namedArg.Key == name && namedArg.Value.Value is bool value) {
					return value;
				}
			}
			return false;
		}

		// The generator prefixes the key with "--" only when it is not already so prefixed.
		private static string Prefix(string key) => key.StartsWith("--") ? key : "--" + key;

		// Mirrors Humanizer's string.Kebaberize() (Underscore().Replace('_','-')), which the generator uses via
		// CommandParameterSetup: `this.Key = propertySymbol.Name.Kebaberize()`.  Kept in sync by hand - see
		// project-mgmt/detect-duplicate-option-names.tsk.md for the drift caveat.
		private static string Kebaberize(string input) {
			var underscored = Regex.Replace(
				Regex.Replace(
					Regex.Replace(input, @"([\p{Lu}]+)([\p{Lu}][\p{Ll}])", "$1_$2"),
					@"([\p{Ll}\d])([\p{Lu}])", "$1_$2"),
				@"[-\s]", "_").ToLowerInvariant();
			return underscored.Replace('_', '-');
		}

		private static bool HasVerbAttribute(INamedTypeSymbol type) {
			foreach (var attribute in type.GetAttributes()) {
				var attrClass = attribute.AttributeClass;
				if (attrClass == null) continue;
				var def = attrClass.IsGenericType ? attrClass.OriginalDefinition : attrClass;
				if (def.ContainingNamespace?.ToDisplayString() != AnnotationsNamespace) continue;
				// MetadataName uses backtick format: VerbAttribute, VerbAttribute`1, VerbAttribute`2
				if (def.MetadataName == "VerbAttribute" || def.MetadataName == "VerbAttribute`1" || def.MetadataName == "VerbAttribute`2") {
					return true;
				}
			}
			return false;
		}
	}
}
