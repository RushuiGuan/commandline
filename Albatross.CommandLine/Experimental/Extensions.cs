using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Albatross.CommandLine.Experimental {
	public static class Extensions {
		public static void AddSharedOption<T>(this Command command, Func<IServiceProvider> serviceLocator) where T : Option, new() {
			var option = new T();
			Func<ParseResult, CancellationToken, Task> handler = async (parseResult, cancellationToken) => {
				var serviceProvider = serviceLocator();
				var service = serviceProvider.GetRequiredService<IAsyncArgumentHandler<T>>();
				await service.InvokeAsync(option, parseResult,cancellationToken);
			};
			option.Action = new AsyncActionHandler<T>(option, serviceLocator, handler);
			command.Add(option);
		}
	}
}