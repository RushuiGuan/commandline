namespace Sample.CommandLine {
	public interface ICodeGenerator {
		string Generate(ExampleCodeGenOptions options);
	}
	public class CSharpCodeGenerator : ICodeGenerator {
		public string Generate(ExampleCodeGenOptions options) {
			return $"generating c# code: {options}";
		}
	}
	public class TypeScriptCodeGenerator : ICodeGenerator {
		public string Generate(ExampleCodeGenOptions options) {
			return $"type script: {options}";
		}
	}
}