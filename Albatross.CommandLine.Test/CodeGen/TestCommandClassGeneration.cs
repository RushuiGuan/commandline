using Albatross.CommandLine.Annotations;
using System.CommandLine;
using Xunit;

namespace Albatross.CommandLine.Test.CodeGen {
	[Verb("test command class", Alias = ["a", "b"], Description = "my test command class")]
	public record TestCommandClassParams {
		[Argument(Description = "A required text argument")]
		public required string TextArgument { get; init; }

		[Argument]
		public int? OptionalIntArgument { get; init; }

		[Argument(ArityMin = 0)]
		public int OptionalIntArgumentByOverride { get; init; }

		[Argument(ArityMin = 1, ArityMax = 100)]
		public int[] RequiredIntArrayArgument { get; init; } = Array.Empty<int>();

		[Option("a", "ab", Description = "A required text option")]
		public required string RequiredStringOption { get; init; }

		[Option]
		public string? OptionalStringOption { get; init; }

		[Option(Required = false)]
		public string OptionalStringOptionByOverride { get; init; } = string.Empty;

		[Argument(Hidden = true)]
		public int? HiddenArgument { get; init; }

		[Option(Hidden = true)]
		public int? HiddenOption { get; init; }

		[Argument(DefaultToInitializer = true)]
		public int ArgumentWithDefault { get; init; } = 42;

		[Option(DefaultToInitializer = true)]
		public int OptionWithDefault { get; init; } = 7;

		[Option]
		public int[] OptionalArrayOption { get; init; } = Array.Empty<int>();

		[Option(AllowMultipleArgumentsPerToken = true)]
		public int[] OptionalArrayOptionWithMultipleArgumentPerToken { get; init; } = Array.Empty<int>();

		[Option(Required = true)] // parser will require at least one input here
		public int[] RequiredArrayOption { get; init; } = Array.Empty<int>();

		[Option]
		public bool BooleanOption { get; init; }

		[Option(Required = true)]
		public bool RequiredBooleanOption { get; init; }
	}

	public class TestCommandClassGeneration {
		TestCommandClassCommand BuildCommand() {
			var host = new CommandHost("test");
			var cmd = host.CommandBuilder.Add<TestCommandClassCommand>("test command class");
			host.CommandBuilder.BuildTree(host.GetServiceProvider);
			return cmd;
		}

		[Fact]
		public void VerifyClassNameAndKey() {
			var cmd = BuildCommand();
			Assert.Equal("class", cmd.Name);
			Assert.Equal("test command class", cmd.GetCommandKey());
			Assert.Equal("my test command class", cmd.Description);
			Assert.Equal(["a", "b"], cmd.Aliases);
			Assert.NotNull(cmd.Action);
		}

		[Fact]
		public void VerifyRequiredStringArgument() {
			var cmd = BuildCommand();
			var arg = cmd.Argument_TextArgument;
			Assert.NotNull(arg);
			Assert.IsType<Argument<string>>(arg);
			Assert.Equal("text-argument", arg.Name);
			Assert.Equal("A required text argument", arg.Description);
			Assert.Equal(1, arg.Arity.MinimumNumberOfValues);
			Assert.Equal(1, arg.Arity.MaximumNumberOfValues);
		}

		[Fact]
		public void VerifyOptionalIntArgument() {
			var cmd = BuildCommand();
			var arg = cmd.Argument_OptionalIntArgument;
			Assert.NotNull(arg);
			Assert.IsType<Argument<int?>>(arg);
			Assert.Equal("optional-int-argument", arg.Name);
			Assert.Null(arg.Description);
			Assert.Equal(0, arg.Arity.MinimumNumberOfValues);
			Assert.Equal(1, arg.Arity.MaximumNumberOfValues);
		}

		[Fact]
		public void VerifyOptionalIntArgumentByOverride() {
			var cmd = BuildCommand();
			var arg = cmd.Argument_OptionalIntArgumentByOverride;
			Assert.NotNull(arg);
			Assert.IsType<Argument<int>>(arg);
			Assert.Equal("optional-int-argument-by-override", arg.Name);
			Assert.Null(arg.Description);
			Assert.Equal(0, arg.Arity.MinimumNumberOfValues);
			Assert.Equal(1, arg.Arity.MaximumNumberOfValues);
		}


		[Fact]
		public void VerifyRequiredIntArrayArgument() {
			var cmd = BuildCommand();
			var arg = cmd.Argument_RequiredIntArrayArgument;
			Assert.NotNull(arg);
			Assert.IsType<Argument<int[]>>(arg);
			Assert.Equal("required-int-array-argument", arg.Name);
			Assert.Null(arg.Description);
			Assert.Equal(1, arg.Arity.MinimumNumberOfValues);
			Assert.Equal(100, arg.Arity.MaximumNumberOfValues);
		}

		[Fact]
		public void VerifyRequiredStringOption() {
			var cmd = BuildCommand();
			var opt = cmd.Option_RequiredStringOption;
			Assert.NotNull(opt);
			Assert.IsType<Option<string>>(opt);
			Assert.Equal("--required-string-option", opt.Name);
			Assert.Equal(["-a", "--ab"], opt.Aliases);
			Assert.Equal("A required text option", opt.Description);
			Assert.True(opt.Required);
		}

