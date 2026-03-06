# Albatross.CommandLine.CodeAnalysis

Roslyn analyzer companion for [Albatross.CommandLine](https://www.nuget.org/packages/Albatross.CommandLine). It catches common misuse of the `[Verb]`, `[Option]`, `[Argument]`, and `[OptionHandler]` attributes at compile time, before they manifest as runtime errors or confusing generated-code compiler errors.

## Diagnostics

| ID | Severity | Title |
|----|----------|-------|
| [ACL0001](#acl0001) | Warning | Duplicate option name (case-insensitive) |
| [ACL0002](#acl0002) | Warning | Params class does not derive from `VerbAttribute.BaseParamsClass` |
| [ACL0003](#acl0003) | Error | `OptionHandlerAttribute` TOption is not assignable from the attributed class |
| [ACL0004](#acl0004) | Warning | Property has both `[Option]` and `[Argument]` |

---

### ACL0001

**Severity:** Warning

**Title:** Duplicate option name (case-insensitive)

Two or more public properties on a `[Verb]` params class have names that are identical when compared case-insensitively. Because option names are derived from property names via kebab-case conversion (e.g. `MyValue` → `--my-value`), properties like `Name` and `name` both produce `--name`, creating a duplicate CLI option.

```csharp
// ❌ ACL0001 — 'Name' and 'name' both produce --name
[Verb<MyHandler>("greet")]
public class GreetParams {
    [Option] public string? Name { get; set; }
    [Option] public string? name { get; set; }  // warning here
}

// ✅ correct
[Verb<MyHandler>("greet")]
public class GreetParams {
    [Option] public string? Name { get; set; }
    [Option] public string? Alias { get; set; }
}
```

---

### ACL0002

**Severity:** Warning

**Title:** Params class does not derive from `VerbAttribute.BaseParamsClass`

When `VerbAttribute.BaseParamsClass` is set, the params class must derive from the specified type. This applies to both class-targeted (`[Verb<THandler>]`) and assembly-targeted (`[Verb<TParams, THandler>]`) usages. The code generator uses `BaseParamsClass` to group related commands under a shared DI registration — if the params class does not derive from it, the generated switch expression will be missing entries.

```csharp
public class BaseParams { }
public class UnrelatedParams { }

// ❌ ACL0002 — GreetParams does not derive from BaseParams
[Verb<MyHandler>("greet", BaseParamsClass = typeof(BaseParams))]
public class GreetParams { }

// ✅ correct
[Verb<MyHandler>("greet", BaseParamsClass = typeof(BaseParams))]
public class GreetParams : BaseParams { }
```

---

### ACL0003

**Severity:** Error

**Title:** `OptionHandlerAttribute` TOption is not assignable from the attributed class

`OptionHandlerAttribute<TOption, THandler>` and `OptionHandlerAttribute<TOption, THandler, TContextValue>` must be applied to a class that is `TOption` itself or a subclass of it. The code generator uses `TOption` to wire up the option handler — if the attributed class is not assignable to `TOption`, the generated code will not compile.

```csharp
// ❌ ACL0003 — MyOption is not assignable to Option<int>
[OptionHandler<Option<int>, MyHandler>]
public class MyOption : Option<string> { ... }

// ✅ correct — MyOption is assignable to Option<string>
[OptionHandler<Option<string>, MyHandler>]
public class MyOption : Option<string> { ... }

// ✅ also correct — attributed class IS the TOption
[OptionHandler<MyOption, MyHandler>]
public class MyOption : Option<string> { ... }
```

---

### ACL0004

**Severity:** Warning

**Title:** Property has both `[Option]` and `[Argument]`

A property cannot be both a named option and a positional argument. The code generator only processes one attribute per property, so having both is always a mistake.

```csharp
// ❌ ACL0004
[Option]
[Argument]
public string? Value { get; set; }

// ✅ use one or the other
[Option]
public string? Value { get; set; }
```

## Installation

Reference the package as a development-only analyzer — it does not add any runtime dependency:

```xml
<PackageReference Include="Albatross.CommandLine.CodeAnalysis" Version="*">
  <PrivateAssets>all</PrivateAssets>
</PackageReference>
```
