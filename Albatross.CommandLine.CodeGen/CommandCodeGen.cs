using Shared = Albatross.CodeAnalysis.My;
using Albatross.CodeAnalysis;
using Albatross.CodeAnalysis.Symbols;
using Albatross.CodeAnalysis.Syntax;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Albatross.CommandLine.CodeGen {
	public class CommandCodeGen {
		public StringWriter Writer { get; } = new StringWriter();
		public CommandCodeGen(SourceProductionContext context, IEnumerable<CommandSetup> commands, IEnumerable<INamedTypeSymbol> commandHandlers, IEnumerable<INamedTypeSymbol> setupClasses) {
			string? setupClassNamespace = setupClasses.FirstOrDefault()?.ContainingNamespace?.GetFullNamespace();
			var setups = new SortedDictionary<string, CommandSetup>();
			foreach (var command in commands) {
				setups.Add(command.Key, command);
			}
			try {
				foreach (var group in setups.Values.GroupBy(x => x.CommandClassName)) {
					if (group.Count() > 1) {
						int index = 0;
						foreach (var setup in group) {
							setup.RenameCommandClass(index++);
						}
					}
				}
				// generate the command class
				foreach (var setup in setups.Values) {
					var cs = new CodeStack();
					using (cs.NewScope(new CompilationUnitBuilder())) {
						cs.With(new UsingDirectiveNode("System.CommandLine"))
							.With(new UsingDirectiveNode(Shared.Namespace.System))
							.With(new UsingDirectiveNode(Shared.Namespace.System_IO))
							.With(new UsingDirectiveNode(Shared.Namespace.System_Threading_Tasks));
						IEnumerable<string> additionalNamespaces = [];
						using (cs.NewScope(new NamespaceDeclarationBuilder(setup.OptionClass.ContainingNamespace.ToDisplayString()))) {
							using (cs.NewScope(new ClassDeclarationBuilder(setup.CommandClassName).Public().Sealed().Partial())) {
								cs.With(new BaseTypeNode(My.CommandClassName));
								using (cs.NewScope(new ConstructorDeclarationBuilder(setup.CommandClassName).Public())) {
									using (cs.NewScope(new ArgumentListBuilder())) {
										cs.With(new LiteralNode(setup.Name)).With(new LiteralNode(setup.Description));
									}
									additionalNamespaces = this.BuildConstructorStatements(cs, setup);
								}
								cs.NewScope(new MethodDeclarationBuilder("void", "Initialize").Partial().SignatureOnly()).Dispose();
								var variableArityArgumentCount = 0;
								var index = 0;
								// build the argument properties
								foreach (var argument in setup.Arguments.OrderBy(x => x.Order).ThenBy(x => x.Index)) {
									var argumentType = new GenericIdentifierNode(My.ArgumentClassName, argument.Type);
									cs.With(new PropertyNode(argumentType, argument.CommandPropertyName).GetterOnly());
									if (!argument.FixedArity) {
										variableArityArgumentCount++;
										if (index != setup.Arguments.Length - 1) {
											//context.CodeGenDiagnostic(DiagnosticSeverity.Warning, $"{My.Diagnostic.IdPrefix}4", $"A variable arity argument [{setup.Name} <{argument.Name}>] should be the last argument in the argument list");
										}
									}
									index++;
								}
								if (variableArityArgumentCount > 1) {
									//context.CodeGenDiagnostic(DiagnosticSeverity.Warning, $"{My.Diagnostic.IdPrefix}5", $"Command [{setup.Name}] should only have a single argument with variable arity");
								}
								// build the option properties
								foreach (var option in setup.Options.OrderBy(x => x.Order).ThenBy(x => x.Index)) {
									var optionType = new GenericIdentifierNode(My.OptionClassName, option.Type);
									cs.With(new PropertyNode(optionType, option.CommandPropertyName).GetterOnly());
								}
							}
						}
						foreach (var item in additionalNamespaces) {
							cs.With(new UsingDirectiveNode(item));
						}
					}
					var text = cs.Build();
					context.AddSource(setup.CommandClassName, SourceText.From(text, Encoding.UTF8));
					Writer.WriteSourceHeader(setup.CommandClassName);
					Writer.WriteLine(text);
				}
				// generate the code to register the commands
				var diCodeStack = new CodeStack();
				using (diCodeStack.NewScope(new CompilationUnitBuilder())) {
					diCodeStack.With(new UsingDirectiveNode(Shared.Namespace.Microsoft_Extensions_DependencyInjection));
					diCodeStack.With(new UsingDirectiveNode("System.CommandLine.Invocation"));
					diCodeStack.With(new UsingDirectiveNode("System.CommandLine"));
					diCodeStack.With(new UsingDirectiveNode("Albatross.CommandLine"));
					diCodeStack.With(new UsingDirectiveNode("System.Collections.Generic"));

					var namespaces = new List<string>();
					var addedOptionClasses = new HashSet<string>();
					using (diCodeStack.NewScope(new NamespaceDeclarationBuilder(setupClassNamespace ?? "RootNamespaceNotYetFound"))) {
						using (diCodeStack.NewScope(new ClassDeclarationBuilder(Shared.Class.CodeGenExtensions).Public().Static())) {
							using (diCodeStack.NewScope(new MethodDeclarationBuilder("IServiceCollection", "RegisterCommands").Public().Static())) {
								diCodeStack.With(new ParameterNode(new TypeNode("IServiceCollection"), "services").WithThis());
								foreach (var setup in setups.Values) {
									using (diCodeStack.NewScope()) {
										diCodeStack.With(new IdentifierNode("services"))
											.With(new GenericIdentifierNode("AddKeyedScoped", "ICommandHandler", setup.HandlerClass))
											.To(new MemberAccessBuilder())
											.Begin(new ArgumentListBuilder()).With(new LiteralNode(setup.Key)).End()
											.To(new InvocationExpressionBuilder());
									}
									if (!addedOptionClasses.Contains(setup.OptionClass.Name)) {
										addedOptionClasses.Add(setup.OptionClass.Name);
										namespaces.Add(setup.OptionClass.ContainingNamespace.ToDisplayString());
										var className = setup.CommandClassName;
										using (diCodeStack.NewScope()) {
											diCodeStack.With(new IdentifierNode("services"))
												.With(new GenericIdentifierNode("AddScoped", setup.OptionClass.Name))
												.ToNewBegin(new InvocationExpressionBuilder())
													.Begin(new LambdaExpressionBuilder())
														.With(new ParameterNode("provider"))
														.With(new LiteralNode(1))
													.End()
												.End();
										}
									}
								}
								diCodeStack.With(SyntaxFactory.ReturnStatement(new IdentifierNode("services").Identifier));
							}

							using (diCodeStack.NewScope(new MethodDeclarationBuilder("Setup", "AddCommands").Public().Static())) {
								diCodeStack.With(new ParameterNode(new TypeNode("Setup"), "setup").WithThis());
								foreach (var setup in setups.Values) {
									namespaces.Add(setup.OptionClass.ContainingNamespace.ToDisplayString());
									using (diCodeStack.NewScope()) {
										diCodeStack.With(new IdentifierNode("setup"))
											.With(new IdentifierNode("CommandBuilder"))
											.With(new GenericIdentifierNode("Add", setup.CommandClassName))
											.ToNewBegin(new InvocationExpressionBuilder())
												.Begin(new ArgumentListBuilder())
													.With(new LiteralNode(setup.Key))
												.End()
											.End();
									}
								}
								diCodeStack.With(SyntaxFactory.ReturnStatement(new IdentifierNode("setup").Identifier));
							}
						}
					}
					foreach (var item in namespaces) {
						diCodeStack.With(new UsingDirectiveNode(item));
					}
				}
				var code = diCodeStack.Build();
				context.AddSource(Shared.Class.CodeGenExtensions, SourceText.From(code, Encoding.UTF8));
				Writer.WriteSourceHeader(Shared.Class.CodeGenExtensions);
				Writer.WriteLine(code);
			} catch (Exception err) {
				context.AddSource(Shared.Class.CodeGenExtensions, SourceText.From(err.ToString(), Encoding.UTF8));
				// context.CodeGenDiagnostic(DiagnosticSeverity.Error, $"{My.Diagnostic.IdPrefix}2", err.BuildCodeGeneneratorErrorMessage("commandline"));
			}
		}

		IEnumerable<string> BuildConstructorStatements(CodeStack cs, CommandSetup setup) {
			var namespaces = new HashSet<string>();
			foreach (var value in setup.Aliases) {
				using (cs.NewScope()) {
					cs.With(new ThisExpression()).With(new IdentifierNode("Aliases")).With(new IdentifierNode("Add"))
						.To(new MemberAccessBuilder())
						.ToNewBegin(new InvocationExpressionBuilder())
							.Begin(new ArgumentListBuilder())
								.With(new LiteralNode($"{value}"))
							.End()
						.End();
				}
			}
			if (setup.Arguments.Any()) {
				foreach (var argument in setup.Arguments.OrderBy(x => x.Order).ThenBy(x => x.Index)) {
					using (cs.NewScope()) {
						using (cs.With(new ThisExpression()).With(new IdentifierNode(argument.CommandPropertyName)).ToNewScope(new AssignmentExpressionBuilder())) {
							using (cs.NewScope(new NewObjectBuilder(new GenericIdentifierNode(My.ArgumentClassName, argument.Type)))) {
								using (cs.NewScope(new ArgumentListBuilder())) {
									cs.With(new LiteralNode(argument.Name));
								}
								if (!string.IsNullOrEmpty(argument.Description)) {
									cs.Begin(new AssignmentExpressionBuilder("Description"))
										.With(new LiteralNode(argument.Description))
									.End();
								}
								if (argument.Hidden) {
									cs.Begin(new AssignmentExpressionBuilder("IsHidden"))
										.With(new LiteralNode(true))
									.End();
								}
								cs.Begin(new AssignmentExpressionBuilder("Arity"))
									.Begin(new NewObjectBuilder("ArgumentArity"))
										.Begin(new ArgumentListBuilder())
											.With(new LiteralNode(argument.ArityMin))
											.With(new LiteralNode(argument.ArityMax))
										.End()
									.End()
								.End();
							}
						}
						SetCommandPropertyDefaultValue(argument, cs, namespaces);
						using (cs.NewScope()) {
							cs.With(new ThisExpression())
								.With(new IdentifierNode("Add"))
								.To(new MemberAccessBuilder())
								.ToNewBegin(new InvocationExpressionBuilder())
									.Begin(new ArgumentListBuilder())
										.With(new IdentifierNode(argument.CommandPropertyName))
									.End()
								.End();
						}
					}
				}
			}
			if (setup.Options.Any()) {
				foreach (var option in setup.Options.OrderBy(x => x.Order).ThenBy(x => x.Index)) {
					using (cs.NewScope()) {
						using (cs.With(new ThisExpression()).With(new IdentifierNode(option.CommandPropertyName)).ToNewScope(new AssignmentExpressionBuilder())) {
							using (cs.NewScope(new NewObjectBuilder(new GenericIdentifierNode(My.OptionClassName, option.Type)))) {
								using (cs.NewScope(new ArgumentListBuilder())) {
									cs.With(new LiteralNode(option.Name));
								}
								if (!string.IsNullOrEmpty(option.Description)) {
									cs.Begin(new AssignmentExpressionBuilder("Description"))
										.With(new LiteralNode(option.Description))
									.End();
								}
								if (option.Required) {
									using (cs.NewScope(new AssignmentExpressionBuilder("Required"))) {
										cs.With(new LiteralNode(true));
									}
								}
								if (option.Hidden) {
									cs.Begin(new AssignmentExpressionBuilder("Hidden"))
										.With(new LiteralNode(true))
									.End();
								}
							}
						}
					}

					foreach (var alias in option.Aliases) {
						string aliasName;
						if (alias.StartsWith("-")) {
							aliasName = alias;
						} else if (alias.Length == 1) {
							aliasName = $"-{alias}";
						} else {
							aliasName = $"--{alias}";
						}
						using (cs.NewScope()) {
							cs.With(new IdentifierNode(option.CommandPropertyName))
								.With(new IdentifierNode("Aliases"))
								.With(new IdentifierNode("Add"))
								.To(new MemberAccessBuilder())
								.ToNewBegin(new InvocationExpressionBuilder())
									.Begin(new ArgumentListBuilder())
										.With(new LiteralNode(aliasName))
									.End()
								.End();
						}
					}
					SetCommandPropertyDefaultValue(option, cs, namespaces);
					using (cs.NewScope()) {
						cs.With(new ThisExpression())
							.With(new IdentifierNode("Add"))
							.To(new MemberAccessBuilder())
							.ToNewBegin(new InvocationExpressionBuilder())
								.Begin(new ArgumentListBuilder())
									.With(new IdentifierNode(option.CommandPropertyName))
								.End()
							.End();
					}
				}
			}
			return namespaces;
		}

		void SetCommandPropertyDefaultValue(CommandPropertySetup propertySetup, CodeStack cs, ISet<string> namespaces) {
			if (propertySetup.ShouldDefaultToInitializer && propertySetup.PropertyInitializer != null) {
				using (cs.NewScope()) {
					namespaces.Add(propertySetup.Property.Type.ContainingNamespace.ToDisplayString());
					// Option_Number.DefaultValueFactory = _ => 100;
					cs.With(new IdentifierNode(propertySetup.CommandPropertyName))
						.With(new IdentifierNode("DefaultValueFactory"))
						.ToNewBegin(new AssignmentExpressionBuilder())
							.Begin(new LambdaExpressionBuilder())
								.With(new ParameterNode("_"))
								.With(new NodeContainer(propertySetup.PropertyInitializer))
							.End()
						.End();
				}
			}
		}
		public void Initialize(GeneratorInitializationContext context) { }
	}
}