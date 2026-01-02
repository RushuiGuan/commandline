using Albatross.CommandLine.Annotations;
using System.CommandLine;
using Xunit;

namespace Albatross.CommandLine.Test.CodeGen {
	[DefaultNameAliases("--option-without-handler", "--wo")]
	public class OptionWithoutHandler : Option<string> {
		public OptionWithoutHandler(string name, params string[] aliases) : base(name, aliases) {
			Description = "original description";
			Required = false;
		}
	}

	public class OptionWithoutDefaultNameAliases : Option<string> {
		public OptionWithoutDefaultNameAliases(string name, params string[] aliases) : base(name, aliases) {
		}
	}

	[OptionHandler(typeof(MyDefaultOptionHandler))]
	[DefaultNameAliases("--option-with-handler", "-w")]
	public class OptionWithHandler : Option<string>, IUseContextValue {
		public OptionWithHandler(string name, params string[] aliases) : base(name, aliases) {
			Description = "original description";
			Required = false;
		}
	}

	public class MyDefaultOptionHandler : IAsyncOptionHandler<OptionWithHandler> {
		private readonly ICommandContext context;
		public MyDefaultOptionHandler(ICommandContext context) {
			this.context = context;
		}
		public Task InvokeAsync(OptionWithHandler symbol, ParseResult result, CancellationToken cancellationToken) {
			context.SetValue(symbol.Name, "MyDefaultOptionHandler");
			return Task.CompletedTask;
		}
	}

	[Verb("test explicit-option", Description = "A test verb with explicit option class and handler")]
	public record class TestExplicitOptionClassAndHandlerParams {
		[UseOption<OptionWithoutHandler>]
		public required string ExplicitOptionWithoutHandler { get; init; }

		[UseOption<OptionWithHandler>]
		public required string ExplicitOptionWithHandler { get; init; }

		[UseOption<OptionWithoutHandler>(AllowMultipleArgumentsPerToken = true, DefaultToInitializer = true, Description = "my own desc", Hidden = true)]
		public string? ExplicitOptionWithItsOwnSetup { get; init; }

		[UseOption<OptionWithoutHandler>("-a")]
		public required string ExplicitOptionWithAliasOverride { get; init; }

		[UseOption<OptionWithoutDefaultNameAliases>]
		public required string ExplicitOptionWithNoDefaultNameAliases { get; init; }

		[UseOption<OptionWithoutHandler>(UseCustomName = true)]
		public required string ExplicitOptionWithCustomName { get; init; }
	}

	public class TestExplicitOptionClassAndHandler {
		TestExplicitOptionClassAndHandlerCommand BuildCommand() {
			var host = new CommandHost("test");
			host.AddCommands();
			host.CommandBuilder.BuildTree(host.GetServiceProvider);
			host.CommandBuilder.TryGetCommand("test explicit-option", out Command cmd);
			return (TestExplicitOptionClassAndHandlerCommand)cmd;
		}

		[Fact]
		public void VerifyExplicitOptionsWithourHandler() {
			var cmd = BuildCommand();
			Assert.IsType<OptionWithoutHandler>(cmd.Option_ExplicitOptionWithoutHandler);
			Assert.Null(cmd.Option_ExplicitOptionWithoutHandler.Action);
			Assert.Equal("--option-without-handler", cmd.Option_ExplicitOptionWithoutHandler.Name);
			Assert.Equal(["--wo"], cmd.Option_ExplicitOptionWithoutHandler.Aliases);
			Assert.Equal("original description", cmd.Option_ExplicitOptionWithoutHandler.Description);
			Assert.True(cmd.Option_ExplicitOptionWithoutHandler.Required);
		}

		[Fact]
		public void VerifyExplicitOptionsWithHandler() {
			var cmd = BuildCommand();
			Assert.IsType<OptionWithHandler>(cmd.Option_ExplicitOptionWithHandler);
			Assert.IsType<AsyncOptionAction<OptionWithHandler, MyDefaultOptionHandler>>(cmd.Option_ExplicitOptionWithHandler.Action);
			Assert.Equal("--option-with-handler", cmd.Option_ExplicitOptionWithHandler.Name);
			Assert.Equal(["-w"], cmd.Option_ExplicitOptionWithHandler.Aliases);
			Assert.Equal("original description", cmd.Option_ExplicitOptionWithHandler.Description);
			Assert.True(cmd.Option_ExplicitOptionWithHandler.Required);
		}

		[Fact]
		public void VerifyExplicitOptionWithItsOwnSetup() {
			var cmd = BuildCommand();
			Assert.IsType<OptionWithoutHandler>(cmd.Option_ExplicitOptionWithItsOwnSetup);
			Assert.Null(cmd.Option_ExplicitOptionWithItsOwnSetup.Action);
			Assert.Equal("--option-with-handler", cmd.Option_ExplicitOptionWithHandler.Name);
			Assert.Equal(["-w"], cmd.Option_ExplicitOptionWithHandler.Aliases);
			Assert.Equal("my own desc", cmd.Option_ExplicitOptionWithItsOwnSetup.Description);
			Assert.True(cmd.Option_ExplicitOptionWithItsOwnSetup.Hidden);
			Assert.True(cmd.Option_ExplicitOptionWithItsOwnSetup.AllowMultipleArgumentsPerToken);
			Assert.False(cmd.Option_ExplicitOptionWithItsOwnSetup.Required);
		}

		[Fact]
		public void VerifyExplicitOptionWithAliasOverride() {
			var cmd = BuildCommand();
			Assert.IsType<OptionWithoutHandler>(cmd.Option_ExplicitOptionWithAliasOverride);
			Assert.Null(cmd.Option_ExplicitOptionWithAliasOverride.Action);
			Assert.Equal("--option-without-handler", cmd.Option_ExplicitOptionWithAliasOverride.Name);
			Assert.Equal(["-a"], cmd.Option_ExplicitOptionWithAliasOverride.Aliases);
		}

		[Fact]
		public void VerifyExplicitOptionWithNoDefaultNameAliases() {
			var cmd = BuildCommand();
			Assert.IsType<OptionWithoutDefaultNameAliases>(cmd.Option_ExplicitOptionWithNoDefaultNameAliases);
			Assert.Null(cmd.Option_ExplicitOptionWithNoDefaultNameAliases.Action);
			Assert.Equal("--explicit-option-with-no-default-name-aliases", cmd.Option_ExplicitOptionWithNoDefaultNameAliases.Name);
			Assert.Empty(cmd.Option_ExplicitOptionWithNoDefaultNameAliases.Aliases);
		}

		[Fact]
		public void VerifyExplicitOptionWithCustomName() {
			var cmd = BuildCommand();
			Assert.Equal("--explicit-option-with-custom-name", cmd.Option_ExplicitOptionWithCustomName.Name);
		}
	}
}