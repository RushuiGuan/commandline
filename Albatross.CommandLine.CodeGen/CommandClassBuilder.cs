using Albatross.CodeGen;
using Albatross.CodeGen.CSharp;
using Albatross.CodeGen.CSharp.Declarations;
using Albatross.CodeGen.CSharp.Expressions;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using InvocationExpression = Albatross.CodeGen.CSharp.Expressions.InvocationExpression;

namespace Albatross.CommandLine.CodeGen {
	public class CommandClassBuilder : IConvertObject<CommandSetup, ClassDeclaration> {
		private readonly Compilation compilation;
		private readonly IConvertObject<ITypeSymbol, ITypeExpression> typeConverter;

		public CommandClassBuilder(Compilation compilation, IConvertObject<ITypeSymbol, ITypeExpression> typeConverter) {
			this.compilation = compilation;
			this.typeConverter = typeConverter;
		}

		object IConvertObject<CommandSetup>.Convert(CommandSetup setup) {
			return Convert(setup);
		}

		public ClassDeclaration Convert(CommandSetup setup) {
			return new ClassDeclaration {
				Name = new IdentifierNameExpression(setup.CommandClassName),
				AccessModifier = Defined.Keywords.Public,
				BaseTypes = [
					new TypeExpression(MyDefined.Identifiers.Command),
				],
				IsPartial = true,
				IsSealed = true,
				Constructors = [
					new ConstructorDeclaration {
						Name = new IdentifierNameExpression(setup.CommandClassName),
						BaseConstructorInvocation = new InvocationExpression {
							CallableExpression = new IdentifierNameExpression("base"),
							Arguments = {
								new StringLiteralExpression(setup.Name),
								{ !string.IsNullOrEmpty(setup.Description), () => new StringLiteralExpression(setup.Description!) },
							}
						},
						AccessModifier = Defined.Keywords.Public,
						Body = { CreateConstructorBody(setup) },
					}
				],
				Methods = [
					new MethodDeclaration {
						Name = new IdentifierNameExpression("Initialize"),
						ReturnType = Defined.Types.Void,
						IsPartial = true,
					}
				],
				Properties = CreateProperties(setup),
			};
		}

		private IEnumerable<PropertyDeclaration> CreateProperties(CommandSetup setup) {
			foreach (var parameter in setup.Parameters) {
				yield return new PropertyDeclaration {
					Name = new IdentifierNameExpression(parameter.CommandPropertyName),
					Type = new TypeExpression(parameter is CommandArgumentPropertySetup ? MyDefined.Identifiers.Argument : MyDefined.Identifiers.Option, this.typeConverter.Convert(parameter.PropertySymbol.Type)),
					AccessModifier = Defined.Keywords.Public,
					GetterBody = new NoOpExpression(),
					SetterBody = null,
				};
			}
		}

		private IEnumerable<IExpression> CreateConstructorBody(CommandSetup setup) {
			foreach (var alias in setup.Aliases) {
				yield return new InvocationExpression {
					CallableExpression = new IdentifierNameExpression("this.Aliases.Add"),
					Arguments = { new StringLiteralExpression(alias) }
				};
			}
			foreach (var parameter in setup.Parameters) {
				yield return new AssignmentExpression {
					Left = new IdentifierNameExpression("this." + parameter.CommandPropertyName),
					Expression = new NewObjectExpression {
						Type = new TypeExpression(parameter is CommandArgumentPropertySetup ? MyDefined.Identifiers.Argument : MyDefined.Identifiers.Option, this.typeConverter.Convert(parameter.PropertySymbol.Type)),
						Arguments = new ListOfArguments {
							new StringLiteralExpression(parameter.Key),
							{ parameter is CommandOptionPropertySetup, ()=> ((CommandOptionPropertySetup)parameter).Aliases.Select(x=>new StringLiteralExpression(x)) }
						},
						Initializers = new() {
							{
								!string.IsNullOrEmpty(parameter.Description), () => new AssignmentExpression {
									Left = new IdentifierNameExpression("Description"),
									Expression = new StringLiteralExpression(parameter.Description!),
								}
							}, {
								parameter.Hidden, () => new AssignmentExpression {
									Left = new IdentifierNameExpression("Hidden"),
									Expression = Defined.Literals.True,
								}
							},
							{
								parameter is CommandOptionPropertySetup { Required: true }, () => new AssignmentExpression {
									Left = new IdentifierNameExpression("Required"),
									Expression = Defined.Literals.True,
								}
							}, {
								parameter is CommandArgumentPropertySetup argumentProperty, () => new AssignmentExpression {
									Left = new IdentifierNameExpression("Arity"),
									Expression = new NewObjectExpression {
										Type = new TypeExpression("ArgumentArity"),
										Arguments = new ListOfArguments {
											new IntLiteralExpression(((CommandArgumentPropertySetup)parameter).ArityMin),
											new IntLiteralExpression(((CommandArgumentPropertySetup)parameter).ArityMax),
										}
									}
								}
							},
							{
								parameter.ShouldDefaultToInitializer, () => new AssignmentExpression {
									Left = new IdentifierNameExpression("DefaultValueFactory"),
									Expression = new AnonymousMethodExpression{
										Parameters = {
											new ParameterDeclaration {
												Name = new IdentifierNameExpression("_"),
												Type = Defined.Types.Var,
											},
										},
										Body = {
											new SyntaxNodeExpression(parameter.PropertyInitializer!, compilation.GetSemanticModel(parameter.PropertyInitializer!.SyntaxTree))
										}
									}
								}
							}
						},
					}
				};
				yield return new InvocationExpression {
					CallableExpression = new IdentifierNameExpression("this.Add"),
					Arguments = {
						new IdentifierNameExpression("this." + parameter.CommandPropertyName)
					}
				};
			}
			yield return new InvocationExpression {
				CallableExpression = new IdentifierNameExpression("this.Initialize"),
			};
		}
	}
}