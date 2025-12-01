using Albatross.CodeAnalysis.Symbols;
using Humanizer;
using Microsoft.CodeAnalysis;

namespace Albatross.CommandLine.CodeGen {
	public class CommandArgumentPropertySetup : CommandPropertySetup {
		public CommandArgumentPropertySetup(Compilation compilation, IPropertySymbol property, AttributeData argumentAttribute) : base(property, argumentAttribute, property.Name.Kebaberize()) {
			if(property.Type.IsCollection(compilation)) {
				this.ArityMin = 0;
				this.ArityMax = 100_000;
			} else {
				if (property.Type.IsNullable(compilation)) {
					this.ArityMin = 0;
				} else {
					this.ArityMin = 1;
				}
				this.ArityMax = 1;
			}
			if(argumentAttribute.TryGetNamedArgument("ArityMin", out TypedConstant result)) {
				this.ArityMin = (int)result.Value!;
			}
			if(argumentAttribute.TryGetNamedArgument("ArityMax", out result)) {
				this.ArityMax = (int)result.Value!;
			}
		}

		public override string CommandPropertyName => $"Argument_{this.Property.Name}";
		public int ArityMin {get; }
		public int ArityMax { get; }
		public bool FixedArity => ArityMin == ArityMax;
	}
}