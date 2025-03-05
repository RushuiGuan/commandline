using System;

namespace Albatross.CommandLine {
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
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