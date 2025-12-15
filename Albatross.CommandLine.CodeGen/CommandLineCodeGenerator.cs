using Albatross.CodeAnalysis.Symbols;
using Albatross.CodeGen;
using Albatross.CodeGen.CSharp.Declarations;
using Albatross.CodeGen.CSharp.Expressions;
using Albatross.CodeGen.CSharp.TypeConversions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Albatross.CommandLine.CodeGen {
	[Generator]
	public class CommandLineCodeGenerator : IIncrementalGenerator {
		public void Initialize(IncrementalGeneratorInitializationContext context) {
			var compilationProvider = context.CompilationProvider.Select(static (x, _) => x);
			var commandSetups = context.SyntaxProvider.ForAttributeWithMetadataName(
					fullyQualifiedMetadataName: MySymbolProvider.VerbAttributeClassName,
					predicate: static (_, _) => true,
					transform: static (ctx, _) => ctx)
				.Combine(compilationProvider).Select(static (tuple, cancellationToken) => {
					var (context, compilation) = tuple;
					var list = new List<CommandSetup>();
					foreach (var attribute in context.Attributes) {
						if (attribute.TryGetNamedArgument(My.OptionsClassProperty, out var typedConstant)) {
							if (typedConstant.Value is INamedTypeSymbol symbol) {
								list.Add(new CommandSetup(compilation, symbol, attribute));
							}
						} else if (context.TargetSymbol is INamedTypeSymbol symbol) {
							list.Add(new CommandSetup(compilation, symbol, attribute));
						}
					}
					return list;
				}).SelectMany(static (x, _) => x);

			var commandHandlers = context.SyntaxProvider.CreateSyntaxProvider(
					predicate: static (node, _) => node is ClassDeclarationSyntax,
					transform: static (ctx, _) => ctx)
				.Combine(compilationProvider).Select(static (tuple, cancellationToken) => {
					var (ctx, compilation) = tuple;
					var declaration = (ClassDeclarationSyntax)ctx.Node;
					var symbol = ctx.SemanticModel.GetDeclaredSymbol(declaration, cancellationToken);
					if (symbol != null && symbol.HasInterface(compilation.ICommandHandler_Interface()) && symbol.IsConcreteClass()) {
						return symbol;
					} else {
						return null;
					}
				}).Where(static x => x is not null);

			var commandNamespaces = context.SyntaxProvider.CreateSyntaxProvider(
				predicate: static (node, _) => node is ClassDeclarationSyntax,
				transform: static (ctx, _) => ctx).Combine(compilationProvider).Select(static (x, cancellationToken) => {
					var (ctx, symbolProvider) = x;
					var declaration = (ClassDeclarationSyntax)ctx.Node;
					var symbol = ctx.SemanticModel.GetDeclaredSymbol(declaration, cancellationToken);
					if (symbol != null && symbol.IsDerivedFrom(symbolProvider.SetupClass())) {
						return symbol.ContainingNamespace.GetFullNamespace();
					} else {
						return null;
					}
				}).Where(static x => x is not null);
			var aggregated = compilationProvider
				.Combine(commandSetups.Collect())
				.Select(static (x, _) => (Compilation: x.Left, optionClasses: x.Right))
				.Combine(commandHandlers.Collect())
				.Select(static (x, _) => (x.Left.Compilation, x.Left.optionClasses, CommandHandlers: x.Right))
				.Combine(commandNamespaces.Collect())
				.Select(static (x, _) => (x.Left.Compilation, x.Left.optionClasses, x.Left.CommandHandlers, Setups: x.Right));

			context.RegisterSourceOutput(
				aggregated,
				static (context, data) => {
					var (compilation, commandSetups, handlers, commandNamespaces) = data;
					var builder = new CommandClassBuilder(compilation, new DefaultTypeConverter(compilation));
					foreach (var group in commandSetups.GroupBy(x => x.CommandClassName)) {
						if (group.Count() > 1) {
							int index = 0;
							foreach (var item in group) {
								item.RenameCommandClass(index++);
							}
						}
					}
					foreach (var setup in commandSetups) {
						var file = new FileDeclaration($"{setup.CommandClassName}.g") {
							Namespace = new NamespaceExpression(setup.CommandClassNamespace),
							Classes = [
								builder.Convert(setup)
							]
						};
						var writer = new StringWriter();
						writer.Code(file);
						context.AddSource(file.FileName, writer.ToString());
					}
				});
		}
	}
}