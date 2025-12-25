using System;

namespace Albatross.CommandLine {
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class VerbAttribute<THandler> : VerbAttribute where THandler : ICommandHandler {
		public VerbAttribute(string name) : base(name, typeof(THandler)) { }
	}

	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
	public class VerbAttribute<TOptions, THandler> : VerbAttribute where THandler : ICommandHandler {
		public VerbAttribute(string name) : base(name, typeof(THandler), typeof(TOptions)) { }
	}

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class VerbAttribute : Attribute {
		public VerbAttribute(string name) {
			Name = name;
			this.Handler = null;
		}

		protected VerbAttribute(string name, Type handler) {
			Name = name;
			this.Handler = handler;
		}

		protected VerbAttribute(string name, Type handler, Type? optionsClass) {
			Name = name;
			this.Handler = handler;
			this.OptionsClass = optionsClass;
		}

		public Type? Handler { get; }

		/// <summary>
		/// When targetting an option class, this property has no effect since the OptionsClass is the target class.  When targetting the assembly, this property is required.
		/// It allows a command to be created without creating a new Options Class.
		/// </summary>
		public Type? OptionsClass { get; }
		public Type? UseBaseOptionsClass { get; set; }
		public string Name { get; }
		public string? Description { get; set; }
		public string[] Alias { get; set; } = [];
	}
}