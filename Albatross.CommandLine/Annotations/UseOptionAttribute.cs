using System;
using System.CommandLine;

namespace Albatross.CommandLine.Annotations {
	/// <summary>
	/// Marks a property to use a predefined option type with built-in validation and parsing logic.
	/// The generated code will instantiate and configure the specified option type.
	/// </summary>
	/// <typeparam name="TOption">The predefined option type to use.</typeparam>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public class UseOptionAttribute<TOption> : OptionAttribute where TOption : Option {
		/// <summary>
		/// if the aliases are specified, they will override the default aliases from TOption.  This is not additive but an override.  Do not specify aliases to use the default from TOption
		/// </summary>
		/// <param name="aliases"></param>
		public UseOptionAttribute(params string[] aliases) : base(aliases) { }
		/// <summary>
		/// if true, the generator will use the target property name as the option name instead of the default name of the TOption class
		/// this is useful when there is a naming conflict using the default name from TOption class
		/// </summary>
		public bool UseCustomName { get; set; }
	}
}