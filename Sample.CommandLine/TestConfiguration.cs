using Albatross.CommandLine;
using Albatross.Config;
using Microsoft.Extensions.Configuration;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine {
	[Verb<TestConfiguration>("test config", Description = "Verify the configuration settings")]
	public record class TestConfigurationOptions {
	}
	public class TestConfiguration: CommandHandler<TestConfigurationOptions> {
		private readonly SampleConfig sampleConfig;
		private readonly IConfiguration configuration;

		public TestConfiguration(SampleConfig sampleConfig, IConfiguration configuration, TestConfigurationOptions options) : base(options) {
			this.sampleConfig = sampleConfig;
			this.configuration = configuration;
		}

		public override async Task<int> InvokeAsync(CancellationToken cancellationToken) {
			await this.Writer.WriteLineAsync(configuration.GetRequiredConnectionString("db"));
			await this.Writer.WriteLineAsync($"SettingA: {this.sampleConfig.SettingA}");
			await this.Writer.WriteLineAsync($"GoogleApiEndPoint: {this.sampleConfig.GoogleApiEndPoint}");
			return 0;
		}
	}
}