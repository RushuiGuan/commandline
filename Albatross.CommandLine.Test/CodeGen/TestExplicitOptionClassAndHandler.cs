using Albatross.CommandLine.Annotations;
using System.CommandLine;
using Xunit;

namespace Albatross.CommandLine.Test.CodeGen {
	public class OptionWithNoHandler : Option<string> {
		public OptionWithNoHandler(string name, params string[] aliases) : base(name, aliases) { }
		public OptionWithNoHandler() : this("--option-without-handler", "--wo") {
			Description = "original description";
			Required = false;
		}
	}

	[DefaultOptionHandler(typeof(MyDefaultOptionHandler))]
	public class OptionWithHandler : Option<string>, IUseContextValue {
		public OptionWithHandler(string name, params string[] aliases) : base(name, aliases) {
			Description = "original description";
			Required = false;
		}
		public OptionWithHandler() : this("--option-with-handler", "-w") { }
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

	public class MyAltOptionHandler : IAsyncOptionHandler<OptionWithHandler> {
		private readonly ICommandContext context;
		public MyAltOptionHandler(ICommandContext context) {
			this.context = context;
		}
		public Task InvokeAsync(OptionWithHandler symbol, ParseResult result, CancellationToken cancellationToken) {
			context.SetValue(symbol.Name, "MyAltOptionHandler");
			return Task.CompletedTask;
		}
	}

	[Verb("test explicit-option", Description = "A test verb with explicit option class and handler")]
	public record class TestExplicitOptionClassAndHandlerParams {
		[UseOption<OptionWithNoHandler>(Description = "my own desc")]
		public required string ExplicitOptionWithoutHandler { get; init; }

		[UseOption<OptionWithHandler>]
		public required string ExplicitOptionWithHandler { get; init; }

		[UseOption<OptionWithHandler, MyAltOptionHandler>(UseCustomNameAlias = true)]
		public required string ExplicitOptionWithExplicitHandler { get; init; }
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
		public void VerifyExplicitOptions() {
			var cmd = BuildCommand();
			Assert.IsType<OptionWithNoHandler>(cmd.Option_ExplicitOptionWithoutHandler);
			Assert.Null(cmd.Option_ExplicitOptionWithoutHandler.Action);
			Assert.Equal("--option-without-handler", cmd.Option_ExplicitOptionWithoutHandler.Name);
			Assert.Equal(["--wo"], cmd.Option_ExplicitOptionWithoutHandler.Aliases);
			Assert.Equal("my own desc", cmd.Option_ExplicitOptionWithoutHandler.Description);
			Assert.True(cmd.Option_ExplicitOptionWithoutHandler.Required);

			Assert.IsType<OptionWithHandler>(cmd.Option_ExplicitOptionWithHandler);
			Assert.IsType<AsyncOptionAction<OptionWithHandler, MyDefaultOptionHandler>>(cmd.Option_ExplicitOptionWithHandler.Action);

			Assert.IsType<OptionWithHandler>(cmd.Option_ExplicitOptionWithExplicitHandler);
			Assert.IsType<AsyncOptionAction<OptionWithHandler, MyAltOptionHandler>>(cmd.Option_ExplicitOptionWithExplicitHandler.Action);
			Assert.Equal("--explicit-option-with-explicit-handler", cmd.Option_ExplicitOptionWithExplicitHandler.Name);
			Assert.Equal([], cmd.Option_ExplicitOptionWithExplicitHandler.Aliases);
			Assert.Equal("original description", cmd.Option_ExplicitOptionWithExplicitHandler.Description);
		}
	}
}