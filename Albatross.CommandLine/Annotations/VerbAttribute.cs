using System;

namespace Albatross.CommandLine.Annotations {
	/// <summary>
	/// Defines a command verb with a strongly-typed handler. Apply to a parameters class to define a command.
	/// </summary>
	/// <typeparam name="THandler">The handler type that executes when this command is invoked.</typeparam>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class VerbAttribute<THandler> : VerbAttribute where THandler : IAsyncCommandHandler {
		/// <summary>
		/// Creates a new verb attribute with the specified command name.
		/// </summary>
		/// <param name="name">The space-separated command name (e.g., "config set" for a subcommand).</param>
		public VerbAttribute(string name) : base(name, typeof(THandler)) { }
	}

	/// <summary>
	/// Defines a command verb at the assembly level, allowing command creation without a dedicated parameters class.
	/// </summary>
	/// <typeparam name="TParams">The parameters class type containing command options and arguments.</typeparam>
	/// <typeparam name="THandler">The handler type that executes when this command is invoked.</typeparam>
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
	public class VerbAttribute<TParams, THandler> : VerbAttribute where THandler : IAsyncCommandHandler {
		/// <summary>
		/// Creates a new verb attribute with the specified command name.
		/// </summary>
		/// <param name="name">The space-separated command name (e.g., "config set" for a subcommand).</param>
		public VerbAttribute(string name) : base(name, typeof(THandler), typeof(TParams)) { }
	}

	/// <summary>
	/// Base attribute for defining command verbs. Use the generic versions for type-safe handler specification.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class VerbAttribute : Attribute {
		/// <summary>
		/// Creates a new verb attribute with the specified command name and no handler.
		/// </summary>
		/// <param name="name">The space-separated command name.</param>
		public VerbAttribute(string name) {
			Name = name;
			this.Handler = null;
		}

		/// <summary>
		/// Creates a new verb attribute with the specified command name and handler type.
		/// </summary>
		protected VerbAttribute(string name, Type handler) {
			Name = name;
			this.Handler = handler;
		}

		/// <summary>
		/// Creates a new verb attribute with the specified command name, handler type, and parameters class.
		/// </summary>
		protected VerbAttribute(string name, Type handler, Type? parametersClass) {
			Name = name;
			this.Handler = handler;
			this.ParamsClass = parametersClass;
		}

		/// <summary>
		/// Gets the handler type for this command.
		/// </summary>
		public Type? Handler { get; }

		/// <summary>
		/// Gets or sets the parameters class type. When targeting a class, the attributed class is used.
		/// When targeting an assembly, this property must be specified.
		/// </summary>
		public Type? ParamsClass { get; }
		/// <summary>
		/// Gets or sets a base parameters class that the target class must derive from.
		/// </summary>
		// TODO: need a ERROR if the target class does not derive from this class
		public Type? BaseParamsClass { get; set; }
		/// <summary>
		/// Gets the space-separated command name defining its position in the command hierarchy.
		/// </summary>
		public string Name { get; }
		/// <summary>
		/// Gets or sets the description displayed in help text.
		/// </summary>
		public string? Description { get; set; }
		/// <summary>
		/// Gets or sets additional command aliases.
		/// </summary>
		public string[] Alias { get; set; } = [];
	}
}