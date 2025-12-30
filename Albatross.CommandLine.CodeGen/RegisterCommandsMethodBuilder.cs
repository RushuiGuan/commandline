using Albatross.CodeAnalysis;
using Albatross.CodeGen;
using Albatross.CodeGen.CSharp;
using Albatross.CodeGen.CSharp.Declarations;
using Albatross.CodeGen.CSharp.Expressions;
using Albatross.Collections;
using Albatross.CommandLine.CodeGen.IR;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Albatross.CommandLine.CodeGen {
	/// <summary>
	/// this class builds the RegisterCommands method that registers all commands and their handlers and parameter classes into the IServiceCollection
	/// </summary>
	/// <code><![CDATA[
	///	public static IServiceCollection RegisterCommands(this IServiceCollection services) {
	///		services.AddKeyedScoped<IAsyncCommandHandler, Albatross.CommandLine.DefaultAsyncCommandHandler<Sample.CommandLine.TestOptionRequiredFlagParams>>("test option-required-flag");
	///		services.AddScoped<Sample.CommandLine.TestOptionRequiredFlagParams>(provider => {
	///			var result = provider.GetRequiredService<ParseResult>();
	///			var parameters = new Sample.CommandLine.TestOptionRequiredFlagParams() {
	///				IntValues = result.GetValue<int[]>("--int-values") ?? Array.Empty<int>(),
	///				TextValues = result.GetValue<string[]>("--text-values") ?? Array.Empty<string>(),
	///				OptionalBoolValue = result.GetValue<bool>("--optional-bool-value"),
	///				OptionalTextValue = result.GetValue<string?>("--optional-text-value"),
	///			};
	///			return parameters;
	///		});
	///		services.AddScoped<Sample.CommandLine.MutuallyExclusiveParams.ProjectParams>(provider => {
	///			var result = provider.GetRequiredService<ParseResult>();
	///			var key = result.CommandResult.Command.GetCommandKey();
	///			return key switch  {
	///				"example project echo" => new Sample.CommandLine.MutuallyExclusiveParams.ProjectEchoOptions() {
	///					Echo = result.GetRequiredValue<int>("--echo"),
	///					Id = result.GetRequiredValue<int>("--id"),
	///				},
	///				"example project fubar" => new Sample.CommandLine.MutuallyExclusiveParams.ProjectFubarOptions() {
	///					Fubar = result.GetRequiredValue<int>("--fubar"),
	///					Id = result.GetRequiredValue<int>("--id"),
	///				},
	///				_ => throw new System.InvalidOperationException($"Command {key} is not registered for base Params class \"ProjectParams\"")
	///			};
	///		});
	///		services.AddKeyedScoped<IAsyncCommandHandler, Sample.CommandLine.SelfContainedParams.GetInstrumentDetails>("example instrument detail");
	///		services.AddScoped<Sample.CommandLine.SelfContainedParams.GetInstrumentDetailsParams>(provider => {
	///			var result = provider.GetRequiredService<ParseResult>();
	///			var context = provider.GetRequiredService<ICommandContext>();
	///			var parameters = new Sample.CommandLine.SelfContainedParams.GetInstrumentDetailsParams() {
	///				Summary = context.GetRequiredValue<Sample.CommandLine.SelfContainedParams.InstrumentSummary>("--summary"),
	///			};
	///			return parameters;
	///		});
	///		return services;
	/// }
	/// ]]></code>
	public class RegisterCommandsMethodBuilder : IConvertObject<ImmutableArray<CommandSetup>, MethodDeclaration> {
		private readonly Compilation compilation;
		private readonly IConvertObject<ITypeSymbol, ITypeExpression> typeConverter;

		public RegisterCommandsMethodBuilder(Compilation compilation, IConvertObject<ITypeSymbol, ITypeExpression> typeConverter) {
			this.compilation = compilation;
			this.typeConverter = typeConverter;
		}

		public MethodDeclaration Convert(ImmutableArray<CommandSetup> commandSetups) {
			return new MethodDeclaration {
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
					CreateCommandHandlerRegistrations(compilation, commandSetups, typeConverter),
					new ReturnExpression {
						Expression = new IdentifierNameExpression("services")
					}
				},
			};
		}

		private static IEnumerable<IExpression> CreateCommandHandlerRegistrations(Compilation compilation, ImmutableArray<CommandSetup> commandSetups, IConvertObject<ITypeSymbol, ITypeExpression> typeConverter) {
			Dictionary<INamedTypeSymbol, List<CommandSetup>> sharedBasedParams = new(SymbolEqualityComparer.Default);
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
				if (setup.BaseParamsClass == null) {
					yield return CreateParamsRegistration(compilation, setup, typeConverter);
				} else {
					sharedBasedParams.GetOrAdd(setup.BaseParamsClass, () => new List<CommandSetup>()).Add(setup);
				}
			}
			foreach (var keyValuePair in sharedBasedParams) {
				yield return CreateParamsRegistrationByBaseClass(compilation, keyValuePair.Key, keyValuePair.Value, typeConverter);
			}
		}

		object IConvertObject<ImmutableArray<CommandSetup>>.Convert(ImmutableArray<CommandSetup> from) {
			return Convert(from);
		}

		private static IExpression CreateParamsRegistration(Compilation compilation, CommandSetup setup, IConvertObject<ITypeSymbol, ITypeExpression> typeConverter) {
			return new InvocationExpression {
				CallableExpression = new IdentifierNameExpression("services.AddScoped") {
					GenericArguments = {
						typeConverter.Convert(setup.ParamsClass)
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
									Identifier = new IdentifierNameExpression("context"),
								},
								Expression = new InvocationExpression {
									CallableExpression = new IdentifierNameExpression("provider.GetRequiredService") {
										GenericArguments = {
											new TypeExpression(MyDefined.Identifiers.ICommandContext)
										}
									}
								}
							},
							new AssignmentExpression {
								Left = new VariableDeclaration {
									Identifier = new IdentifierNameExpression("parameters"),
								},
								Expression = new NewObjectExpression {
									Type = typeConverter.Convert(setup.ParamsClass),
									Initializers = {
										setup.Parameters.Select(x => CreateGetOptionPropertyValueExpression(compilation, x, typeConverter))
									}
								}
							},
							new ReturnExpression {
								Expression = new IdentifierNameExpression("parameters")
							},
						}
					}
				}
			};
		}

		private static IExpression CreateParamsRegistrationByBaseClass(Compilation compilation, INamedTypeSymbol sharedOptionBaseClass, List<CommandSetup> setups, IConvertObject<ITypeSymbol, ITypeExpression> typeConverter) {
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
									Identifier = new IdentifierNameExpression("context"),
								},
								Expression = new InvocationExpression {
									CallableExpression = new IdentifierNameExpression("provider.GetRequiredService") {
										GenericArguments = {
											new TypeExpression(MyDefined.Identifiers.ICommandContext)
										}
									}
								}
							},
							new ReturnExpression {
								Expression = new SwitchExpression {
									Value = new IdentifierNameExpression("context.Key"),
									Sections = new ListOfNodes<SwitchCaseExpression> {
										{
											true, () => setups.Select(x => new SwitchCaseExpression {
												Value = new StringLiteralExpression(x.Key),
												Expression = new NewObjectExpression {
													Type = typeConverter.Convert(x.ParamsClass),
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
														new IdentifierNameExpression("context.Key"),
														new StringLiteralExpression($" is not registered for base Params class \"{sharedOptionBaseClass.Name}\"")
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
		
		static string GetCommandParameterRetrievalMethodName(Compilation compilation, CommandParameterSetup parameter) {
			var sb = new StringBuilder("context");
			if (parameter.ExplicitParameterClass == null) {
				sb.Append(".Result");
			}
			if(ShouldUseRequiredValue(compilation, parameter)) {
				sb.Append(".GetRequiredValue");
			} else {
				sb.Append(".GetValue");
			}
			return sb.ToString();
		}

		public static AssignmentExpression CreateGetOptionPropertyValueExpression(Compilation compilation, CommandParameterSetup parameter, IConvertObject<ITypeSymbol, ITypeExpression> typeConverter) {
			IExpression expression = new InvocationExpression {
				CallableExpression = new IdentifierNameExpression(GetCommandParameterRetrievalMethodName(compilation, parameter)) {
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
	}
}