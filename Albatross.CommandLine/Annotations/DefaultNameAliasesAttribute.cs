using System;

namespace Albatross.CommandLine.Annotations {
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public class DefaultNameAliasesAttribute : Attribute {
		public DefaultNameAliasesAttribute(string name, params string[] aliases) { }
	}
}
