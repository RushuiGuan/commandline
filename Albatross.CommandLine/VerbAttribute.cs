using System;

namespace Albatross.CommandLine {
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
	public class VerbAttribute : Attribute {
		public VerbAttribute(string name) {
			Name = name;
			this.Handler = null;
		}
		public VerbAttribute(string name, Type handler) {
			Name = name;
			this.Handler = handler;
		}
		public Type? Handler { get; }
		/// <summary>
		/// When targetting an option class, this property has no effect since the OptionsClass is the target class.  When targetting the assembly, this property is required.
		//  It allows a command to be created without creating a new Options Class.
		/// </summary>
		public Type? OptionsClass { get; set; }
		public string Name { get; }
		public string? Description { get; set; }
		public string[] Alias { get; set; } = new string[0];
		/// <summary>
		/// When true, the properties of the base class are included in the options.  
		/// When false, only the properties of the current class are included.
		/// </summary>
		public bool UseBaseClassProperties { get; set; } = true;
	}
}