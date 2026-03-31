using System.Threading.Tasks;
using Albatross.CommandLine;
using Albatross.CommandLine.Defaults;
using Albatross.Config;
using Albatross.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System.CommandLine;

namespace LoggingTest {
	internal class Program {
		static async Task<int> Main(string[] args) {
			await using var host = new CommandHost("LoggingTest")
				.RegisterServices(RegisterServices)
				.AddCommands()
				.Parse(args)
				.WithConfig()
				.ConfigureHost(builder => {
					builder.UseSerilog();
					builder.ConfigureLogging((context, logging) => {
						var setupSerilog = new SetupSerilog();
						setupSerilog.UseConfigFile(EnvironmentSetting.DOTNET_ENVIRONMENT.Value, null, null, true);
						setupSerilog.Create();
					});
				})
				.Build();
			return await host.InvokeAsync();
		}
		static void RegisterServices(ParseResult result, IServiceCollection services) {
			services.RegisterCommands();
			services.AddSingleton<MyService>();
		}
	}
}