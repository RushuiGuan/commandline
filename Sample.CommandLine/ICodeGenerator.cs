namespace Sample.CommandLine {
	public interface ICodeGenerator {
		string Generate(ExampleCommandSpecificRegistrationsOptions options);
	}
	public class CSharpCodeGenerator : ICodeGenerator {
		public string Generate(ExampleCommandSpecificRegistrationsOptions options) {
			return $"generating c# code: {options}";
		}
	}
	public class TypeScriptCodeGenerator : ICodeGenerator {
		public string Generate(ExampleCommandSpecificRegistrationsOptions options) {
			return $"type script: {options}";
		}
	}
}