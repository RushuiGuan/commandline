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
	public class CommandHost: IAsyncDisposable {
		protected IConfiguration configuration;
		protected IHostBuilder hostBuilder;
		ParseResult? parseResult;
		IHost? host;
		protected ParseResult RequiredResult => this.parseResult ?? throw new InvalidOperationException("Parse(args) has not been called yet");
		public CommandBuilder CommandBuilder { get; }

		IHost GetHost() => host ?? throw new InvalidOperationException($"Host has not been built, Call the Build() method!");

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
			this.CommandBuilder.BuildTree(GetHost);
			parseResult = this.CommandBuilder.RootCommand.Parse(args);
			this.hostBuilder.ConfigureServices(services => services.AddSingleton(parseResult));
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

		public virtual void Configure(ParseResult result, ILogger<CommandHost> logger) {
			logger.LogInformation("Configuring application");
		}

		public CommandHost Build() {
			this.host = this.hostBuilder.Build();
			this.Configure(this.RequiredResult, host.Services.GetRequiredService<ILogger<CommandHost>>());
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