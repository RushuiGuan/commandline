using System;

namespace Albatross.CommandLine.Annotations {
	/// <summary>
	/// Marks a property as a command-line option. Options are named values prefixed with -- or -.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public class OptionAttribute : Attribute {
		/// <summary>
		/// Creates a new option attribute with the specified aliases.
		/// </summary>
		/// <param name="alias">The aliases for this option (e.g., "--name", "-n").</param>
		public OptionAttribute(params string[] alias) {
			this.Alias = alias;
		}
		/// <summary>
		/// Gets or sets the description displayed in help text.
		/// </summary>
		public string? Description { get; set; }
		/// <summary>
		/// Gets the aliases for this option.
		/// </summary>
		public string[] Alias { get; }
		/// <summary>
		/// Gets or sets whether the option is hidden from help text.
		/// </summary>
		public bool Hidden { get; set; }
		/// <summary>
		/// Gets or sets whether the option is required.
		/// </summary>
		public bool Required { get; set; }
		/// <summary>
		/// When true, the code generator will generate a default value based on the initializer of the property.
		/// This property has no effect when used with predefined option type.
		/// </summary>
		public bool DefaultToInitializer { get; set; }
		/// <summary>
		/// Gets or sets whether multiple arguments can be provided in a single token (e.g., -abc instead of -a -b -c).
		/// </summary>
		public bool AllowMultipleArgumentsPerToken { get; set; }
	}
}