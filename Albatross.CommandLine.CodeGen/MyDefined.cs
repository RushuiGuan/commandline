using Albatross.CodeGen;
using Albatross.CodeGen.CSharp.Expressions;

namespace Albatross.CommandLine.CodeGen {
	public static class MyDefined {
		public static class Namespaces {
			public static readonly NamespaceExpression AlbatrossCommandLine = new("Albatross.CommandLine");
			public static readonly NamespaceExpression SystemCommandLine = new("System.CommandLine");
			public static readonly NamespaceExpression MicrosoftExtensionsDepedencyInjectionExtensions = new("Microsoft.Extensions.DependencyInjection.Extensions");
		}
		public static class Identifiers {
			public static readonly QualifiedIdentifierNameExpression Command = new("Command", Namespaces.SystemCommandLine);
			public static readonly QualifiedIdentifierNameExpression Option = new("Option", Namespaces.SystemCommandLine);
			
			public static readonly QualifiedIdentifierNameExpression IAsyncCommandHandler = new("IAsyncCommandHandler", Namespaces.AlbatrossCommandLine);
			public static readonly QualifiedIdentifierNameExpression CommandHost = new("CommandHost", Namespaces.AlbatrossCommandLine);
			public static readonly QualifiedIdentifierNameExpression ICommandContext = new("ICommandContext", Namespaces.AlbatrossCommandLine);
		}
		public static class Types {
			public static readonly TypeExpression IAsyncCommandHandler = new(Identifiers.IAsyncCommandHandler);
			public static readonly TypeExpression CommandHost = new(Identifiers.CommandHost);
			public static readonly TypeExpression Command = new TypeExpression(Identifiers.Command);
		}
	}
}