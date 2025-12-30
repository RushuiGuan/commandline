using System;

namespace Albatross.CommandLine.Annotations {
	public class OptionAttribute : Attribute {
		public OptionAttribute(params string[] alias) {
			this.Alias = alias;
		}
		public string? Description { get; set; }
		public string[] Alias { get; }
		public bool Hidden { get; set; }
		public bool Required { get; set; }
		/// <summary>
		/// When true, the code generator will generate a default value based on the initializer of the property
		/// This property has no effect when used with predefined option type
		/// </summary>
		public bool DefaultToInitializer { get; set; }
	}
}