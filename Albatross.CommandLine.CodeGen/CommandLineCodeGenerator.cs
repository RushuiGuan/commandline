using Albatross.CodeAnalysis;
using Albatross.CodeGen;
using Albatross.CodeGen.CSharp;
using Albatross.CodeGen.CSharp.Declarations;
using Albatross.CodeGen.CSharp.Expressions;
using Albatross.CodeGen.CSharp.TypeConversions;
using Albatross.Collections;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace Albatross.CommandLine.CodeGen {
	/// <summary>
	/// TODO: code gen can be created for manual command too.  user doesn't need to wire it up manually
	/// TODO: Need a warning if the Option Class does derive from the value of VerbAttribute.UseBaseOptionsClass
	/// 
	/// </summary>
	[Generator]
	public class CommandLineCodeGenerator : IIncrementalGenerator {
		static IncrementalValuesProvider<CommandSetup> BuildVerbsCommandSetups(IncrementalGeneratorInitializationContext context,
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
							if (ctx.TargetSymbol is INamedTypeSymbol optionsClass) {
								if (!attribueClass.IsGenericType) {
									// this one matches [Verb("commandKey")] target Options Class Only
									list.Add(new CommandSetup(compilation, optionsClass, attribute));
								} else if (attribueClass.TypeArguments.Length == 1) {
									// this one matches [Verb<THandler>("commandKey")] target Options Class Only with Handler Type Argument
									list.Add(new CommandSetup(compilation, optionsClass, attribueClass.TypeArguments[0], attribute));
								}
							} else if (attribueClass.TypeArguments.Length == 2 && attribueClass.TypeArguments[0] is INamedTypeSymbol namedTypeSymbol) {
								// this one matches [Verb<THandler, TOptions>("commandKey")] target assembly Only with Handler Type and Options Type Argument
								list.Add(new CommandSetup(compilation, namedTypeSymbol, attribueClass.TypeArguments[1], attribute));
							}
						}
					}
					return list;
				}).SelectMany(static (x, _) => x);
		}

		public void Initialize(IncrementalGeneratorInitializationContext context) {
			// System.Diagnostics.Debugger.Launch();
			var compilationProvider = context.CompilationProvider.Select(static (x, _) => x);
			var commandSetups = BuildVerbsCommandSetups(context, compilationProvider, MySymbolProvider.VerbAttributeClassName);
			var commandSetups1 = BuildVerbsCommandSetups(context, compilationProvider, MySymbolProvider.VerbAttributeClassNameGeneric1);
			var commandSetups2 = BuildVerbsCommandSetups(context, compilationProvider, MySymbolProvider.VerbAttributeClassNameGeneric2);

			var aggregated = compilationProvider.Combine(commandSetups.Collect()).Select((tuple, _) => {
				var (compilation, setups) = tuple;
				return (compilation, setups);
			}).Combine(commandSetups1.Collect()).Select((tuple, _) => {
				var ((compilation, setups), setups1) = tuple;
				return (compilation, setups.AddRange(setups1));
			}).Combine(commandSetups2.Collect()).Select((tuple, _) => {
				var ((compilation, setups), setups2) = tuple;
				return (compilation, setups.AddRange(setups2));
			});

			context.RegisterSourceOutput(
				aggregated,
				static (context, data) => {
					var typeConverter = new DefaultTypeConverter();
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
										Body = {
											{
												true,
												() => CreateCommandHandlerRegistrations(compilation, commandSetups, typeConverter)
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
										ReturnType = MyDefined.Types.CommandHost,
										Parameters = [
											new ParameterDeclaration {
												Name = new IdentifierNameExpression("host"),
												Type = MyDefined.Types.CommandHost,
												UseThisKeyword = true,
											}
										],
										Body = {
											{ true, () => CreateAddCommandsBody(commandSetups) },
											new ReturnExpression {
												Expression = new IdentifierNameExpression("host")
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
					CallableExpression = new IdentifierNameExpression("host.CommandBuilder.Add") {
						GenericArguments = new ListOfGenericArguments {
							new TypeExpression(new QualifiedIdentifierNameExpression(setup.CommandClassName, new NamespaceExpression(setup.CommandClassNamespace)))
						}
					},
					Arguments = {
						new StringLiteralExpression(setup.Key)
					}
				};
			}
		}

		private static IEnumerable<IExpression> CreateCommandHandlerRegistrations(Compilation compilation, ImmutableArray<CommandSetup> commandSetups, IConvertObject<ITypeSymbol, ITypeExpression> typeConverter) {
			Dictionary<INamedTypeSymbol, List<CommandSetup>> sharedBasedOptions = new(SymbolEqualityComparer.Default);
			foreach (var setup in commandSetups) {
				if (setup.HandlerClass != null) {
					yield return new InvocationExpression {
						CallableExpression = new IdentifierNameExpression("services.AddKeyedScoped") {
							GenericArguments = new ListOfGenericArguments {
								MyDefined.Types.IAsyncCommandHandler,
								typeConverter.Convert(setup.HandlerClass)
							}
						},
						Arguments = {
							new StringLiteralExpression(setup.Key)
						}
					};
				}
				if (setup.BaseOptionsClass == null) {
					yield return CreateCommandOptionsRegistration(compilation, setup, typeConverter);
				} else {
					sharedBasedOptions.GetOrAdd(setup.BaseOptionsClass, () => new List<CommandSetup>()).Add(setup);
				}
			}
			foreach (var keyValuePair in sharedBasedOptions) {
				yield return CreateCommandSharedOptionsRegistration(compilation, keyValuePair.Key, keyValuePair.Value, typeConverter);
			}
		}

		static bool ShouldUseRequiredValue(Compilation compilation, CommandParameterSetup parameter) {
			// for collection types, always use null-coalescing operator: GetValue(..) ?? new List<T>()
			if (parameter.PropertySymbol.Type.IsCollection(compilation)) {
				return false;
			}
			if (parameter is CommandOptionParameterSetup optionPropertySetup) {
				if (optionPropertySetup.Required || optionPropertySetup.ShouldDefaultToInitializer) {
					return true;
				}
			} else if (parameter is CommandArgumentParameterSetup argumentPropertySetup) {
				if (argumentPropertySetup is not { ArityMin: 0, ArityMax: 1 }) {
					return true;
				}
			}
			return false;
		}

		private static IExpression CreateCommandSharedOptionsRegistration(Compilation compilation, INamedTypeSymbol sharedOptionBaseClass, List<CommandSetup> setups, IConvertObject<ITypeSymbol, ITypeExpression> typeConverter) {
			return new InvocationExpression {
				CallableExpression = new IdentifierNameExpression("services.AddScoped") {
					GenericArguments = {
						typeConverter.Convert(sharedOptionBaseClass)
					}
				},
				Arguments = {
					new AnonymousMethodExpression {
						Parameters = {
							new ParameterDeclaration {
								Name = new IdentifierNameExpression("provider"),
								Type = Defined.Types.Var,
							}
						},
						Body = {
							new AssignmentExpression {
								Left = new VariableDeclaration {
									Identifier = new IdentifierNameExpression("result"),
								},
								Expression = new InvocationExpression {
									CallableExpression = new IdentifierNameExpression("provider.GetRequiredService") {
										GenericArguments = {
											new TypeExpression(MyDefined.Identifiers.ParserResult)
										}
									}
								}
							},
							new AssignmentExpression {
								Left = new VariableDeclaration {
									Identifier = new IdentifierNameExpression("key"),
								},
								Expression = new InvocationExpression {
									CallableExpression = new IdentifierNameExpression("result.CommandResult.Command.GetCommandKey"),
								}
							},
							new ReturnExpression {
								Expression = new SwitchExpression {
									Value = new IdentifierNameExpression("key"),
									Sections = new ListOfNodes<SwitchCaseExpression> {
										{
											true, () => setups.Select(x => new SwitchCaseExpression {
												Value = new StringLiteralExpression(x.Key),
												Expression = new NewObjectExpression {
													Type = typeConverter.Convert(x.OptionsClass),
													Initializers = {
														x.Parameters.Select(y => CreateGetOptionPropertyValueExpression(compilation, y, typeConverter))
													}
												},
											})
										}
									},
									DefaultExpression = new ThrowExpression {
										Expression = new NewObjectExpression {
											Type = new TypeExpression("System.InvalidOperationException"),
											Arguments = {
												new StringInterpolationExpression {
													Items = {
														new StringLiteralExpression("Command "),
														new IdentifierNameExpression("key"),
														new StringLiteralExpression($" is not registered for base Options class \"{sharedOptionBaseClass.Name}\"")
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			};
		}
		public static AssignmentExpression CreateGetOptionPropertyValueExpression(Compilation compilation, CommandParameterSetup parameter, IConvertObject<ITypeSymbol, ITypeExpression> typeConverter) {
			IExpression expression = new InvocationExpression {
				CallableExpression = new IdentifierNameExpression(ShouldUseRequiredValue(compilation, parameter) ? "result.GetRequiredValue" : "result.GetValue") {
					GenericArguments = { typeConverter.Convert(parameter.PropertySymbol.Type) }
				},
				Arguments = { new StringLiteralExpression(parameter.Key) }
			};
			if (parameter.PropertySymbol.Type.TryGetCollectionElementType(compilation, out var elementType)) {
				expression = new InfixExpression {
					Left = expression,
					Operator = new Operator("??"),
					Right = new InvocationExpression {
						CallableExpression = new QualifiedIdentifierNameExpression("Array.Empty", Defined.Namespaces.System) {
							GenericArguments = { typeConverter.Convert(elementType) }
						}
					}
				};
			}
			return new AssignmentExpression {
				Left = new IdentifierNameExpression(parameter.PropertySymbol.Name),
				Expression = expression
			};
		}

		private static IExpression CreateCommandOptionsRegistration(Compilation compilation, CommandSetup setup, IConvertObject<ITypeSymbol, ITypeExpression> typeConverter) {
			return new InvocationExpression {
				CallableExpression = new IdentifierNameExpression("services.AddScoped") {
					GenericArguments = {
						typeConverter.Convert(setup.OptionsClass)
					}
				},
				Arguments = {
					new AnonymousMethodExpression {
						Parameters = {
							new ParameterDeclaration {
								Name = new IdentifierNameExpression("provider"),
								Type = Defined.Types.Var,
							}
						},
						Body = {
							new AssignmentExpression {
								Left = new VariableDeclaration {
									Identifier = new IdentifierNameExpression("result"),
								},
								Expression = new InvocationExpression {
									CallableExpression = new IdentifierNameExpression("provider.GetRequiredService") {
										GenericArguments = {
											new TypeExpression(MyDefined.Identifiers.ParserResult)
										}
									}
								}
							},
							new AssignmentExpression {
								Left = new VariableDeclaration {
									Identifier = new IdentifierNameExpression("options"),
								},
								Expression = new NewObjectExpression {
									Type = typeConverter.Convert(setup.OptionsClass),
									Initializers = {
										setup.Parameters.Select(x => CreateGetOptionPropertyValueExpression(compilation, x, typeConverter))
									}
								}
							},
							new ReturnExpression {
								Expression = new IdentifierNameExpression("options")
							},
						}
					}
				}
			};
		}
	}
}