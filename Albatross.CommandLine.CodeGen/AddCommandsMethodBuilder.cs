using Albatross.CodeGen;
using Albatross.CodeGen.CSharp;
using Albatross.CodeGen.CSharp.Declarations;
using Albatross.CodeGen.CSharp.Expressions;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Albatross.CommandLine.CodeGen {
	public class AddCommandsMethodBuilder : IConvertObject<ImmutableArray<CommandSetup>, MethodDeclaration> {
		public AddCommandsMethodBuilder() {
		}

		object IConvertObject<ImmutableArray<CommandSetup>>.Convert(ImmutableArray<CommandSetup> commandSetups)
			=> this.Convert(commandSetups);

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
					{ true, () => CreateAddCommandsBody(commandSetups) },
					new ReturnExpression {
						Expression = new IdentifierNameExpression("host")
					}
				}
			};
		}
	}
}