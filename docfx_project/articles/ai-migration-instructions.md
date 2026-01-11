# Migrating from `Albatross.CommandLine` v7 to v8

The instructions are written for AI agents.  Please follow the exact steps below.  


1. Reference the latest version of the following nuget libraries:
	- `Albatross.CommandLine`
	- `Albatross.CommandLine.Inputs`
	- `Albatross.CommandLine.Defaults`
2. Go through the source code and make necessary changes based on [the instructions](#instructions-by-changes) below.
3. Once completed, compile the project and fix any symbol not found errors caused by missing namespaces.


#### Instructions by Changes
|Change|v7|v8|Actions|
|-|-|-|-|
|The postfix for Command Params|`Options`|`Params`|Rename all parameters classes by removing `Options` postfix and add `Params` postfix|
|namespace of the `VerbAttribute`|`Albatross.CommandLine`|`Albatross.CommandLine.Annotations`| Import namespace `Albatross.CommandLine.Annotations`  |
|namespace of the `OptionAttribute`|`Albatross.CommandLine`|`Albatross.CommandLine.Annotations`|  Import namespace `Albatross.CommandLine.Annotations` |
|namespace of the `ArgumentAttribute`|`Albatross.CommandLine`|`Albatross.CommandLine.Annotations`| Import namespace `Albatross.CommandLine.Annotations`  |
|Specify a CommandHandler for the `VerbAttribute`| Use the second constructor parameter| Use the first Generic Argument| Specify the command handler class using the Generic Argument.  See [VerbAttribute Change](#verbattribute-changes)|
|How parameters are injected to command handlers| Inject Microsoft.Extensions.Options.IOptions\<T>|Inject parameters class T directly|Remove the reference to `Microsoft.Extensions.Options` namespace, Change the constructor parameter `IOption\<T> options` to `T parameters`|
|`Albatross.CommandLine.BaseHandler` Class Constructor|Take a single parameter of type `Microsoft.Extensions.Options.IOptions<T>`|Has new signature (ParseResult, T).  T is the command's parameters class|Inject ParseResult to the derived classes and fix the base class constructor invocation.  See [Changes in BaseHandler Class Constructor](#changes-in-basehandler-class-constructor)|
|`BaseHandler` Class|Supports synchoronized invoke signature: `int Invoke(InvocationContext)`|Only supports async signature `Task<int> InvokeAsync(CancellationToken cancellationToken)`|Convert sync to async method.  See [Convert `Invoke` to `InvokeAsync` method](#convert-invoke-to-invokeasync-method)|
|`BaseHandler` Class field|`BaseHandler` has a protected `options` field with the type of `T` |The `options` field has been renamed to `parameters`|Rename any usage of the `options` field to `parameters` for any derived class of `BaseHandler<T>`|
|`BaseHandler` class `Writer` property|The `writer` property is all lower case|has been renamed to `Writer`|Rename any usage of `this.writer` to `this.Writer`|
|`BaseHandler.InvokeAsync` signature|`Task<int> InvokeAsync(InvocationContext context)`|`Task<int> InvokeAsync(ParseResult result)`|Change the signature.  `context` is likely not used in the body of the `InvokeAsync` method|
|Command Class initialization|use `IRequireInitialization` interface| use `partial void Initialize()` method|Move the implementation from the `Init` to the `Initialize` method and remove the interface|
|Params class Property Inspection|Has a default annotation of `Option` for any public properties.  Ignore a public property using `Albatross.CommandLine.IgnoreAttribute`|Has no default annotation of public properties.  No longer use ``Albatross.CommandLine.IgnoreAttribute``|Any property that doesnot have the `OptionAttribute`, `ArgumentAttribute` or `IgnoreAttribute` should be annotated with `Albatross.CommandLine.Annotation.OptionAttribute`.  Remove any usage of `IgnoreAttribute`|
|Program entry point: `Program.cs` file|Use a custom `Setup` class| Use generic `CommandHost`| Replace entry point method `Main` with [the entry point code](#entry-point-code) |
|Service Registration|Overwritten in the custom `Setup` class:`public void RegisterServices(InvocationContext, IConfiguration, EnvironmentSetting, IServiceCollection)`|Registration can be done as part of the `CommandHost`creation.  See [the entry point code](#entry-point-code) |Move the content of the registration to the static method `RegisterServices`.  It should have a signature of `void RegisterServices(ParseResult, IServiceCollection)`.|
|Use of `Format` parameter in Parameters classes|Many params class have an output format parameters: `[Option("f", ...)]public string? Format { get; set; }`| Use the prebuilt Format parameter: `[UseOption<FormatExpressionOption>]public IExpression? Format { get; init; }`|Change the `Format` parameter in the Params class and Change all usage of the `Format` parameter in the command handlers according the [sample code below](#format-parameter-usage)|


#### Entry Point Code
```csharp
static async Task<int> Main(string[] args) {
	await using var host = new CommandHost("Albatross Expression Utility")
		.RegisterServices(RegisterServices)
		.AddCommands()
		.Parse(args)
		.WithDefaults()
		.Build();
		return await host.InvokeAsync();
	}
	static void RegisterServices(ParseResult result, IServiceCollection services) {
		// Move your registration logic here
	}
```

#### Changes in BaseHandler Class Constructor
```csharp
// V7
public class List : BaseHandler<EvalOptions> {
		private readonly ExpressionConfig config;
		public List(IOptions<EvalOptions> options, ExpressionConfig config) : base(options) {
			this.config = config;
		}
}
// V8
public class List : BaseHandler<EvalParams> {
	ExpressionConfig config;
	public List(ParseResult result, EvalParams parameters, ExpressionConfig config) : base(result, parameters) {
		this.config = config;
	}
}
```

#### Convert `Invoke` to `InvokeAsync` method
```csharp
// V7
public override int Invoke(InvocationContext context) {
	// the body of the method could mostly remain the same
	...
	return 0;
}
//V8
public override Task<int> InvokeAsync(CancellationToken cancellationToken) {
	// copy body of the Invoke method here
	...
	return Task.FromResult(0);
}
```

#### VerbAttribute Changes
```csharp
// V7
[Verb("csharp-proxy", typeof(CSharpWebClientCodeGenCommandHandler), Description = "Generate CSharp Http Proxy class")]
public record class CodeGenCommandOptions {
	[Option("p")]
	public FileInfo ProjectFile { get; set; } = null!;
}
// V8
[Verb<CSharpWebClientCodeGenCommandHandler>("csharp-proxy", Description = "Generate CSharp Http Proxy class")]
public record class CodeGenCommandOptions {
	[Option("p")]
	public FileInfo ProjectFile { get; set; } = null!;
}
```

#### Format Parameter Usage
```csharp
// V7
[Verb("instruments", typeof(GetInstruments))]
public class GetInstrumentsOptions {
	[Option("format", Description = "Format of the output data")]
	public string? Format { get; set; }
}
public class GetInstruments : BaseHandler<GetInstrumentsOptions> {
	public GetInstrument() : base(options) {
	}
	public override async Task<int> InvokeAsync(InvocationContext context) {
		var instruments = await ...	// logic to get instruments here
		this.writer.CliPrint(instruments, options.Format);
		return 0;
	}
}
// V8
using Albatross.Expression.Nodes;
using Albatross.CommandLine.Inputs;
[Verb<GetInstruments>("instruments")]
public class GetInstrumentsParams {
	[UseOption<FormatExpressionOption>]
	public IExpression? Format { get; init; }
}
public class GetInstruments : BaseHandler<GetInstrumentsParams> {
	public GetInstrument() : base(options) {
	}
	public override async Task<int> InvokeAsync(CancellationToken cancellationToken) {
		var instruments = await ...	// logic to get instruments here
		// Use extension method CliPrintWithExpression instead
		this.Writer.CliPrintWithExpression(instruments, options.Format);
		return 0;
	}
}
```