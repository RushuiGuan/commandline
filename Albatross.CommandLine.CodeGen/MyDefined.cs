using Albatross.CodeGen.CSharp.Expressions;

namespace Albatross.CommandLine.CodeGen {
	public static class MyDefined {
		public static class Namespaces {
			public static readonly NamespaceExpression SystemCommandLine = new NamespaceExpression("System.CommandLine");
		}
		public static class Identifiers {
			public static readonly QualifiedIdentifierNameExpression Command = new QualifiedIdentifierNameExpression("Command", MyDefined.Namespaces.SystemCommandLine);
			public static readonly QualifiedIdentifierNameExpression Option = new QualifiedIdentifierNameExpression("Option", MyDefined.Namespaces.SystemCommandLine);
			public static readonly QualifiedIdentifierNameExpression Argument = new QualifiedIdentifierNameExpression("Argument", MyDefined.Namespaces.SystemCommandLine);
		}
	}
}