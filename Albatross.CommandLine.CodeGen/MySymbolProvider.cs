using Albatross.CodeAnalysis.Symbols;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;

namespace Albatross.CommandLine.CodeGen {
	public static class MySymbolProvider {
		public const string VerbAttributeClassName = "Albatross.CommandLine.VerbAttribute";
		public const string VerbAttributeClassNameGeneric1 = "Albatross.CommandLine.VerbAttribute`1";
		public const string VerbAttributeClassNameGeneric2 = "Albatross.CommandLine.VerbAttribute`2";
		public static INamedTypeSymbol OptionAttributeClass(this Compilation compilation) => compilation.GetRequiredSymbol("Albatross.CommandLine.OptionAttribute");
		public static INamedTypeSymbol ArgumentAttributeClass(this Compilation compilation) => compilation.GetRequiredSymbol("Albatross.CommandLine.ArgumentAttribute");
	}
}
