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
using System.Linq;
using System.Threading.Tasks;

namespace Albatross.CommandLine {
	public class Setup {
		protected IConfiguration configuration;
		protected IHostBuilder hostBuilder;
		protected ParseResult? result;
		public RootCommand RootCommand { get; }

		public Setup(string rootCommandDescription) {
			var environment = EnvironmentSetting.DOTNET_ENVIRONMENT.Value;
			var hostBuilder = Host.CreateDefaultBuilder().UseSerilog();
			var setupSerilog = ConfigureLogging(new SetupSerilog(), environment);
			var configBuilder = new ConfigurationBuilder()
				.SetBasePath(AppContext.BaseDirectory)
				.AddJsonFile("appsettings.json", false, true);
			if (!string.IsNullOrEmpty(environment)) { configBuilder.AddJsonFile($"appsettings.{environment}.json", true, true); }
			this.configuration = configBuilder.AddEnvironmentVariables().Build();

			hostBuilder.ConfigureAppConfiguration(builder => {
				builder.Sources.Clear();
				builder.AddConfiguration(configuration);
			});
			CreateRootCommand(rootCommandDescription);
		}

		Setup ConfigureServices() {
			this.hostBuilder.ConfigureServices(services => {
				this.RegisterServices(this.result ?? throw new InvalidOperationException("Call Parse() before Build()"), configuration, EnvironmentSetting.DOTNET_ENVIRONMENT, services);
			});
			return this;
		}

		Setup AddCommands() {
			return this;
		}

		Setup Parse(string[] args) {
			result = this.RootCommand.Parse(args);
			return this;
		}

		protected virtual SetupSerilog ConfigureLogging(SetupSerilog setup, string environment) {
			setup.UseConfigFile(environment, null, null);
			return setup;
		}

		private Task AddLoggingMiddleware(InvocationContext context, Func<InvocationContext, Task> next) {
			var logOption = this.RootCommand.Options.OfType<Option<LogEventLevel?>>().First();
			var result = context.ParseResult.GetValueForOption(logOption);
			if (result != null) {
				SetupSerilog.SwitchConsoleLoggingLevel(result.Value);
			}
			return next(context);
		}

		public RootCommand CreateRootCommand(IHost host, string descriptions) {
			var root = new RootCommand(descriptions);
			var logOption = new Option<LogEventLevel?>("--verbosity", "-v") {
				Description = "Set the verbosity level of logging",
				Arity = ArgumentArity.ZeroOrOne,
				DefaultValueFactory = _ => LogEventLevel.Error
			};
			root.Add(logOption);
			root.Add(new Option<bool>("--benchmark", "Show the time it takes to run the command in milliseconds"));
			root.Add(new Option<bool>("--show-stack", "Show the full stack when an exception has been thrown"));
			var globalHandler = new GlobalCommandHandler(host);
			root.SetAction(globalHandler.InvokeAsync);
			return root;
		}

		protected virtual void RegisterServices(ParseResult result, IConfiguration configuration, EnvironmentSetting envSetting, IServiceCollection services) {
			Serilog.Log.Debug("Registering services");
			services.AddSingleton(new ProgramSetting(configuration));
			services.AddSingleton(envSetting);
			services.AddSingleton(provider => provider.GetRequiredService<ILoggerFactory>().CreateLogger("default"));
			services.AddSingleton<IHostEnvironment, MyHostEnvironment>();
		}

		protected virtual void ConfigureLogging(LoggerConfiguration cfg) => SetupSerilog.UseConsole(cfg, null);
		public virtual void Configure(ParseResult result, ProgramSetting programSetting, EnvironmentSetting environmentSetting, ILogger<Setup> logger) { }
	}
}