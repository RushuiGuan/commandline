namespace Albatross.CommandLine.CodeGen {
	public static class My {
		public const string VerbAttributeClass = "Albatross.CommandLine.VerbAttribute";
		public const string OptionsClassProperty = "OptionsClass";
		public const string OptionAttributeClass = "Albatross.CommandLine.OptionAttribute";
		public const string IgnoreAttributeClass = "Albatross.CommandLine.IgnoreAttribute";
		public const string ArgumentAttributeClass = "Albatross.CommandLine.ArgumentAttribute";
		public const string ICommandHandler_InterfaceName = "System.CommandLine.Invocation.ICommandHandler";
		public const string CommandClassName = "Command";
		public const string OptionClassName = "Option";
		public const string ArgumentClassName = "Argument";
		public const string Postfix_Options = "Options";
		public const string SetupClassName = "Albatross.CommandLine.Setup";

		public static class Diagnostic {
			public const string IdPrefix = "ComandLineCodeGen";
		}

		public static class ProjectProperty {
			public const string EmitDebugFile = "build_property.EmitAlbatrossCodeGenDebugFile";
		}
	}
}