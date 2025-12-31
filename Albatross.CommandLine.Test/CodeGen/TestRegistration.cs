using Albatross.CommandLine.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Albatross.CommandLine.Test.CodeGen {
	[Verb<TestRegistrationHandler>("test registration", Description = "A test verb for registration")]
	public class TestRegistrationParams {
	}

	[Verb("shared 2", BaseParamsClass = typeof(SharedParams))]
	public class Test2Params : SharedParams {
	}
	[Verb("shared 1", BaseParamsClass = typeof(SharedParams))]
	public class Test1Params : SharedParams {
	}
	public class SharedParams {
	}

	public class TestRegistrationHandler : IAsyncCommandHandler {
		public Task<int> InvokeAsync(CancellationToken cancellationToken) {
			return Task.FromResult(0);
		}
	}
	public class TestRegistration {
		[Fact]
		public void TestNormalRegistration() {
			var host = new CommandHost("_");
			host.RegisterServices((result, config, services) => services.RegisterCommands());
			host.AddCommands();
			host.Parse(["test", "registration"]);
			host.CommandBuilder.BuildTree(host.GetServiceProvider);
			host.Build();

			host.CommandBuilder.TryGetCommand("test registration", out var cmd);
			Assert.NotNull(cmd);
			Assert.IsType<TestRegistrationCommand>(cmd);
			var service = host.GetServiceProvider();
			Assert.NotNull(service.GetService<TestRegistrationParams>());
			var instance = service.GetRequiredKeyedService<IAsyncCommandHandler>("test registration");
			Assert.IsType<TestRegistrationHandler>(instance);
		}

		[Fact]
		public void TestSharedParamsRegistration() {
			var host = new CommandHost("_");
			host.RegisterServices((result, config, services) => services.RegisterCommands());
			host.AddCommands();
			host.Parse(["shared", "1"]);
			host.CommandBuilder.BuildTree(host.GetServiceProvider);
			host.Build();
			host.CommandBuilder.TryGetCommand("shared 1", out var cmd);
			Assert.NotNull(cmd);
			Assert.IsType<Test1Command>(cmd);
			var service = host.GetServiceProvider();
			var @params = service.GetService<SharedParams>();
			Assert.NotNull(@params);
			Assert.IsType<Test1Params>(@params);
			var instance = service.GetKeyedService<IAsyncCommandHandler>("shared 1");
			Assert.Null(instance);
		}
	}
}