		[Fact]
		public void VerifyOptionalStringOption() {
			var cmd = BuildCommand();
			var opt = cmd.Option_OptionalStringOption;
			Assert.NotNull(opt);
			Assert.IsType<Option<string?>>(opt);
			Assert.Equal("--optional-string-option", opt.Name);
			Assert.Empty(opt.Aliases);
			Assert.Null(opt.Description);
			Assert.False(opt.Required);
		}

		[Fact]
		public void VerifyOptionalStringOptionByOverride() {
			var cmd = BuildCommand();
			var opt = cmd.Option_OptionalStringOptionByOverride;
			Assert.NotNull(opt);
			Assert.IsType<Option<string>>(opt);
			Assert.Equal("--optional-string-option-by-override", opt.Name);
			Assert.Null(opt.Description);
			Assert.False(opt.Required);
		}

		[Fact]
		public void VerifyHiddenArgument() {
			var cmd = BuildCommand();
			var arg = cmd.Argument_HiddenArgument;
			Assert.NotNull(arg);
			Assert.IsType<Argument<int?>>(arg);
			Assert.Equal("hidden-argument", arg.Name);
			Assert.True(arg.Hidden);
			Assert.Equal(0, arg.Arity.MinimumNumberOfValues);
			Assert.Equal(1, arg.Arity.MaximumNumberOfValues);
		}

		[Fact]
		public void VerifyHiddenOption() {
			var cmd = BuildCommand();
			var opt = cmd.Option_HiddenOption;
			Assert.NotNull(opt);
			Assert.IsType<Option<int?>>(opt);
			Assert.Equal("--hidden-option", opt.Name);
			Assert.True(opt.Hidden);
		}

		[Fact]
		public void VerifyArgumentWithDefault() {
			var cmd = BuildCommand();
			var arg = cmd.Argument_ArgumentWithDefault;
			Assert.NotNull(arg);
			Assert.IsType<Argument<int>>(arg);
			Assert.Equal("argument-with-default", arg.Name);
			// if an argument has a default value, its arity minimum should be 0
			Assert.Equal(0, arg.Arity.MinimumNumberOfValues);
			Assert.Equal(1, arg.Arity.MaximumNumberOfValues);
			Assert.NotNull(arg.DefaultValueFactory);
			Assert.Equal(42, arg.DefaultValueFactory(null));
		}

		[Fact]
		public void VerifyOptionWithDefault() {
			var cmd = BuildCommand();
			var opt = cmd.Option_OptionWithDefault;
			Assert.NotNull(opt);
			Assert.IsType<Option<int>>(opt);
			Assert.Equal("--option-with-default", opt.Name);
			Assert.False(opt.Required);
			Assert.NotNull(opt.DefaultValueFactory);
			Assert.Equal(7, opt.DefaultValueFactory(null));
		}

		[Fact]
		public void VerifyOptionalArrayOption() {
			var cmd = BuildCommand();
			var opt = cmd.Option_OptionalArrayOption;
			Assert.NotNull(opt);
			Assert.IsType<Option<int[]>>(opt);
			Assert.Equal("--optional-array-option", opt.Name);
			Assert.Null(opt.Description);
			Assert.False(opt.Required);
		}

		[Fact]
		public void VerifyOptionArrayOptionWithMultipleArgumentPerToken() {
			var cmd = BuildCommand();
			var opt = cmd.Option_OptionalArrayOptionWithMultipleArgumentPerToken;
			Assert.NotNull(opt);
			Assert.IsType<Option<int[]>>(opt);
			Assert.Equal("--optional-array-option-with-multiple-argument-per-token", opt.Name);
			Assert.Null(opt.Description);
			Assert.False(opt.Required);
			Assert.True(opt.AllowMultipleArgumentsPerToken);
		}

		[Fact]
		public void VerifyRequiredArrayOption() {
			var cmd = BuildCommand();
			var opt = cmd.Option_RequiredArrayOption;
			Assert.NotNull(opt);
			Assert.IsType<Option<int[]>>(opt);
			Assert.Equal("--required-array-option", opt.Name);
			Assert.True(opt.Required);
		}

		[Fact]
		public void VerifyBooleanOption() {
			var cmd = BuildCommand();
			var opt = cmd.Option_BooleanOption;
			Assert.NotNull(opt);
			Assert.IsType<Option<bool>>(opt);
			Assert.Equal("--boolean-option", opt.Name);
			Assert.Null(opt.Description);
			Assert.False(opt.Required);
		}

		[Fact]
		public void VerifyRequiredBooleanOption() {
			var cmd = BuildCommand();
			var opt = cmd.Option_RequiredBooleanOption;
			Assert.NotNull(opt);
			Assert.IsType<Option<bool>>(opt);
			Assert.Equal("--required-boolean-option", opt.Name);
			Assert.True(opt.Required);
		}
	}
}