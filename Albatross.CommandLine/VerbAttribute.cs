using System;

namespace Albatross.CommandLine {
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class VerbAttribute<THandler> : VerbAttribute where THandler : IAsyncCommandHandler {
		public VerbAttribute(string name) : base(name, typeof(THandler)) { }
	}

	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
	public class VerbAttribute<TParams, THandler> : VerbAttribute where THandler : IAsyncCommandHandler {
		public VerbAttribute(string name) : base(name, typeof(THandler), typeof(TParams)) { }
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

		protected VerbAttribute(string name, Type handler, Type? parametersClass) {
			Name = name;
			this.Handler = handler;
			this.ParamsClass = parametersClass;
		}

		public Type? Handler { get; }

		/// <summary>
		/// When targetting an option class, this property has no effect since the ParamsClass is the target class.  When targetting the assembly, this property is required.
		/// It allows a command to be created without creating a new Params Class.
		/// </summary>
		public Type? ParamsClass { get; }
		public Type? BaseParamsClass { get; set; }
		public string Name { get; }
		public string? Description { get; set; }
		public string[] Alias { get; set; } = [];
	}
}