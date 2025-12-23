using Albatross.Config;
using Albatross.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using System;

namespace Albatross.CommandLine.Defaults {
	public static class Extensions {
		public static LogEventLevel ToSerilogLevel(this LogLevel level) =>
			level switch
			{
				LogLevel.Trace => LogEventLevel.Verbose,
				LogLevel.Debug => LogEventLevel.Debug,
				LogLevel.Information => LogEventLevel.Information,
				LogLevel.Warning => LogEventLevel.Warning,
				LogLevel.Error => LogEventLevel.Error,
				LogLevel.Critical => LogEventLevel.Fatal,
				_ => LogEventLevel.Information
			};
		
		public static CommandHost WithDefaults(this CommandHost commandHost)
			=> commandHost.WithConfig().WithSerilog();

		public static CommandHost WithSerilog(this CommandHost commandHost) {
			commandHost.ConfigureHost((result, builder) => {
				var logLevel = result.GetRequiredValue(CommandBuilder.VerbosityOption);
				builder.UseSerilog();
				var setupSerilog = new SetupSerilog().UseConsole(logLevel.ToSerilogLevel());
				if (logLevel != LogLevel.None) {
					setupSerilog.Create();
				}
			});
			return commandHost;
		}

		public static CommandHost WithConfig(this CommandHost commandHost) {
			commandHost.ConfigureHost(builder => {
				var environment = EnvironmentSetting.DOTNET_ENVIRONMENT;
				var configBuilder = new ConfigurationBuilder()
					.SetBasePath(AppContext.BaseDirectory)
					.AddJsonFile("appsettings.json", true, true);
				if (!string.IsNullOrEmpty(environment.Value)) {
					configBuilder.AddJsonFile($"appsettings.{environment.Value}.json", true, true);
				}
				var configuration = configBuilder.AddEnvironmentVariables().Build();
				builder.ConfigureAppConfiguration(configurationBuilder => {
					configurationBuilder.AddConfiguration(configuration);
				});
				builder.ConfigureServices(services => {
					services.AddSingleton(environment);
					services.AddSingleton(new ProgramSetting(configuration));
					services.AddSingleton<IHostEnvironment, MyHostEnvironment>();
				});
			});
			return commandHost;
		}
	}
}