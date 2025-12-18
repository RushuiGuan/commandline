using Albatross.Config;
using Albatross.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using System;
using System.CommandLine;
using System.Threading.Tasks;

namespace Albatross.CommandLine {
	/// <summary>
	/// new Setup("My App Description").AddCommands().Parse(args).RegisterServices().Build().InvokeAsync();
	/// </summary>
	public class Setup {
		protected IConfiguration configuration;
		protected IHostBuilder hostBuilder;
		ParseResult? parseResult;
		IHost? host;
		protected ParseResult RequiredResult => this.parseResult ?? throw new InvalidOperationException("Parse() has not been called yet");
		protected SetupSerilog setupSerilog;
		public CommandBuilder CommandBuilder { get; }

		IHost GetHost() => host ?? throw new InvalidOperationException($"Host has not been built, Call the Build() method!");

		public Setup(string description) {
			var environment = EnvironmentSetting.DOTNET_ENVIRONMENT;
			this.hostBuilder = Host.CreateDefaultBuilder().UseSerilog();
			this.setupSerilog = new SetupSerilog().UseConsole(LogEventLevel.Error);
			this.setupSerilog.Create();
			var configBuilder = new ConfigurationBuilder()
				.SetBasePath(AppContext.BaseDirectory)
				.AddJsonFile("appsettings.json", false, true);
			if (!string.IsNullOrEmpty(environment.Value)) { configBuilder.AddJsonFile($"appsettings.{environment.Value}.json", true, true); }
			this.configuration = configBuilder.AddEnvironmentVariables().Build();
			hostBuilder.ConfigureAppConfiguration(builder => {
				builder.Sources.Clear();
				builder.AddConfiguration(configuration);
			});
			CommandBuilder = new CommandBuilder(description);
		}
		/// <summary>
		/// Command Hierarchy should be built before calling Parse
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		public Setup Parse(string[] args) {
			Serilog.Log.Logger.Information("Parsing commandline arguments");
			this.CommandBuilder.BuildTree(GetHost);
			parseResult = this.CommandBuilder.RootCommand.Parse(args);
			this.hostBuilder.ConfigureServices(services => services.AddSingleton(parseResult));
			// right after parsing is the earliest time to configure logging level
			var logLevel = parseResult.GetValue<LogLevel?>(CommandBuilder.VerbosityOptionName);
			if (logLevel != null) {
				SetupSerilog.SwitchConsoleLoggingLevel(logLevel.Value.Translate());
			}
			return this;
		}
		public Setup RegisterServices() {
			this.hostBuilder.ConfigureServices(services => {
				this.RegisterServices(this.RequiredResult, configuration, EnvironmentSetting.DOTNET_ENVIRONMENT, services);
			});
			return this;
		}
		protected virtual void RegisterServices(ParseResult result, IConfiguration configuration, EnvironmentSetting envSetting, IServiceCollection services) {
			Serilog.Log.Information("Registering services");
			services.AddSingleton(new ProgramSetting(configuration));
			services.AddSingleton(envSetting);
			services.AddSingleton(provider => provider.GetRequiredService<ILoggerFactory>().CreateLogger("default"));
			services.AddSingleton<IHostEnvironment, MyHostEnvironment>();
		}
		public virtual void Configure(ParseResult result, ProgramSetting programSetting, EnvironmentSetting environmentSetting, ILogger<Setup> logger) {
			logger.LogInformation("Configuring application");
		}
		public Setup Build() {
			Serilog.Log.Information("Building host");
			this.host = this.hostBuilder.Build();
			var programSetting = host.Services.GetRequiredService<ProgramSetting>();
			var environmentSetting = host.Services.GetRequiredService<EnvironmentSetting>();
			this.Configure(this.RequiredResult, programSetting, environmentSetting, host.Services.GetRequiredService<ILogger<Setup>>());
			return this;
		}
		public Task<int> InvokeAsync() => this.RequiredResult.InvokeAsync();
	}
}