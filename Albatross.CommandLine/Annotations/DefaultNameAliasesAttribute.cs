using System;

namespace Albatross.CommandLine.Annotations {
	/// <summary>
	/// Use this attribute to specify the default name and aliases of an Option class.  
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public class DefaultNameAliasesAttribute : Attribute {
		public DefaultNameAliasesAttribute(string name, params string[] aliases) { }
	}
}
