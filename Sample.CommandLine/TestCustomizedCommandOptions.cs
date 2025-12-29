using Albatross.CommandLine;

namespace Sample.CommandLine {
	[Verb<DefaultAsyncCommandHandler<TestCustomizedCommandOptions>>("test customized", Description = "Commands can be customized by extending its partial class")]
	public record class TestCustomizedCommandOptions {
		[Option("d")]
		public required string Description { get; init; }
	}
	public partial class TestCustomizedCommand {
		partial void Initialize() {
			this.Option_Description.Validators.Add(r =>{
				var text = r.GetRequiredValue(this.Option_Description);
				if (text.Length < 3) {
					r.AddError("Description must be at least 3 characters long.");
				}
			});
		}
	}
}
