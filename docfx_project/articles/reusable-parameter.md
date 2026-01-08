# Reusable Parameter (Singular)

## Creating a Reusable Parameter Class
`Option` and `Argument` classes can be created with complexity.  `Albatross.CommandLine` provides a way to reuse these custom classes. 

**For the code generator to use the reusable parameter, the option has to have a constructor signature of `(string name, params string[] aliases)` and the argument has to have a constructor signature of `(string name)`**.

Here is an sample custom Option class with some validation logic.  

```csharp
[DefaultNameAliases("--input-directory", "--in", "-i")]
public class InputDirectoryOption : Option<DirectoryInfo> {
	public InputDirectoryOption(string name, params string[] aliases) : base(name, aliases) {
		Description = "Specify an existing input directory";
		this.Validators.Add(result => {
			var directory = result.GetValue(this);
			if (directory != null && !directory.Exists) {
				result.AddError($"Input directory {directory.FullName} doesn't exist");
			}
		});
	}
}
```
Use the `DefaultNameAliasesAttribute` to annotate the default name and aliases of the option.  This attribute has no effect on Arguments.

## Usage
To use this option in a command, use the `UseOptionAttribute`.  By default, names and aliases comes from the `DefaultNameAliasesAttribute` of the custom option class.  To avoid naming conflicts, set the `UseCustomName` flag to true.
```csharp
[Verb("backup")]
public record class BackUpParams {
	// the name of Directory1 is --input-directory
	[UseOption<InputDirectoryOption>]
	public required DirectoryInfo Directory1 { get; init; }

	// set the UseCustomName flag to true so that Directory2 will have its own name: --directory2
	[UseOption<InputDirectoryOption>(UseCustomName = true)]
	public required DirectoryInfo Directory2 { get; init; }
}
```

Same thing can be done with Arguments
```csharp
public class InputDirectoryArgument : Argument<DirectoryInfo> {
	public InputDirectoryArgument(string name, params string[] aliases) : base(name, aliases) {
		Description = "Specify an existing input directory";
		this.Validators.Add(result => {
			var directory = result.GetValue(this);
			if (directory != null && !directory.Exists) {
				result.AddError($"Input directory {directory.FullName} doesn't exist");
			}
		});
	}
}
[Verb("backup")]
public record class BackUpParams {
	[UseArgument<InputDirectoryOption>]
	public required DirectoryInfo Directory1 { get; init; }
}
```

- See also: [Albatross.CommandLine.Inputs](https://github.com/RushuiGuan/commandline/blob/main/Albatross.CommandLine.Inputs/README.md)