using System.Threading.Tasks;

namespace Sample.CommandLine {
	public interface IMyService {
		Task<string> DoSomething();
	}
	public class MyService : IMyService {
		public Task<string> DoSomething() {
			return Task.FromResult("Hello World!");
		}
	}
}