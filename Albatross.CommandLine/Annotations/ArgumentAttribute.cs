using System;

namespace Albatross.CommandLine.Annotations {
	/// <summary>
	/// Marks a property as a command-line argument. Arguments are positional values that do not require a name prefix.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public class ArgumentAttribute : Attribute {
		/// <summary>
		/// Gets or sets the description displayed in help text.
		/// </summary>
		public string? Description { get; set; }
		/// <summary>
		/// Gets or sets whether the argument is hidden from help text.
		/// </summary>
		public bool Hidden { get; set; }
		/// <summary>
		/// Gets or sets the minimum number of values this argument accepts.
		/// </summary>
		public int ArityMin { get; set; }
		/// <summary>
		/// Gets or sets the maximum number of values this argument accepts.
		/// </summary>
		public int ArityMax { get; set; }
		/// <summary>
		/// When true, the code generator will generate a default value based on the initializer of the property.
		/// </summary>
		public bool DefaultToInitializer { get; set; }
	}
}