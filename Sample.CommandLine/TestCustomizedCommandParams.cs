using Albatross.CommandLine;
using Albatross.CommandLine.Annotations;

namespace Sample.CommandLine {
	[Verb<DefaultAsyncCommandHandler<TestCustomizedCommandParams>>("test customized", Description = "Commands can be customized by extending its partial class")]
	public record class TestCustomizedCommandParams {
		[Option("d")]
		public required string Description { get; init; }
	}
	public partial class TestCustomizedCommandCommand {
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
