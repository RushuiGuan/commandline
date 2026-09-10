using Albatross.CommandLine.Annotations;
using System.Collections;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace Albatross.CommandLine.Outputs {
	public class JsonHelpOption : Option<bool> {
		public JsonHelpOption(string name, params string[] aliases) : base("--json-help", "--jh") {
			Description = "Print help for this command and all of its subcommands as JSON";
			Arity = ArgumentArity.Zero;
			this.Action = new JsonHelpOptionAction();
		}


		public sealed class JsonHelpOptionAction : SynchronousCommandLineAction {
			/// <inheritdoc/>
			public override bool Terminating => true;
			public override bool ClearsParseErrors => true;

			public override int Invoke(ParseResult parseResult) {
				var command = parseResult.CommandResult.Command;
				var dto = this.Create(command, GetInheritedRecursiveOptions(command));
				return 0;
			}
		}
		HelpDto Create(Command command, IEnumerable<Option> inheritedOptions) => new HelpDto {
			Name = command.Name,
			Path = command.GetCommandKey(),
			Aliases = command.Aliases.ToArray(),
			Description = command.Description,
			Hidden = command.Hidden,
			Arguments = command.Arguments.Select(CreateArgument).ToArray(),
			Options = command.Options.Concat(inheritedOptions).Select(CreateOption).ToArray(),
			Subcommands = command.Subcommands.Select(x => Create(x, [])).ToArray(),
		};

		static readonly Dictionary<Type, string> TypeNames = new() {
			[typeof(string)] = "string",
			[typeof(bool)] = "bool",
			[typeof(char)] = "char",
			[typeof(byte)] = "byte",
			[typeof(sbyte)] = "sbyte",
			[typeof(short)] = "short",
			[typeof(ushort)] = "ushort",
			[typeof(int)] = "int",
			[typeof(uint)] = "uint",
			[typeof(long)] = "long",
			[typeof(ulong)] = "ulong",
			[typeof(float)] = "float",
			[typeof(double)] = "double",
			[typeof(decimal)] = "decimal",
			[typeof(object)] = "object",
		};


		/// <summary>
		/// Walks the ancestors outward and returns the options they apply recursively, ordered from the outermost
		/// ancestor inward so that the most global flags read first.
		/// </summary>
		protected virtual IEnumerable<Option> GetInheritedRecursiveOptions(Command command) {
			var list = new List<Option>();
			// The visited set mirrors GetCommandNames: a malformed tree would otherwise loop forever here, before
			// GetCommandKey gets the chance to report the cycle.
			var visited = new HashSet<Command> { command };
			for (var parent = command.Parents.FirstOrDefault() as Command; parent != null; parent = parent.Parents.FirstOrDefault() as Command) {
				if (!visited.Add(parent)) {
					throw new InvalidOperationException("Circular reference detected in command hierarchy.");
				}
				list.AddRange(parent.Options.Where(x => x.Recursive));
			}
			list.Reverse();
			return list;
		}

		protected virtual OptionDto CreateOption(Option option) => new OptionDto {
			Name = option.Name,
			Aliases = option.Aliases.ToArray(),
			Description = option.Description,
			Hidden = option.Hidden,
			Required = option.Required,
			Recursive = option.Recursive,
			ValueType = GetTypeName(option.ValueType),
			HelpName = option.HelpName,
			HasDefaultValue = option.HasDefaultValue,
			DefaultValue = option.HasDefaultValue ? option.GetDefaultValue() : null,
			AllowedValues = GetAllowedValues(option.ValueType),
			Arity = CreateArity(option.Arity),
		};

		protected virtual ArgumentDto CreateArgument(Argument argument) => new ArgumentDto {
			Name = argument.Name,
			Description = argument.Description,
			Hidden = argument.Hidden,
			ValueType = GetTypeName(argument.ValueType),
			HelpName = argument.HelpName,
			HasDefaultValue = argument.HasDefaultValue,
			DefaultValue = argument.HasDefaultValue ? argument.GetDefaultValue() : null,
			AllowedValues = GetAllowedValues(argument.ValueType),
			Arity = CreateArity(argument.Arity),
		};

		static ArityDto CreateArity(ArgumentArity arity) => new ArityDto {
			Minimum = arity.MinimumNumberOfValues,
			Maximum = arity.MaximumNumberOfValues,
		};

		/// <summary>
		/// Renders a value type the way a reader would write it - "string", "int", "LogLevel", "string[]" - rather
		/// than as a CLR type name.  Null for a flag such as --help, whose ValueType is void: its zero arity already
		/// says it takes no value, and "Void" would only invite a consumer to look for one.
		/// </summary>
		protected static string? GetTypeName(Type type) {
			if (type == typeof(void)) {
				return null;
			}
			var value = Nullable.GetUnderlyingType(type) ?? type;
			if (value.IsArray) {
				return $"{GetTypeName(value.GetElementType()!)}[]";
			}
			// string is IEnumerable but not generic, so the generic test keeps it out of the collection branch.
			if (value.IsGenericType && typeof(IEnumerable).IsAssignableFrom(value)) {
				var argument = value.GetGenericArguments().FirstOrDefault();
				if (argument != null) {
					return $"{GetTypeName(argument)}[]";
				}
			}
			return TypeNames.TryGetValue(value, out var name) ? name : value.Name;
		}

		/// <summary>
		/// Lists the members of an enum-valued symbol - the closed set of values it accepts - looking through
		/// Nullable and collection wrappers to the type a single token supplies.  Empty for every other type.
		/// </summary>
		protected static IReadOnlyCollection<string> GetAllowedValues(Type type) {
			var value = GetElementType(type);
			return value.IsEnum ? Enum.GetNames(value) : [];
		}

		static Type GetElementType(Type type) {
			var value = Nullable.GetUnderlyingType(type) ?? type;
			if (value.IsArray) {
				return GetElementType(value.GetElementType()!);
			}
			if (value.IsGenericType && typeof(IEnumerable).IsAssignableFrom(value)) {
				var argument = value.GetGenericArguments().FirstOrDefault();
				if (argument != null) {
					return GetElementType(argument);
				}
			}
			return value;
		}
	}
}
