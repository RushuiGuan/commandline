namespace Sample.CommandLine {
	public interface ICodeGenerator {
		string Generate(CodeGenOptions options);
	}
	public class CSharpCodeGenerator : ICodeGenerator {
		public string Generate(CodeGenOptions options) {
			return $"generating c# code: {options}";
		}
	}
	public class TypeScriptCodeGenerator : ICodeGenerator {
		public string Generate(CodeGenOptions options) {
			return $"type script: {options}";
		}
	}
}