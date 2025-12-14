using Albatross.CodeGen.CSharp.Expressions;

namespace Albatross.CommandLine.CodeGen {
	public static class MyDefined {
		public static class Namespaces {
			public static readonly NamespaceExpression SystemCommandLine = new("System.CommandLine");
		}
		public static class Identifiers {
			public static readonly QualifiedIdentifierNameExpression Command = new("Command", MyDefined.Namespaces.SystemCommandLine);
			public static readonly QualifiedIdentifierNameExpression Option = new("Option", MyDefined.Namespaces.SystemCommandLine);
			public static readonly QualifiedIdentifierNameExpression Argument = new("Argument", MyDefined.Namespaces.SystemCommandLine);
		}
	}
}