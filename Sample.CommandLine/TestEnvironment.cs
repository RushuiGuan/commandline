using Albatross.CommandLine;
using Albatross.Config;
using Microsoft.Extensions.Hosting;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine {
	[Verb<TestEnvironment>("test environment", Description = "Verify the current host environment")]
	public record class TestEnvironmentOptions {
	}

	public class TestEnvironment : BaseHandler<TestEnvironmentOptions> {
		private readonly ProgramSetting programSetting;
		private readonly EnvironmentSetting environmentSetting;
		private readonly IHostEnvironment hostEnvironment;

		public TestEnvironment(ProgramSetting programSetting, EnvironmentSetting environmentSetting, IHostEnvironment hostEnvironment, ParseResult result, TestEnvironmentOptions options) : base(result, options) {
			this.programSetting = programSetting;
			this.environmentSetting = environmentSetting;
			this.hostEnvironment = hostEnvironment;
		}

		public override async Task<int> InvokeAsync(CancellationToken cancellationToken) {
			await this.Writer.WriteLineAsync($"App: {this.programSetting.App}; Group: {this.programSetting.Group}");
			await this.Writer.WriteLineAsync($"Environment: {this.environmentSetting.HostName}; IsProd: {this.environmentSetting.IsProd}; Environment: {this.environmentSetting.Value}");
			await this.Writer.WriteLineAsync($"IHostEnvironment: {this.hostEnvironment.EnvironmentName}; App: {this.hostEnvironment.ApplicationName}");
			return 0;
		}
	}
}