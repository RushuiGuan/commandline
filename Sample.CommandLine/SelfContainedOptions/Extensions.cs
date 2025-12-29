using Albatross.CommandLine;
using Microsoft.Extensions.DependencyInjection;

namespace Sample.CommandLine.SelfContainedParams {
	public static class Extensions {
		public static IServiceCollection AddInstrumentOption(this IServiceCollection services) {
			services.AddSingleton<InstrumentProxy>();
			services.AddScoped<IAsyncCommandParameterHandler<InstrumentOption>, InstrumentOptionHandler>();
			return services;
		}
	}
}