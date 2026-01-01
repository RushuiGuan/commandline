using Albatross.CodeAnalysis;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;

namespace Albatross.CommandLine.CodeGen {
	public static class MySymbolProvider {
		public const string VerbAttributeClassName = "Albatross.CommandLine.Annotations.VerbAttribute";
		public const string VerbAttributeClassNameGeneric1 = "Albatross.CommandLine.Annotations.VerbAttribute`1";
		public const string VerbAttributeClassNameGeneric2 = "Albatross.CommandLine.Annotations.VerbAttribute`2";
		
		public static INamedTypeSymbol UseOptionAttributeClassGeneric1(this Compilation compilation) 
			=> compilation.GetRequiredSymbol("Albatross.CommandLine.Annotations.UseOptionAttribute`1");
		public static INamedTypeSymbol UseOptionAttributeClassGeneric2(this Compilation compilation) 
			=> compilation.GetRequiredSymbol("Albatross.CommandLine.Annotations.UseOptionAttribute`2");
		
		public static INamedTypeSymbol UseArgumentAttributeClassGeneric1(this Compilation compilation) 
			=> compilation.GetRequiredSymbol("Albatross.CommandLine.Annotations.UseArgumentAttribute`1");
		
		public static INamedTypeSymbol OptionAttributeClass(this Compilation compilation) 
			=> compilation.GetRequiredSymbol("Albatross.CommandLine.Annotations.OptionAttribute");
		public static INamedTypeSymbol ArgumentAttributeClass(this Compilation compilation) 
			=> compilation.GetRequiredSymbol("Albatross.CommandLine.Annotations.ArgumentAttribute");
		
		public static INamedTypeSymbol DefaultOptionHandlerAttributeClass(this Compilation compilation) 
			=> compilation.GetRequiredSymbol("Albatross.CommandLine.Annotations.DefaultOptionHandlerAttribute");
		
		public static INamedTypeSymbol OptionGenericClass(this Compilation compilation) => compilation.GetRequiredSymbol("System.CommandLine.Option`1");
		public static INamedTypeSymbol ArgumentGenericClass(this Compilation compilation) => compilation.GetRequiredSymbol("System.CommandLine.Argument`1");
		public static INamedTypeSymbol IUseContextValueInterface(this Compilation compilation) => compilation.GetRequiredSymbol("Albatross.CommandLine.IUseContextValue");
	}
}
