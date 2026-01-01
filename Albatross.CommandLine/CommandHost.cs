using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.CommandLine;
using System.Threading.Tasks;

namespace Albatross.CommandLine {
	/// <summary>
	/// Host for running commands 
	/// <code>
	///		await using var host = new CommandHost("Sample Command Line Application")
	///			.RegisterServices((result, config, services) => {...})
	///			.AddCommands()
	///			.Parse(args)
	///			.ConfigureHost((result, builder) => {...})
	///			.Build();
	///		return await host.InvokeAsync();
	///	</code>
	/// </summary>
	public class CommandHost : IAsyncDisposable {
		readonly IConfiguration configuration;
		readonly IHostBuilder hostBuilder;
		ParseResult? parseResult;
		IHost? host;
		private Action<ParseResult, IServiceProvider> configApplication = (result, provider) => { };
		private IServiceScope? scope;
		public ParseResult RequiredResult => this.parseResult ?? throw new InvalidOperationException("Parse(args) has not been called yet");
		public CommandBuilder CommandBuilder { get; }

		public IServiceProvider GetServiceProvider() => scope?.ServiceProvider ?? throw new InvalidOperationException($"Host has not been built, Call the Build() method!");

		public CommandHost(string description) {
			this.hostBuilder = Host.CreateDefaultBuilder();
			this.configuration = new ConfigurationBuilder().Build();
			hostBuilder.ConfigureAppConfiguration(builder => {
				builder.AddConfiguration(configuration);
			});
			CommandBuilder = new CommandBuilder(description);
		}

		/// <summary>
		/// Command Hierarchy should be built before calling Parse
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		public CommandHost Parse(string[] args) {
			this.CommandBuilder.BuildTree(GetServiceProvider);
			parseResult = this.CommandBuilder.RootCommand.Parse(args);
			this.hostBuilder.ConfigureServices(services => {
				services.AddSingleton(parseResult);
				services.AddSingleton<ICommandContext, CommandContext>();
			});
			// configure default logging level based on parsed result
			var logLevel = parseResult.GetRequiredValue(CommandBuilder.VerbosityOption);
			this.hostBuilder.ConfigureLogging((_, builder) => builder.SetMinimumLevel(logLevel));
			return this;
		}

		public CommandHost ConfigureHost(Action<IHostBuilder> configure) {
			configure(this.hostBuilder);
			return this;
		}

		public CommandHost ConfigureHost(Action<ParseResult, IHostBuilder> configure) {
			configure(this.RequiredResult, this.hostBuilder);
			return this;
		}

		public CommandHost RegisterServices(Action<ParseResult, IConfiguration, IServiceCollection> action) {
			this.hostBuilder.ConfigureServices(services => action(this.RequiredResult, configuration, services));
			return this;
		}

		public CommandHost ConfigureApplication(Action<ParseResult, IServiceProvider> action) {
			configApplication += action;
			return this;
		}

		public CommandHost Build() {
			this.host = this.hostBuilder.Build();
			this.scope = this.host.Services.CreateScope();
			this.configApplication(this.RequiredResult, host.Services);
			return this;
		}

		public Task<int> InvokeAsync() => this.RequiredResult.InvokeAsync();

		public async ValueTask DisposeAsync() {
			if (host is IAsyncDisposable hostAsyncDisposable) {
				await hostAsyncDisposable.DisposeAsync();
			} else if (host != null) {
				host.Dispose();
			}
		}
	}
}