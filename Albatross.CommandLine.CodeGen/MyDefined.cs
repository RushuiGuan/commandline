using Albatross.CodeGen;
using Albatross.CodeGen.CSharp.Expressions;

namespace Albatross.CommandLine.CodeGen {
	public static class MyDefined {
		public static class Namespaces {
			public static readonly NamespaceExpression AlbatrossCommandLine = new("Albatross.CommandLine");
			public static readonly NamespaceExpression SystemCommandLine = new("System.CommandLine");
			public static readonly NamespaceExpression MicrosoftExtensionsOptions = new("Microsoft.Extensions.Options");
		}
		public static class Identifiers {
			public static readonly QualifiedIdentifierNameExpression IOptions = new ("IOptions", Namespaces.MicrosoftExtensionsOptions);
			public static readonly QualifiedIdentifierNameExpression Command = new("Command", Namespaces.SystemCommandLine);
			public static readonly QualifiedIdentifierNameExpression Option = new("Option", Namespaces.SystemCommandLine);
			public static readonly QualifiedIdentifierNameExpression Argument = new("Argument", Namespaces.SystemCommandLine);
			public static readonly QualifiedIdentifierNameExpression ICommandHandler = new("ICommandHandler", Namespaces.AlbatrossCommandLine);
			public static readonly QualifiedIdentifierNameExpression CommandHost = new("CommandHost", Namespaces.AlbatrossCommandLine);
			public static readonly QualifiedIdentifierNameExpression ParserResult = new ("ParseResult", Namespaces.SystemCommandLine);
		}
		public static class Types {
			public static readonly TypeExpression ICommandHandler = new(Identifiers.ICommandHandler);
			public static readonly TypeExpression CommandHost = new(Identifiers.CommandHost);
		}
	}
}