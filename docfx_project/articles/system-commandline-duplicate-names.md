# Duplicate Symbol Names (System.CommandLine Behavior)

This article documents a behavior of the underlying `System.CommandLine` library, not of
Albatross.CommandLine. It is captured here because it is easy to trigger through Albatross, the
failure is deferred and non-obvious, and understanding *when* it fires explains why the
[Code Analysis](code-analysis.md) package exists.

## Summary

`System.CommandLine` lets you add two child symbols (options or arguments) with the **same name**
to a command without complaint. Nothing validates uniqueness while the command tree is being built
or parsed. The conflict only surfaces the first time a value is looked up **by name**, at which point
it throws:

```
System.InvalidOperationException: Command '<command>' has more than one child named "<name>".
```

Because that lookup builds a name index across the **whole command tree**, the exception can be
thrown by an unrelated command — any code path that resolves a value by name in a process whose tree
contains the duplicate will hit it.

## When the duplicate is tolerated vs. when it throws

| Stage | Duplicate names? |
|-------|------------------|
| Constructing a command and adding options/arguments (`command.Add(symbol)`) | ✅ tolerated — children are appended, no uniqueness check |
| Building the command hierarchy (Albatross `AddCommands()` / `BuildTree()`) | ✅ tolerated — only wires parent/child *commands* |
| `ParseResult` from `RootCommand.Parse(args)` | ✅ tolerated — token matching is structural, not name-indexed |
| First `ParseResult.GetValue<T>(name)` / `GetRequiredValue<T>(name)` (or `SymbolResult.GetResult(name)`) | ❌ **throws** |

The throw originates from `SymbolResultTree.PopulateSymbolsByName`, which is invoked lazily on the
first name-based lookup and builds a `name → symbol` dictionary for the tree:

```
System.InvalidOperationException: Command 'explicit-option' has more than one child named "--option-without-handler".
   at System.CommandLine.Parsing.SymbolResultTree.PopulateSymbolsByName(Command command)
   at System.CommandLine.Parsing.SymbolResultTree.GetResult(String name)
   at System.CommandLine.Parsing.SymbolResult.GetRequiredValue[T](String name)
   at System.CommandLine.ParseResult.GetRequiredValue[T](String name)
```

## What counts as a "name"

The conflict is over the **union of the primary name and all aliases**. `System.CommandLine` treats
an option's aliases as additional child names, so all of the following collide with each other:

- two options whose primary names are equal (case-sensitive match on the resolved name),
- an option whose alias equals another option's primary name,
- two options that share an alias.

## How this shows up through Albatross.CommandLine

Albatross never calls `GetValue`/`GetRequiredValue` while building commands, so the duplicate is
invisible during generation, `AddCommands()`, and `Parse()`. It surfaces when the **parameters object
is materialized**: the generated `RegisterCommands()` factory reads every property from the parse
result by name —

```csharp
// generated in RegisterCommands()
FileName = context.Result.GetRequiredValue<string>("--file-name"),
Count    = context.Result.GetValue<int?>("--count"),
```

— and that resolution happens when the params class is resolved from DI, which the execution pipeline
does during command invocation. So a CLI with duplicate option names typically **builds, tests
structurally, and even parses fine**, then throws the first time any command is actually run and its
parameters are bound. Because the name index spans the whole tree, running a completely unrelated
command can throw too.

### Common ways to create the duplicate in Albatross

- Two properties whose names kebab-case to the same option name (e.g. `Name` and `name` → `--name`).
- Multiple properties applying the same reusable option via `[UseOption<T>]`, where `T` carries a
  fixed `[DefaultNameAliases("--x")]` — every property then resolves to `--x`. (Use
  `[UseOption<T>(UseCustomName = true)]` to fall back to the property-derived name instead.)
- Explicit aliases on `[Option("x")]` that collide with another option's name or alias.

## Avoiding and detecting it

- **Give every option a distinct resolved name and alias set.** When reusing an option type with
  `[DefaultNameAliases]` on more than one property, set `UseCustomName = true` on all but one so they
  don't all inherit the same fixed name.
- **Catch it at compile time.** This is exactly the class of mistake the
  [`Albatross.CommandLine.CodeAnalysis`](code-analysis.md) analyzer is meant to flag (`ACL0001`), so
  the deferred runtime exception becomes an IDE warning instead.

## Note for maintainers

The code generator **deliberately** emits duplicate names as-is rather than silently de-duplicating
them (see `CommandClassBuilder`), on the principle that hiding the mistake is worse than surfacing it —
the developer should see and fix their own conflict. The intended safety net is the analyzer, not
generation-time rewriting.
