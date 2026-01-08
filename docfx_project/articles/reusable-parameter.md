# Reusable Parameters

In `System.CommandLine`, you can encapsulate complex validation logic, default values, and descriptions into reusable `Option` and `Argument` classes. This allows you to maintain consistency and reduce boilerplate code across your commands.

## Creating a Reusable Parameter Class

To create a reusable parameter, you define a class that inherits from `System.CommandLine.Option<T>` or `System.CommandLine.Argument<T>`.

**Key Requirements:**

1.  **Constructor:** The class **must** have a constructor with the signature `(string name, params string[] aliases)`. This allows the source generator to correctly instantiate it.
2.  **Default Names (Optional):** You can use the `[DefaultNameAliases]` attribute to specify the default name and aliases for the option.

Here is an example of a custom `InputDirectoryOption` that validates if the specified directory exists:

```csharp
using Albatross.CommandLine;
using System.CommandLine;
using System.IO;

[DefaultNameAliases("--input-directory", "--in", "-i")]
public class InputDirectoryOption : Option<DirectoryInfo> {
	public InputDirectoryOption(string name, params string[] aliases) : base(name, aliases) {
		Description = "Specify an existing input directory";
		this.AddValidator(result => {
			if (result.GetValueForOption(this) is DirectoryInfo directory && !directory.Exists) {
				result.ErrorMessage = $"Input directory {directory.FullName} does not exist";
			}
		});
	}
}
```

### Reusable Arguments

The same pattern applies to `Argument` classes.

**Key Requirement:**

1.  **Constructor:** The class **must** have a constructor with the signature `(string name)`.

```csharp
using System.CommandLine;
using System.IO;

public class InputDirectoryArgument : Argument<DirectoryInfo> {
	public InputDirectoryArgument(string name) : base(name) {
		Description = "Specify an existing input directory";
		this.AddValidator(result => {
			if (result.GetValueForArgument(this) is DirectoryInfo directory && !directory.Exists) {
				result.ErrorMessage = $"Input directory {directory.FullName} does not exist";
			}
		});
	}
}
```

## Using Reusable Parameters

To use a reusable parameter in a command's `Parameters` class, decorate a property with the `[UseOption]` or `[UseArgument]` attribute.

### Using `[UseOption]`

When you use `[UseOption]`, the option's name is determined as follows:

-   **Default:** The names and aliases are taken from the `[DefaultNameAliases]` attribute on the reusable option class.
-   **Custom Name:** If you need to use the same option multiple times or want to avoid naming conflicts, set `UseCustomName = true`. The option name will then be derived from the property name (e.g., a property named `Directory2` will become the option `--directory2`).

```csharp
[Verb("backup")]
public record class BackUpParams {
	// The name of this option will be --input-directory (with aliases --in and -i)
	// as defined by the [DefaultNameAliases] attribute on InputDirectoryOption.
	[UseOption<InputDirectoryOption>]
	public required DirectoryInfo Directory1 { get; init; }

	// By setting UseCustomName = true, the option name becomes --directory2,
	// ignoring the names from [DefaultNameAliases].
	[UseOption<InputDirectoryOption>(UseCustomName = true)]
	public required DirectoryInfo Directory2 { get; init; }
}
```

### Using `[UseArgument]`

You can apply a reusable argument to a property using the `[UseArgument]` attribute. The argument's name is derived from the property name.

```csharp
[Verb("backup")]
public record class BackUpParams {
	[UseArgument<InputDirectoryArgument>]
	public required DirectoryInfo SourceDirectory { get; init; }
}
```

---

-   **See Also:** For more examples, check out the [Albatross.CommandLine.Inputs](https://github.com/RushuiGuan/commandline/blob/main/Albatross.CommandLine.Inputs/README.md) project, which provides a library of common reusable parameters.