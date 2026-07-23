using Albatross.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using System;
using System.IO;
using System.Reflection;

namespace Albatross.CommandLine.Defaults {
	/// <summary>
	/// Extension methods for configuring CommandHost with default settings including file-based Serilog logging and configuration.
	/// </summary>
	public static class Extensions {
		/// <summary>
		/// The default Serilog output template used by the file sink.
		/// </summary>
		public const string DefaultOutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:sszzz} {SourceContext} {ThreadId} [{Level:w3}] {Message:lj}{NewLine}{Exception}";

		/// <summary>
		/// Configure the CommandHost with default configuration and file-based Serilog logging.
		/// The console is left free for the command's own output; log messages are written to a file under
		/// <see cref="IApplicationPath.LogRoot"/>.
		/// </summary>
		/// <param name="commandHost"></param>
		/// <returns></returns>
		public static CommandHost WithDefaults(this CommandHost commandHost, string? configDirectory = null, Action<IConfiguration, IServiceCollection>? configureServices = null)
			=> commandHost.WithConfig(configDirectory, configureServices).WithSerilog();

		/// <summary>
		/// Configure the CommandHost with file-based Serilog logging. Log messages are written to a daily rolling file
		/// under <see cref="IApplicationPath.LogRoot"/>; no console sink is attached, so standard output and standard
		/// error remain available for the command's own output.
		/// <para>
		/// The <see cref="IApplicationPath.LogRoot"/> of the registered <see cref="IApplicationPath"/> determines the log
		/// directory. If no <see cref="IApplicationPath"/> is registered, a <see cref="DefaultApplicationPath"/> is used as
		/// a fallback (logging to a <c>log</c> folder under the application base directory) so logging works with no extra
		/// setup.
		/// </para>
		/// <para>
		/// The code-configured defaults (minimum level <see cref="LogEventLevel.Information"/> and the file sink) can be
		/// overridden at deploy time without recompiling: a <c>Serilog</c> section in <c>appsettings.json</c> (loaded by
		/// <see cref="WithConfig"/>) is layered on top via <c>ReadFrom.Configuration</c>. For example, set
		/// <c>Serilog:MinimumLevel:Default</c> to <c>Debug</c> to raise verbosity, or add per-namespace overrides and
		/// extra sinks. Because <see cref="WithConfig"/> applies environment-specific files
		/// (<c>appsettings.{DOTNET_ENVIRONMENT}.json</c>), the level can differ per environment for free.
		/// </para>
		/// </summary>
		/// <param name="commandHost"></param>
		/// <returns></returns>
		public static CommandHost WithSerilog(this CommandHost commandHost) {
			commandHost.ConfigureHost(builder => {
				builder.UseSerilog((context, services, configuration) => {
					var applicationPath = services.GetService<IApplicationPath>() ?? new DefaultApplicationPath();
					var applicationName = Assembly.GetEntryAssembly()?.GetName().Name ?? "app";
					configuration
						.MinimumLevel.Information()
						.Enrich.FromLogContext()
						.Enrich.WithThreadId()
						// Baseline above (Information + FromLogContext) is set in code so logging works with zero
						// configuration.  A Serilog section in appsettings.json (loaded by WithConfig) is layered on
						// top here — it can raise/lower the level, add per-namespace overrides, and add enrichers or
						// sinks.  The file sink and its LogRoot-derived path are always added in code below, so
						// configuration never needs to know the log path.
						.ReadFrom.Configuration(context.Configuration)
					.WriteTo.File(
						Path.Combine(applicationPath.LogRoot, $"{applicationName}-.log"),
						rollingInterval: RollingInterval.Day,
						outputTemplate: DefaultOutputTemplate);

				});
			});
			return commandHost;
		}

		/// <summary>
		/// Configure the CommandHost with default configuration from appsettings.json and environment variables.
		/// This method will leverage the DOTNET_ENVIRONMENT environment variable to load environment specific configuration file.
		/// </summary>
		/// <param name="commandHost"></param>
		/// <param name="configDirectory"></param>
		/// <returns></returns>
		public static CommandHost WithConfig(this CommandHost commandHost, string? configDirectory = null, Action<IConfiguration, IServiceCollection>? configureServices = null) {
			commandHost.ConfigureHost(builder => {
				var environment = EnvironmentSetting.DOTNET_ENVIRONMENT;
				var configBuilder = new ConfigurationBuilder()
					.SetBasePath(configDirectory ?? AppContext.BaseDirectory)
					.AddJsonFile("appsettings.json", true, false);
				if (!string.IsNullOrEmpty(environment.Value)) {
					configBuilder.AddJsonFile($"appsettings.{environment.Value}.json", true, false);
				}
				var configuration = configBuilder.AddEnvironmentVariables().Build();
				builder.ConfigureAppConfiguration(configurationBuilder => {
					configurationBuilder.AddConfiguration(configuration);
				});
				builder.ConfigureServices(services => {
					services.AddSingleton(environment);
					services.AddSingleton(new ProgramSetting(configuration));
					services.AddSingleton<IHostEnvironment, MyHostEnvironment>();
					configureServices?.Invoke(configuration, services);
				});
			});
			return commandHost;
		}
	}
}
