using Albatross.CommandLine.Experimental;
using Microsoft.Extensions.DependencyInjection;

namespace Sample.CommandLine.SelfContainedOptions {
	public static class Extensions {
		public static IServiceCollection AddInstrumentOption(this IServiceCollection services) {
			services.AddSingleton<InstrumentProxy>();
			services.AddScoped<IAsyncArgumentHandler<InstrumentOption>, InstrumentOptionHandler>();
			return services;
		}
	}
}