using System;

namespace Albatross.CommandLine.Annotations {
	public class ArgumentAttribute : Attribute {
		public string? Description { get; set; }
		public bool Hidden { get; set; }
		public int ArityMin { get; set; }
		public int ArityMax { get; set; }
		/// <summary>
		/// When true, the code generator will generate a default value based on the initializer of the property
		/// </summary>
		public bool DefaultToInitializer { get; set; }
	}
}