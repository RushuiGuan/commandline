using Albatross.CommandLine.Annotations;
using System;
using System.CommandLine;

namespace Albatross.CommandLine.Inputs {
	[DefaultNameAliases("time-zone", "tz")]
	public class TimeZoneOption : Option<string> {
		public TimeZoneOption(string name, params string[] aliases) : base(name, aliases) {
			this.Description = "Time zone identifier (Windows or IANA format, e.g., 'Eastern Standard Time' or 'America/New_York').";
			this.DefaultValueFactory = _ => TimeZoneInfo.Local.StandardName;
			this.Validators.Add(result => {
				var value = result.GetValueOrDefault<string>();
				if (value != null) {
					if (!TimeZoneInfo.TryFindSystemTimeZoneById(value, out _)) {
						result.AddError($"Time zone '{value}' is invalid.");
					}
				}
			});
		}
	}
}
