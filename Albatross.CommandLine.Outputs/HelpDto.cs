namespace Albatross.CommandLine.Outputs {
	/// <summary>
	/// Machine-readable help for a single command and, recursively, every command beneath it.  Serialized as a bare
	/// JSON document rather than a <see cref="CommandOutput"/> envelope: help returns data and reports no outcome.
	/// </summary>
	public record class HelpDto {
		public required string Name { get; init; }

		/// <summary>
		/// The space-separated command path as it is typed, excluding the executable — "csharp generate".  Empty for
		/// the root command.
		/// </summary>
		public required string Path { get; init; }
		public IReadOnlyCollection<string> Aliases { get; init; } = [];
		public string? Description { get; init; }

		/// <summary>
		/// Hidden commands are included, unlike the default text help, so that a machine consumer receives the
		/// complete surface and decides for itself what to surface.
		/// </summary>
		public bool Hidden { get; init; }
		public IReadOnlyCollection<ArgumentDto> Arguments { get; init; } = [];

		/// <summary>
		/// Options declared on this command.  Options inherited from an ancestor (<see cref="OptionDto.Recursive"/>)
		/// appear only on the top node of the document — the command help was requested for — because
		/// System.CommandLine lists a recursive option solely on the command that declares it, and that command may
		/// sit above the emitted subtree.
		/// </summary>
		public IReadOnlyCollection<OptionDto> Options { get; init; } = [];
		public IReadOnlyCollection<HelpDto> Subcommands { get; init; } = [];
	}

	public record class OptionDto {
		public required string Name { get; init; }
		public IReadOnlyCollection<string> Aliases { get; init; } = [];
		public string? Description { get; init; }
		public bool Hidden { get; init; }
		public bool Required { get; init; }

		/// <summary>True when the option applies to the declaring command and all of its descendants.</summary>
		public bool Recursive { get; init; }

		/// <summary>The value type rendered for a reader — "string", "int", "string[]", "LogLevel".</summary>
		public string? ValueType { get; init; }

		/// <summary>The placeholder shown for the value in usage text, when the option customizes it.</summary>
		public string? HelpName { get; init; }

		/// <summary>Distinguishes "no default" from "the default is null", which <see cref="DefaultValue"/> alone cannot.</summary>
		public bool HasDefaultValue { get; init; }
		public object? DefaultValue { get; init; }

		/// <summary>The values accepted, when they form a closed set — the members of an enum, for instance.</summary>
		public IReadOnlyCollection<string> AllowedValues { get; init; } = [];
		public required ArityDto Arity { get; init; }
	}

	public record class ArgumentDto {
		public required string Name { get; init; }
		public string? Description { get; init; }
		public bool Hidden { get; init; }
		public string? ValueType { get; init; }
		public string? HelpName { get; init; }

		/// <summary>Distinguishes "no default" from "the default is null", which <see cref="DefaultValue"/> alone cannot.</summary>
		public bool HasDefaultValue { get; init; }
		public object? DefaultValue { get; init; }
		public IReadOnlyCollection<string> AllowedValues { get; init; } = [];

		/// <summary>An argument is required when <see cref="ArityDto.Minimum"/> is greater than zero.</summary>
		public required ArityDto Arity { get; init; }
	}

	/// <summary>
	/// How many values a symbol accepts.
	/// </summary>
	public record class ArityDto {
		public required int Minimum { get; init; }

		/// <summary>
		/// An unbounded arity reports 100000 rather than <see cref="int.MaxValue"/> - that is the sentinel
		/// System.CommandLine itself uses for ZeroOrMore and OneOrMore, and it is reported verbatim.
		/// </summary>
		public required int Maximum { get; init; }
	}
}
