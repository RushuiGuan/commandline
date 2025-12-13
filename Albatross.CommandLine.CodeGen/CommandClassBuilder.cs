using Albatross.CodeGen;
using Albatross.CodeGen.CSharp;
using Albatross.CodeGen.CSharp.Declarations;
using Albatross.CodeGen.CSharp.Expressions;
using Albatross.CodeGen.Syntax;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;

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
							Arguments = new ListOfArguments(
								new StringLiteralExpression(setup.Name),
								string.IsNullOrEmpty(setup.Description) ? Defined.Literals.Null : new StringLiteralExpression(setup.Description!)),
						},
						AccessModifier = Defined.Keywords.Public,
						Body = new CodeBlock(CreateConstructorBody(setup)),
					}
				],
				Properties = CreateOptionProperties(setup),
			};
		}

		private IEnumerable<PropertyDeclaration> CreateOptionProperties(CommandSetup setup) {
			foreach (var option in setup.Options) {
				yield return new PropertyDeclaration {
					Name = new IdentifierNameExpression(option.CommandPropertyName),
					Type = this.typeConverter.Convert(option.PropertySymbol.Type),
					AccessModifier = Defined.Keywords.Public,
					GetterBody = new NoOpExpression(),
					SetterBody = null,
				};
			}
		}

		private IEnumerable<IExpression> CreateConstructorBody(CommandSetup setup) {
			foreach (var property in setup.Options) {
				yield return new AssignmentExpression {
					Left = new IdentifierNameExpression("this." + property.CommandPropertyName),
					Expression = new NewObjectExpression {
						Type = new TypeExpression(MyDefined.Identifiers.Option, this.typeConverter.Convert(property.PropertySymbol.Type)),
						Arguments = new ListOfArguments(new StringLiteralExpression(property.Name))
					},
				};
			}
		}
	}
}