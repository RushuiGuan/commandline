using Albatross.CodeAnalysis;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;

namespace Albatross.CommandLine.CodeGen {
	public static class MySymbolProvider {
		public const string VerbAttributeClassName = "Albatross.CommandLine.VerbAttribute";
		public const string VerbAttributeClassNameGeneric1 = "Albatross.CommandLine.VerbAttribute`1";
		public const string VerbAttributeClassNameGeneric2 = "Albatross.CommandLine.VerbAttribute`2";
		
		public static INamedTypeSymbol UseOptionAttributeClassGeneric1(this Compilation compilation) => compilation.GetRequiredSymbol("Albatross.CommandLine.UseOptionAttribute`1");
		public static INamedTypeSymbol UseOptionAttributeClassGeneric2(this Compilation compilation) => compilation.GetRequiredSymbol("Albatross.CommandLine.UseOptionAttribute`2");
		
		public static INamedTypeSymbol UseArgumentAttributeClassGeneric1(this Compilation compilation) => compilation.GetRequiredSymbol("Albatross.CommandLine.UseArgumentAttribute`1");
		public static INamedTypeSymbol UseArgumentAttributeClassGeneric2(this Compilation compilation) => compilation.GetRequiredSymbol("Albatross.CommandLine.UseArgumentAttribute`2");
		
		public static INamedTypeSymbol OptionAttributeClass(this Compilation compilation) => compilation.GetRequiredSymbol("Albatross.CommandLine.OptionAttribute");
		public static INamedTypeSymbol ArgumentAttributeClass(this Compilation compilation) => compilation.GetRequiredSymbol("Albatross.CommandLine.ArgumentAttribute");
		
		public static INamedTypeSymbol DefaultActionHandlerAttributeClass(this Compilation compilation) => compilation.GetRequiredSymbol("Albatross.CommandLine.DefaultActionHandlerAttribute");
		
		public static INamedTypeSymbol OptionGenericClass(this Compilation compilation) => compilation.GetRequiredSymbol("System.CommandLine.Option`1");
		public static INamedTypeSymbol ArgumentGenericClass(this Compilation compilation) => compilation.GetRequiredSymbol("System.CommandLine.Argument`1");
		
	}
}
