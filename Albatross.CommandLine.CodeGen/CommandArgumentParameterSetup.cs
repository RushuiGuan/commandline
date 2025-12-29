using Albatross.CodeAnalysis;
using Humanizer;
using Microsoft.CodeAnalysis;

namespace Albatross.CommandLine.CodeGen {
	public record class CommandArgumentParameterSetup : CommandParameterSetup {
		public override string CommandPropertyName => $"Argument_{this.PropertySymbol.Name}";
		public override INamedTypeSymbol DefaultParameterClass { get; }
		
		public int ArityMin {get; }
		public int ArityMax { get; }
		
		public CommandArgumentParameterSetup(Compilation compilation, IPropertySymbol propertySymbol, AttributeData argumentPropertyAttribute) 
			: base(propertySymbol, argumentPropertyAttribute) {
			if(propertySymbol.Type.IsCollection(compilation)) {
				this.ArityMin = 0;
				this.ArityMax = 100_000;
			} else {
				if (propertySymbol.Type.IsNullable(compilation)) {
					this.ArityMin = 0;
				} else {
					this.ArityMin = 1;
				}
				this.ArityMax = 1;
			}
			if(argumentPropertyAttribute.TryGetNamedArgument("ArityMin", out TypedConstant result)) {
				this.ArityMin = (int)result.Value!;
			}
			if(argumentPropertyAttribute.TryGetNamedArgument("ArityMax", out result)) {
				this.ArityMax = (int)result.Value!;
			}
			this.DefaultParameterClass = compilation.ArgumentGenericClass().Construct(propertySymbol.Type);
		}
	}
}