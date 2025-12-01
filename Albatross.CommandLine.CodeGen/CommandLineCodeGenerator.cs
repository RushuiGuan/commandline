using Albatross.CodeAnalysis.Symbols;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Albatross.CommandLine.CodeGen {
	[Generator]
	public class CommandLineCodeGenerator : IIncrementalGenerator {

		public void Initialize(IncrementalGeneratorInitializationContext context) {
			var symbolProvider = context.CompilationProvider.Select(static (x, _) => x);
			var optionClasses = context.SyntaxProvider.ForAttributeWithMetadataName(
				fullyQualifiedMetadataName: MySymbolProvider.VerbAttributeClassName,
				predicate: static (_, _) => true,
				transform: static (ctx, _) => ctx)
				.Combine(symbolProvider).Select(static (tuple, _) => {
					var (ctx, symbolProvider) = tuple;
					var list = new List<CommandSetup>();
					foreach (var attribute in ctx.Attributes) {
						if (attribute.TryGetNamedArgument(My.OptionsClassProperty, out var typedConstant)) {
							if (typedConstant.Value is INamedTypeSymbol symbol) {
								list.Add(new CommandSetup(symbolProvider, symbol, attribute));
							}
						} else if (ctx.TargetSymbol is INamedTypeSymbol symbol) {
							list.Add(new CommandSetup(symbolProvider, symbol, attribute));
						}
					}
					return list;
				}).SelectMany(static (x, _) => x);

			var commandHandlers = context.SyntaxProvider.CreateSyntaxProvider(
				predicate: static (node, _) => node is ClassDeclarationSyntax,
				transform: static (ctx, _) => ctx)
				.Combine(symbolProvider).Select(static (x, _) => {
					var (ctx, symbolProvider) = x;
					var declaration = (ClassDeclarationSyntax)ctx.Node;
					var symbol = (INamedTypeSymbol)ctx.SemanticModel.GetDeclaredSymbol(declaration, _);
					if (symbol != null && symbol.HasInterface(symbolProvider.ICommandHandler_Interface()) && symbol.IsConcreteClass()) {
						return symbol;
					} else {
						return null;
					}
				}).Where(static x => x is not null);

			var setupClasses = context.SyntaxProvider.CreateSyntaxProvider(
				predicate: static (node, _) => node is ClassDeclarationSyntax,
				transform: static (ctx, _) => ctx).Combine(symbolProvider).Select(static (x, _) => {
					var (ctx, symbolProvider) = x;
					var declaration = (ClassDeclarationSyntax)ctx.Node;
					var symbol = (INamedTypeSymbol)ctx.SemanticModel.GetDeclaredSymbol(declaration, _);
					if (symbol != null && symbol.IsDerivedFrom(symbolProvider.SetupClass())) {
						return symbol;
					} else {
						return null;
					}
				}).Where(static x => x is not null);
			var aggregated = optionClasses.Collect()
					.Combine(commandHandlers.Collect())
					.Select(static (x, _) => (Options: x.Left, CommandHandlers: x.Right))
					.Combine(setupClasses.Collect())
					.Select(static (x, _) => (x.Left.Options, x.Left.CommandHandlers, Setups: x.Right));

			context.RegisterSourceOutput(
			  aggregated,
			  static (spc, data) => {
				  var (options, handlers, setups) = data;
				  new CommandCodeGen(spc, options, handlers, setups);
			  });
		}
	}
}