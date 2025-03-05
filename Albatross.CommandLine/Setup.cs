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
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading.Tasks;

namespace Albatross.CommandLine {
	public class Setup {
		protected virtual string RootCommandDescription => string.Empty;
		public Setup() {
			this.RootCommand = CreateRootCommand();
			this.CommandBuilder = new CommandLineBuilder(this.RootCommand);
			this.CommandBuilder.AddMiddleware(AddLoggingMiddleware);
			this.CommandBuilder.UseHost(args => Host.CreateDefaultBuilder(), hostBuilder => {
				var environment = EnvironmentSetting.DOTNET_ENVIRONMENT;
				var logger = new SetupSerilog().Configure(ConfigureLogging).Create();
				hostBuilder.UseSerilog(logger);
				logger.Debug("Building Host");
				var configBuilder = new ConfigurationBuilder()
					.SetBasePath(AppContext.BaseDirectory)
					.AddJsonFile("appsettings.json", true, false);
				if (!string.IsNullOrEmpty(environment.Value)) {
					configBuilder.AddJsonFile($"appsettings.{environment.Value}.json", true, false);
				}
				logger.Debug("Creating configuration");
				var configuration = configBuilder.AddEnvironmentVariables().Build();
				hostBuilder.ConfigureAppConfiguration(builder => {
					builder.Sources.Clear();
					builder.AddConfiguration(configuration);
				}).ConfigureServices((context, svc) => RegisterServices(context.GetInvocationContext(), context.Configuration, environment, svc));
			});
			this.ConfigureBuilder();
		}

		public virtual CommandLineBuilder ConfigureBuilder() {
			return this.CommandBuilder.UseDefaults();
		}
		private Task AddLoggingMiddleware(InvocationContext context, Func<InvocationContext, Task> next) {
			var logOption = this.RootCommand.Options.OfType<Option<LogEventLevel?>>().First();
			var result = context.ParseResult.GetValueForOption(logOption);
			if (result != null) {
				SetupSerilog.SwitchConsoleLoggingLevel(result.Value);
			}
			return next(context);
		}
		/// <summary>
		/// Overwrite this method to use a custom global command handler for all commands
		/// </summary>
		public virtual ICommandHandler CreateGlobalCommandHandler(Command command) {
			return new GlobalCommandHandler(command);
		}

		public virtual RootCommand CreateRootCommand() {
			var cmd = new RootCommand(RootCommandDescription);
			cmd.Handler = new HelpCommandHandler();
			var logOption = new Option<LogEventLevel?>("--verbosity", () => LogEventLevel.Error, "Change the verbosity of logging");
			logOption.AddAlias("-v");
			cmd.AddGlobalOption(logOption);
			cmd.AddGlobalOption(new Option<bool>("--benchmark", "Show the time it takes to run the command in milliseconds"));
			cmd.AddGlobalOption(new Option<bool>("--show-stack", "Show the full stack when an exception has been thrown"));
			return cmd;
		}
		public RootCommand RootCommand { get; }
		public CommandLineBuilder CommandBuilder { get; }

		[Obsolete]
		public Setup AddCommand<TCommand>() where TCommand : Command, new() {
			var command = new TCommand();
			if (command is IRequireInitialization instance) {
				instance.Init();
			}
			this.RootCommand.Add(command);
			return this;
		}

		public virtual void RegisterServices(InvocationContext context, IConfiguration configuration, EnvironmentSetting envSetting, IServiceCollection services) {
			Serilog.Log.Debug("Registering services");
			services.AddSingleton(new ProgramSetting(configuration));
			services.AddSingleton(envSetting);
			services.AddSingleton(provider => provider.GetRequiredService<ILoggerFactory>().CreateLogger("default"));
			services.AddSingleton<IHostEnvironment, MyHostEnvironment>();
			services.AddOptions<GlobalOptions>().BindCommandLine();
		}

		protected virtual void ConfigureLogging(LoggerConfiguration cfg) {
			SetupSerilog.UseConsole(cfg, null);
		}
	}
}