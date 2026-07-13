using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.CommandLine;
using System.Runtime.CompilerServices;
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
		readonly IHostBuilder hostBuilder;
		ParseResult? parseResult;
		IHost? host;
		private IServiceScope? scope;

		private Action<ParseResult, IServiceProvider> configApplication = (result, provider) => {
			var logger = provider.GetRequiredService<ILogger<CommandHost>>();
			logger.LogInformation("----------- {app} -----------", result.RootCommandResult.Command.Description);
		};

		/// <summary>
		/// Gets the parse result. Throws if <see cref="Parse"/> has not been called.
		/// </summary>
		public ParseResult RequiredResult => this.parseResult ?? throw new InvalidOperationException("Parse(args) has not been called yet");

		/// <summary>
		/// Gets the command builder used to configure the command hierarchy.
		/// </summary>
		public CommandBuilder CommandBuilder { get; }

		/// <summary>
		/// The grace period the command handler is given to cancel and clean up after Ctrl+C
		/// (SIGINT/SIGTERM) before the process is force-terminated. Defaults to 30 seconds; the
		/// System.CommandLine default of 2 seconds is too short for handlers doing real I/O.
		/// Set this before calling <see cref="Build"/>.
		/// </summary>
		public TimeSpan ProcessTerminationTimeout { get; set; } = TimeSpan.FromSeconds(30);

		/// <summary>
		/// Gets the scoped service provider. Throws if <see cref="Build"/> has not been called.
		/// </summary>
		/// <returns>The scoped service provider.</returns>
		public IServiceProvider GetServiceProvider() => scope?.ServiceProvider ?? throw new InvalidOperationException($"Host has not been built, Call the Build() method!");

		/// <summary>
		/// Creates a new command host with the specified description for the root command.
		/// </summary>
		/// <param name="description">The description to display in help text for the root command.</param>
		public CommandHost(string description) {
			this.hostBuilder = Host.CreateDefaultBuilder();
			CommandBuilder = new CommandBuilder(description);
		}

		/// <summary>
		/// Command Hierarchy should be built before calling Parse
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		public CommandHost Parse(string[] args) => Parse(args, true);

		public CommandHost Parse(string[] args, bool withLogging) {
			this.CommandBuilder.BuildTree(GetServiceProvider);
			parseResult = this.CommandBuilder.RootCommand.Parse(args);
			this.hostBuilder.ConfigureServices(services => {
				services.AddSingleton(parseResult);
				services.AddSingleton<ICommandContext, CommandContext>();
			});
			//if (withLogging) {
			//	// configure default logging level based on parsed result
			//	var verbosityOption = parseResult.GetVerbosityOption();
			//	if (verbosityOption != null) {
			//		var logLevel = verbosityOption.GetLogLevel(parseResult);
			//		this.hostBuilder.ConfigureLogging((_, builder) => builder.SetMinimumLevel(logLevel));
			//	}
			//} else {
			//	this.hostBuilder.ConfigureLogging((_, builder) => builder.ClearProviders());
			//}
			return this;
		}

		/// <summary>
		/// Configures the underlying host builder.
		/// </summary>
		/// <param name="configure">An action to configure the host builder.</param>
		/// <returns>The command host for method chaining.</returns>
		public CommandHost ConfigureHost(Action<IHostBuilder> configure) {
			configure(this.hostBuilder);
			return this;
		}

		/// <summary>
		/// Configures the underlying host builder with access to the parse result.
		/// Must be called after <see cref="Parse"/>.
		/// </summary>
		/// <param name="configure">An action to configure the host builder with the parse result.</param>
		/// <returns>The command host for method chaining.</returns>
		public CommandHost ConfigureHost(Action<ParseResult, IHostBuilder> configure) {
			configure(this.RequiredResult, this.hostBuilder);
			return this;
		}

		/// <summary>
		/// Registers services with the dependency injection container.
		/// Must be called after <see cref="Parse"/>.
		/// </summary>
		/// <param name="action">An action to register services with access to the parse result.</param>
		/// <returns>The command host for method chaining.</returns>
		public CommandHost RegisterServices(Action<ParseResult, IServiceCollection> action) {
			this.hostBuilder.ConfigureServices(services => action(this.RequiredResult, services));
			return this;
		}

		/// <summary>
		/// Registers an action to configure the application after the host is built.
		/// </summary>
		/// <param name="action">An action to configure the application with access to the parse result and service provider.</param>
		/// <returns>The command host for method chaining.</returns>
		public CommandHost ConfigureApplication(Action<ParseResult, IServiceProvider> action) {
			configApplication += action;
			return this;
		}

		/// <summary>
		/// Builds the host and creates a service scope for command execution.
		/// </summary>
		/// <returns>The command host for method chaining.</returns>
		public CommandHost Build() {
			this.host = this.hostBuilder.Build();
			this.scope = this.host.Services.CreateScope();
			this.configApplication(this.RequiredResult, host.Services);
			// System.CommandLine gives the command handler a grace period after Ctrl+C (SIGINT/SIGTERM):
			// it cancels the token, waits up to ProcessTerminationTimeout for the handler to unwind, then
			// force-terminates the process. The default is only 2 seconds, which is too short for handlers
			// doing real I/O (e.g. an HTTP call with a longer timeout) to cancel and clean up gracefully.
			// Must be set here, before InvokeAsync() runs: the pipeline reads this value and builds its
			// termination handler before invoking the command action, so setting it later has no effect.
			this.RequiredResult.InvocationConfiguration.ProcessTerminationTimeout = this.ProcessTerminationTimeout;
			return this;
		}

		/// <summary>
		/// Invokes the parsed command asynchronously.
		/// </summary>
		/// <returns>A task that represents the asynchronous operation, with the exit code as the result.</returns>
		public Task<int> InvokeAsync() => this.RequiredResult.InvokeAsync();

		/// <inheritdoc/>
		public async ValueTask DisposeAsync() {
			if (host != null) {
				host.Services.GetRequiredService<ILogger<CommandHost>>().LogInformation("Disposing");
			}
			if (host is IAsyncDisposable hostAsyncDisposable) {
				await hostAsyncDisposable.DisposeAsync();
			} else if (host != null) {
				host.Dispose();
			}
		}
	}
}