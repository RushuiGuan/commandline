using Albatross.CodeAnalysis.Symbols;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;

namespace Albatross.CommandLine.CodeGen {
	public static class MySymbolProvider {
		public const string VerbAttributeClassName = "Albatross.CommandLine.VerbAttribute";
		public static INamedTypeSymbol VerbAttributeClass(this Compilation compilation) => compilation.GetRequiredSymbol(VerbAttributeClassName);
		public static INamedTypeSymbol OptionAttributeClass(this Compilation compilation) => compilation.GetRequiredSymbol("Albatross.CommandLine.OptionAttribute");
		public static INamedTypeSymbol ArgumentAttributeClass(this Compilation compilation) => compilation.GetRequiredSymbol("Albatross.CommandLine.ArgumentAttribute");
		public static INamedTypeSymbol ICommandAction_Interface(this Compilation compilation) => compilation.GetRequiredSymbol("Albatross.CommandLine.ICommandAction");
		public static INamedTypeSymbol HelpCommandAction(this Compilation compilation) => compilation.GetRequiredSymbol("Albatross.CommandLine.HelpCommandAction");
	}
}
