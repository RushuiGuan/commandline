namespace Sample.CommandLine {
	public interface ICodeGenerator {
		string Generate(ExampleCommandSpecificRegistrationsParams parameters);
	}
	public class CSharpCodeGenerator : ICodeGenerator {
		public string Generate(ExampleCommandSpecificRegistrationsParams parameters) {
			return $"generating c# code: {parameters}";
		}
	}
	public class TypeScriptCodeGenerator : ICodeGenerator {
		public string Generate(ExampleCommandSpecificRegistrationsParams parameters) {
			return $"type script: {parameters}";
		}
	}
}