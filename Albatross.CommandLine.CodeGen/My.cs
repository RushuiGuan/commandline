namespace Albatross.CommandLine.CodeGen {
	public static class My {
		public const string CommandClassName = "Command";
		public const string OptionClassName = "Option";
		public const string ArgumentClassName = "Argument";
		public const string OptionsClassProperty = "OptionsClass";

		public static class Diagnostic {
			public const string IdPrefix = "ComandLineCodeGen";
		}

		public static class ProjectProperty {
			public const string EmitDebugFile = "build_property.EmitAlbatrossCodeGenDebugFile";
		}
	}
}