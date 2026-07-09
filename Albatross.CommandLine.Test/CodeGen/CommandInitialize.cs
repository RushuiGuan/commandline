using Albatross.CommandLine.Annotations;
using Xunit;

namespace Albatross.CommandLine.Test.CodeGen {
	[Verb("test initialize", Description = "A test verb that exercises the generated Initialize() hook")]
	public record InitializeHookParams {
		[Option]
		public string? Name { get; init; }
	}

	// The generator emits `public sealed partial class InitializeHookCommand : Command` whose constructor
	// calls this.Initialize().  This partial supplies the hook implementation to prove it runs on construction.
	public sealed partial class InitializeHookCommand {
		public bool InitializeWasCalled { get; private set; }
		partial void Initialize() {
			InitializeWasCalled = true;
		}
	}

	/// <summary>
	/// Verifies the generated command constructor invokes the partial <c>Initialize()</c> hook, the public
	/// extension point commands use for custom validators/configuration.
	/// </summary>
	public class CommandInitialize {
		[Fact]
		public void InitializeHookRunsDuringConstruction() {
			var host = new CommandHost("test");
			var cmd = host.CommandBuilder.Add<InitializeHookCommand>("test initialize");
			Assert.True(cmd.InitializeWasCalled);
		}
	}
}
