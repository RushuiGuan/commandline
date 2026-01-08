using Sample.CommandLine.CommandSpecificRegistrations;

namespace Sample.CommandLine.Services {
	public interface ICodeGenerator {
		string Generate(CodeGenParams parameters);
	}
	public class CSharpCodeGenerator : ICodeGenerator {
		public string Generate(CodeGenParams parameters) {
			return $"generating c# code: {parameters}";
		}
	}
	public class TypeScriptCodeGenerator : ICodeGenerator {
		public string Generate(CodeGenParams parameters) {
			return $"type script: {parameters}";
		}
	}
}