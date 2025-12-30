using Albatross.CodeGen;
using Albatross.CodeGen.CSharp;
using Albatross.CodeGen.CSharp.Declarations;
using Albatross.CodeGen.CSharp.Expressions;
using Albatross.CommandLine.CodeGen.IR;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Albatross.CommandLine.CodeGen {
	public class AddCommandsMethodBuilder : IConvertObject<ImmutableArray<CommandSetup>, MethodDeclaration> {
		private readonly IConvertObject<ITypeSymbol, ITypeExpression> typeConverter;
		public AddCommandsMethodBuilder(IConvertObject<ITypeSymbol, ITypeExpression> typeConverter) {
			this.typeConverter = typeConverter;
		}
		object IConvertObject<ImmutableArray<CommandSetup>>.Convert(ImmutableArray<CommandSetup> commandSetups)
			=> this.Convert(commandSetups);

		private IEnumerable<IExpression> CreateAddCommandsBody(ImmutableArray<CommandSetup> commandSetups) {
			foreach (var setup in commandSetups) {
				IExpression expression = new InvocationExpression {
					CallableExpression = new IdentifierNameExpression("host.CommandBuilder.Add") {
						GenericArguments = new ListOfGenericArguments {
							new TypeExpression(new QualifiedIdentifierNameExpression(setup.CommandClassName, new NamespaceExpression(setup.CommandClassNamespace)))
						}
					},
					Arguments = {
						new StringLiteralExpression(setup.Key)
					}
				};
				foreach (var param in setup.Parameters.OfType<CommandOptionParameterSetup>().Where(x => x.ExplicitParameterHandlerClass is not null)) {
					expression = expression.Chain(false,
						new InvocationExpression {
							CallableExpression = new IdentifierNameExpression("SetOptionAction"),
							Arguments = {
								new AnonymousMethodExpression {
									Parameters = {
										new ParameterDeclaration {
											Name = new IdentifierNameExpression("cmd"),
										}
									},
									Body = {
										new IdentifierNameExpression($"cmd.{param.CommandPropertyName}"),
									}
								},
								new IdentifierNameExpression("host"),
							}
						});
				}
				yield return expression;
			}
		}

		public MethodDeclaration Convert(ImmutableArray<CommandSetup> commandSetups) {
			return new MethodDeclaration {
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
					CreateAddCommandsBody(commandSetups),
					new ReturnExpression {
						Expression = new IdentifierNameExpression("host")
					}
				}
			};
		}
	}
}