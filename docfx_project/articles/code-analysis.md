# Code Analysis

The `Albatross.CommandLine.CodeAnalysis` package is a Roslyn analyzer companion for Albatross.CommandLine. It catches common misuse of the `[Verb]`, `[Option]`, `[Argument]`, and `[OptionHandler]` attributes at compile time, before they manifest as runtime errors or confusing generated-code compiler errors.

## Installation

Reference the package as a development-only analyzer — it does not add any runtime dependency:

```xml
<PackageReference Include="Albatross.CommandLine.CodeAnalysis" Version="*">
  <PrivateAssets>all</PrivateAssets>
</PackageReference>
```

Or when using project references (e.g., in this repository):

```xml
<ProjectReference Include="..\Albatross.CommandLine.CodeAnalysis\Albatross.CommandLine.CodeAnalysis.csproj">
  <PrivateAssets>all</PrivateAssets>
  <ReferenceOutputAssembly>false</ReferenceOutputAssembly>
  <OutputItemType>Analyzer</OutputItemType>
</ProjectReference>
```

## Diagnostics Overview

| ID | Severity | Title |
|----|----------|-------|
| [ACL0001](#acl0001-duplicate-option-name) | Warning | Duplicate option name (case-insensitive) |
| [ACL0002](#acl0002-baseparamsclass-inheritance) | Warning | Params class does not derive from `VerbAttribute.BaseParamsClass` |
| [ACL0003](#acl0003-optionhandler-type-mismatch) | Error | `OptionHandlerAttribute` TOption is not assignable from the attributed class |
| [ACL0004](#acl0004-conflicting-option-and-argument) | Warning | Property has both `[Option]` and `[Argument]` |

---

## ACL0001: Duplicate Option Name

**Severity:** Warning

**Description:** Two or more public properties on a `[Verb]` params class have names that are identical when compared case-insensitively. Because option names are derived from property names via kebab-case conversion (e.g., `MyValue` → `--my-value`), properties like `Name` and `name` both produce `--name`, creating a duplicate CLI option.

### Example

```csharp
// ❌ ACL0001 — 'Name' and 'name' both produce --name
[Verb<MyHandler>("greet")]
public class GreetParams {
    [Option] public string? Name { get; set; }
    [Option] public string? name { get; set; }  // warning here
}
```

### Fix

Use distinct property names:

```csharp
// ✅ Correct
[Verb<MyHandler>("greet")]
public class GreetParams {
    [Option] public string? Name { get; set; }
    [Option] public string? Alias { get; set; }
}
```

---

## ACL0002: BaseParamsClass Inheritance

**Severity:** Warning

**Description:** When `VerbAttribute.BaseParamsClass` is set, the params class must derive from the specified type. This applies to both class-targeted (`[Verb<THandler>]`) and assembly-targeted (`[Verb<TParams, THandler>]`) usages.

The code generator uses `BaseParamsClass` to group related commands under a shared DI registration. If the params class does not derive from it, the generated switch expression will be missing entries.

### Example

```csharp
public class BaseParams { }
public class UnrelatedParams { }

// ❌ ACL0002 — GreetParams does not derive from BaseParams
[Verb<MyHandler>("greet", BaseParamsClass = typeof(BaseParams))]
public class GreetParams { }
```

### Fix

Ensure the params class inherits from the specified base class:

```csharp
// ✅ Correct
[Verb<MyHandler>("greet", BaseParamsClass = typeof(BaseParams))]
public class GreetParams : BaseParams { }
```

---

## ACL0003: OptionHandler Type Mismatch

**Severity:** Error

**Description:** `OptionHandlerAttribute<TOption, THandler>` and `OptionHandlerAttribute<TOption, THandler, TContextValue>` must be applied to a class that is `TOption` itself or a subclass of it.

The code generator uses `TOption` to wire up the option handler. If the attributed class is not assignable to `TOption`, the generated code will not compile.

### Example

```csharp
// ❌ ACL0003 — MyOption is not assignable to Option<int>
[OptionHandler<Option<int>, MyHandler>]
public class MyOption : Option<string> { ... }
```

### Fix

Ensure `TOption` matches the attributed class or its base:

```csharp
// ✅ Correct — MyOption is assignable to Option<string>
[OptionHandler<Option<string>, MyHandler>]
public class MyOption : Option<string> { ... }

// ✅ Also correct — attributed class IS the TOption
[OptionHandler<MyOption, MyHandler>]
public class MyOption : Option<string> { ... }
```

---

## ACL0004: Conflicting Option and Argument

**Severity:** Warning

**Description:** A property cannot be both a named option and a positional argument. The code generator only processes one attribute per property, so having both is always a mistake.

### Example

```csharp
// ❌ ACL0004 — Property has both attributes
[Option]
[Argument]
public string? Value { get; set; }
```

### Fix

Use one or the other based on your intent:

```csharp
// ✅ As an option (named: --value)
[Option]
public string? Value { get; set; }

// ✅ Or as an argument (positional)
[Argument]
public string? Value { get; set; }
```

---

## Why Use Code Analysis?

Without the analyzer, these mistakes typically surface as:

1. **Cryptic compiler errors** in generated code that's hard to trace back to your source
2. **Runtime exceptions** when the CLI parser encounters duplicate options
3. **Silent failures** where commands don't behave as expected

The analyzer catches these issues immediately in your IDE, with clear error messages pointing to the exact line that needs fixing.

## Suppressing Diagnostics

If you need to suppress a diagnostic (not recommended), use the standard `#pragma` or `SuppressMessage` approaches:

```csharp
#pragma warning disable ACL0001
[Option] public string? name { get; set; }
#pragma warning restore ACL0001
```

Or in `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.ACL0001.severity = none
```
