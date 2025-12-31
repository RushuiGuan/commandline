using Albatross.CommandLine.Annotations;
using System.CommandLine;
using Xunit;

namespace Albatross.CommandLine.Test.CodeGen {
	public class MyExplicitArgument : Argument<int> {
		public MyExplicitArgument() : this("my-explicit-arg") {
		}

		public MyExplicitArgument(string name) : base(name) {
			Description = "An explicit argument example";
			Arity = new ArgumentArity(1, 1);
		}
	}

	[Verb("test explicit-argument", Description = "A test verb with explicit argument class")]
	public class TestExplicitArgumentParams {
		[UseArgument<MyExplicitArgument>]
		public required int ExplicitArgument1 { get; init; }

		[UseArgument<MyExplicitArgument>(Description = "my own desc")]
		public required int ExplicitArgument2 { get; init; }

		[UseArgument<MyExplicitArgument>]
		public int? ExplicitArgument3 { get; init; }

		[UseArgument<MyExplicitArgument>(DefaultToInitializer = true)]
		public int ExplicitArgument4 { get; init; } = 4;
	}

	public class TestExplicitArgument {
		TestExplicitArgumentCommand BuildCommand() {
			var host = new CommandHost("test");
			host.AddCommands();
			host.CommandBuilder.BuildTree(host.GetServiceProvider);
			host.CommandBuilder.TryGetCommand("test explicit-argument", out Command cmd);
			return (TestExplicitArgumentCommand)cmd;
		}

		[Fact]
		public void Run() {
			var cmd = BuildCommand();
			Assert.NotNull(cmd.Argument_ExplicitArgument1);
			Assert.Equal("explicit-argument1", cmd.Argument_ExplicitArgument1.Name);
			Assert.Equal("An explicit argument example", cmd.Argument_ExplicitArgument1.Description);
			Assert.Equal(1, cmd.Argument_ExplicitArgument1.Arity.MinimumNumberOfValues);
			Assert.Equal(1, cmd.Argument_ExplicitArgument1.Arity.MaximumNumberOfValues);

			Assert.NotNull(cmd.Argument_ExplicitArgument2);
			Assert.Equal("explicit-argument2", cmd.Argument_ExplicitArgument2.Name);
			Assert.Equal("my own desc", cmd.Argument_ExplicitArgument2.Description);
			Assert.Equal(1, cmd.Argument_ExplicitArgument2.Arity.MinimumNumberOfValues);
			Assert.Equal(1, cmd.Argument_ExplicitArgument2.Arity.MaximumNumberOfValues);

			Assert.NotNull(cmd.Argument_ExplicitArgument3);
			Assert.Equal("explicit-argument3", cmd.Argument_ExplicitArgument3.Name);
			Assert.Equal("An explicit argument example", cmd.Argument_ExplicitArgument3.Description);
			Assert.Equal(0, cmd.Argument_ExplicitArgument3.Arity.MinimumNumberOfValues);
			Assert.Equal(1, cmd.Argument_ExplicitArgument3.Arity.MaximumNumberOfValues);

			Assert.NotNull(cmd.Argument_ExplicitArgument4);
			Assert.Equal("explicit-argument4", cmd.Argument_ExplicitArgument4.Name);
			Assert.Equal("An explicit argument example", cmd.Argument_ExplicitArgument4.Description);
			Assert.Equal(0, cmd.Argument_ExplicitArgument4.Arity.MinimumNumberOfValues);
			Assert.Equal(1, cmd.Argument_ExplicitArgument4.Arity.MaximumNumberOfValues);
		}
	}
}