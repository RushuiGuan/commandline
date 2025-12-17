using Albatross.CodeAnalysis.Symbols;
using Albatross.CodeGen;
using Albatross.CodeGen.CSharp;
using Albatross.CodeGen.CSharp.Declarations;
using Albatross.CodeGen.CSharp.Expressions;
using Albatross.CodeGen.CSharp.TypeConversions;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
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

			var aggregated = compilationProvider
				.Combine(commandSetups.Collect())
				.Select(static (x, _) => (Compilation: x.Left, optionClasses: x.Right));

			context.RegisterSourceOutput(
				aggregated,
				static (context, data) => {
					var typeConverter = new DefaultTypeConverter(data.Compilation);
					var (compilation, commandSetups) = data;
					var builder = new CommandClassBuilder(compilation, typeConverter);
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
						context.AddSource(file.FileName, new StringWriter().Code(file).ToString());
					}
					var entryPoint = compilation.GetEntryPoint(context.CancellationToken);
					var entryPointNamespace = entryPoint?.ContainingNamespace.GetFullNamespace() ?? "EntryMethodNamespaceNotFound";
					var registrationFile = new FileDeclaration($"CodeGenExtensions.g") {
						Namespace = new NamespaceExpression(entryPointNamespace),
						Classes = [
							new ClassDeclaration {
								Name = new IdentifierNameExpression("CodeGenExtensions"),
								AccessModifier = Defined.Keywords.Public,
								IsStatic = true,
								Methods = [
									new MethodDeclaration {
										IsStatic = true,
										AccessModifier = Defined.Keywords.Public,
										Name = new IdentifierNameExpression("RegisterCommands"),
										ReturnType = Defined.Types.IServiceCollection,
										Parameters = [
											new ParameterDeclaration {
												Name = new IdentifierNameExpression("services"),
												Type = Defined.Types.IServiceCollection,
												UseThisKeyword = true,
											}
										],
										Body = new CSharpCodeBlock {
											{
												true,
												() => CreateCommandHandlerRegistrations(commandSetups, typeConverter)
											},
											new ReturnExpression {
												Expression = new IdentifierNameExpression("services")
											}
										},
									},
									new MethodDeclaration {
										IsStatic = true,
										AccessModifier = Defined.Keywords.Public,
										Name = new IdentifierNameExpression("AddCommands"),
										ReturnType = MyDefined.Types.Setup,
										Parameters = [
											new ParameterDeclaration {
												Name = new IdentifierNameExpression("setup"),
												Type = MyDefined.Types.Setup,
												UseThisKeyword = true,
											}
										],
										Body = new CSharpCodeBlock {
											{ true, () => CreateAddCommandsBody(commandSetups) },
											new ReturnExpression {
												Expression = new IdentifierNameExpression("setup")
											}
										}
									}
								]
							}
						]
					};
					context.AddSource(registrationFile.FileName, new StringWriter().Code(registrationFile).ToString());
				});
		}

		private static IEnumerable<IExpression> CreateAddCommandsBody(ImmutableArray<CommandSetup> commandSetups) {
			foreach (var setup in commandSetups) {
				yield return new InvocationExpression {
					CallableExpression = new IdentifierNameExpression("setup.CommandBuilder.Add") {
						GenericArguments = new ListOfGenericArguments {
							new TypeExpression(setup.CommandClassName)
						}
					},
					Arguments = new ListOfArguments {
						new StringLiteralExpression(setup.Key)
					}
				};
			}
		}

		private static IEnumerable<IExpression> CreateCommandHandlerRegistrations(ImmutableArray<CommandSetup> commandSetups, IConvertObject<ITypeSymbol, ITypeExpression> typeConverter) {
			foreach (var setup in commandSetups) {
				yield return new InvocationExpression {
					CallableExpression = new IdentifierNameExpression("services.AddKeyedScoped") {
						GenericArguments = new ListOfGenericArguments {
							MyDefined.Types.ICommandHandler,
							typeConverter.Convert(setup.HandlerClass)
						}
					},
					Arguments = new ListOfArguments {
						new StringLiteralExpression(setup.Key)
					}
				};
			}
		}
	}
}