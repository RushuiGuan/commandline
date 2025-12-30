using Albatross.CommandLine;
using Albatross.CommandLine.Annotations;
using Albatross.Config;
using Microsoft.Extensions.Configuration;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine {
	[Verb<TestConfiguration>("test config", Description = "Verify the configuration settings")]
	public record class TestConfigurationParams {
	}

	public class TestConfiguration : BaseHandler<TestConfigurationParams> {
		private readonly SampleConfig sampleConfig;
		private readonly IConfiguration configuration;

		public TestConfiguration(SampleConfig sampleConfig, IConfiguration configuration, ParseResult result, TestConfigurationParams parameters) : base(result, parameters) {
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