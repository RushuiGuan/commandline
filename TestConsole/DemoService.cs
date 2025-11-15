using Microsoft.Extensions.Logging;
using System;

namespace TestConsole;

// Demonstration service (simplified)
public interface IDemoService {
	void ShowCapabilities();
}

public class DemoService : IDemoService {
	private readonly ILogger<DemoService> logger;

	public DemoService(ILogger<DemoService> logger) {
		this.logger = logger;
	}

	public void ShowCapabilities() {
		logger.LogInformation("Demonstrating System.CommandLine 2.0.0 capabilities");
		Console.WriteLine("System.CommandLine 2.0.0 Features Demonstrated:");
		Console.WriteLine("✓ Root commands and subcommands");
		Console.WriteLine("✓ Options with different types (string, int, bool, FileInfo)");
		Console.WriteLine("✓ Arguments with various arity settings");
		Console.WriteLine("✓ Command descriptions and help text");
		Console.WriteLine("✓ Dependency injection integration");
		Console.WriteLine("✓ Logging integration");
		Console.WriteLine("✓ Error handling and validation");
	}
}
