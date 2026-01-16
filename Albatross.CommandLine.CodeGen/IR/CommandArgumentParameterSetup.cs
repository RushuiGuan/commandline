using Albatross.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Albatross.CommandLine.CodeGen.IR {
	public record class CommandArgumentParameterSetup : CommandParameterSetup {
		public override string CommandPropertyName => $"Argument_{this.PropertySymbol.Name}";
		public override INamedTypeSymbol DefaultParameterClass { get; }

		public int ArityMin { get; }
		public int ArityMax { get; }

		/// <summary>
		/// this is defined in System.CommandLine, weirdly the same value as v7 max arity.
		/// </summary>
		private const int MaximumArity = 100_000;

		public CommandArgumentParameterSetup(Compilation compilation, IPropertySymbol propertySymbol, AttributeData argumentPropertyAttribute)
			: base(compilation, propertySymbol, argumentPropertyAttribute) {
			if (propertySymbol.Type.IsCollection(compilation)) {
				this.ArityMin = 0;
				this.ArityMax = MaximumArity;
			} else {
				if (propertySymbol.Type.IsNullable(compilation) || this.DefaultToInitializer) {
					this.ArityMin = 0;
				} else {
					this.ArityMin = 1;
				}
				this.ArityMax = 1;
			}
			if (argumentPropertyAttribute.TryGetNamedArgument("ArityMin", out TypedConstant result)) {
				this.ArityMin = (int)result.Value!;
			}
			if (argumentPropertyAttribute.TryGetNamedArgument("ArityMax", out result)) {
				this.ArityMax = (int)result.Value!;
			}
			this.DefaultParameterClass = compilation.ArgumentGenericClass().Construct(propertySymbol.Type);
		}
	}
}