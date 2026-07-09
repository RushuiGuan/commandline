using Albatross.CodeAnalysis;
using Albatross.CodeGen;
using Albatross.CodeGen.CSharp;
using Albatross.CodeGen.CSharp.Declarations;
using Albatross.CodeGen.CSharp.Expressions;
using Albatross.CodeGen.CSharp.TypeConversions;
using Albatross.CommandLine.CodeGen.IR;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Albatross.CommandLine.CodeGen {
	/// <summary>
	/// TODO: code gen can be created for manual command too.  user doesn't need to wire it up manually
	/// TODO: Need a warning if the Params Class does derive from the value of VerbAttribute.BaseParamsClass
	/// 
	/// </summary>
	[Generator]
	public class CommandLineCodeGenerator : IIncrementalGenerator {
		static IncrementalValuesProvider<CommandSetup> CreateCommandSetups(IncrementalGeneratorInitializationContext context,
			IncrementalValueProvider<Compilation> compilationProvider,
			string attributeMetadataName) {
			return context.SyntaxProvider.ForAttributeWithMetadataName(
					fullyQualifiedMetadataName: attributeMetadataName,
					predicate: static (_, _) => true,
					transform: static (ctx, _) => ctx)
				.Combine(compilationProvider).Select(static (tuple, _) => {
					var (ctx, compilation) = tuple;
					var list = new List<CommandSetup>();
					foreach (var attribute in ctx.Attributes) {
						var attribueClass = attribute.AttributeClass;
						if (attribueClass != null) {
							if (ctx.TargetSymbol is INamedTypeSymbol parametersClass) {
								if (!attribueClass.IsGenericType) {
									// this one matches [Verb("commandKey")] target Params Class Only
									list.Add(new CommandSetup(compilation, parametersClass, attribute));
								} else if (attribueClass.TypeArguments.Length == 1) {
									// this one matches [Verb<THandler>("commandKey")] target Params Class Only with Handler Type Argument
									list.Add(new CommandSetup(compilation, parametersClass, attribueClass.TypeArguments[0], attribute));
								}
							} else if (attribueClass.TypeArguments.Length == 2 && attribueClass.TypeArguments[0] is INamedTypeSymbol namedTypeSymbol) {
								// this one matches [Verb<THandler, TParams>("commandKey")] target assembly Only with Handler Type and Params Type Argument
								list.Add(new CommandSetup(compilation, namedTypeSymbol, attribueClass.TypeArguments[1], attribute));
							}
						}
					}
					return list;
				}).SelectMany(static (x, _) => x);
		}

		public void Initialize(IncrementalGeneratorInitializationContext context) {
			//System.Diagnostics.Debugger.Launch();
			var compilationProvider = context.CompilationProvider.Select(static (x, _) => x);
			var commandSetups = CreateCommandSetups(context, compilationProvider, MySymbolProvider.VerbAttributeClassName);
			var commandSetups1 = CreateCommandSetups(context, compilationProvider, MySymbolProvider.VerbAttributeClassNameGeneric1);
			var commandSetups2 = CreateCommandSetups(context, compilationProvider, MySymbolProvider.VerbAttributeClassNameGeneric2);

			// The RootNamespace is the deterministic home for the generated CodeGenExtensions class.  It is read
			// from the project's build properties rather than derived from the compilation entry point: a foreign
			// entry point (e.g. the one xunit.v3 injects into test projects) would otherwise dictate the namespace
			// and hide the generated RegisterCommands()/AddCommands() extension methods from the consuming code.
			var rootNamespaceProvider = context.AnalyzerConfigOptionsProvider.Select(static (provider, _) =>
				provider.GlobalOptions.TryGetValue("build_property.RootNamespace", out var ns) ? ns : null);

			var aggregated = compilationProvider.Combine(commandSetups.Collect()).Select((tuple, _) => {
				var (compilation, setups) = tuple;
				return (compilation, setups);
			}).Combine(commandSetups1.Collect()).Select((tuple, _) => {
				var ((compilation, setups), setups1) = tuple;
				return (compilation, setups.AddRange(setups1));
			}).Combine(commandSetups2.Collect()).Select((tuple, _) => {
				var ((compilation, setups), setups2) = tuple;
				return (compilation, setups.AddRange(setups2));
			}).Combine(rootNamespaceProvider).Select((tuple, _) => {
				var ((compilation, setups), rootNamespace) = tuple;
				return (compilation, setups, rootNamespace);
			});

			context.RegisterSourceOutput(
				aggregated,
				static (context, data) => {
					var typeConverter = new DefaultTypeConverter(new Dictionary<string, string>());
					var (compilation, commandSetups, rootNamespace) = data;
					var builder = new CommandClassBuilder(compilation, typeConverter);
					foreach (var group in commandSetups.GroupBy(x => x.CommandClassName)) {
						if (group.Count() > 1) {
							int index = 0;
							foreach (var item in group.OrderBy(x => x.Name)) {
								item.RenameCommandClass(index++);
							}
						}
					}
					foreach (var setup in commandSetups.OrderBy(x => x.Name)) {
						var file = new FileDeclaration($"{setup.CommandClassName}.g") {
							Namespace = new NamespaceExpression(setup.CommandClassNamespace),
							Classes = [
								builder.Convert(setup)
							]
						};
						context.AddSource(file.FileName, new StringWriter().Code(file).ToString());
					}
					// Host the generated extensions under <RootNamespace>.AutoGenerated so generated wiring stays in a
					// dedicated, clearly-named segment of the consuming assembly's own namespace.  Fall back to the
					// assembly name when RootNamespace is not supplied so we never emit into the global namespace.
					var baseNamespace = string.IsNullOrEmpty(rootNamespace) ? compilation.AssemblyName : rootNamespace;
					var generatedNamespace = string.IsNullOrEmpty(baseNamespace) ? "AutoGenerated" : $"{baseNamespace}.AutoGenerated";
					var registrationFile = new FileDeclaration($"CodeGenExtensions.g") {
						Namespace = new NamespaceExpression(generatedNamespace),
						Classes = [
							new ClassDeclaration {
								Name = new IdentifierNameExpression("CodeGenExtensions"),
								AccessModifier = Defined.Keywords.Public,
								IsStatic = true,
								Methods = [
									new RegisterCommandsMethodBuilder(compilation, typeConverter).Convert(commandSetups),
									new AddCommandsMethodBuilder(typeConverter).Convert(commandSetups),
								]
							}
						]
					};
					context.AddSource(registrationFile.FileName, new StringWriter().Code(registrationFile).ToString());
				});
		}
	}
}