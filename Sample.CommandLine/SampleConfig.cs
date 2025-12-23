using Albatross.Config;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;

namespace Sample.CommandLine {
	public class SampleConfig : ConfigBase{
		public SampleConfig(IConfiguration configuration) : base(configuration, "sample") {
			this.GoogleApiEndPoint = configuration.GetRequiredEndPoint("google-api");
		}

		[Required]
		public string SettingA { get; init; } = string.Empty;
		public string GoogleApiEndPoint { get;  }
	}
}