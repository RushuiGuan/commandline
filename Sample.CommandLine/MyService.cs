namespace Sample.CommandLine {
	public interface IMyService {
		void DoSomething();
	}
	public class MyService : IMyService {
		public void DoSomething() {
			System.Console.WriteLine("Hello World");
		}
	}
}