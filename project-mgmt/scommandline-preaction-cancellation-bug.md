# Async pre-actions are not covered by process-termination handling: Ctrl+C hard-kills the process instead of cancelling the token

## Summary

`InvocationPipeline.InvokeAsync` installs the `ProcessTerminationHandler` only around the
**main command action**, never around **pre-actions**. As a result, when the user presses
Ctrl+C (SIGINT/SIGTERM) while an *async pre-action* is running:

- the `CancellationToken` the pre-action received is **never cancelled**, and
- the OS default signal handling is **not suppressed**, so the process is **hard-terminated
  immediately** — no `OperationCanceledException`, no unwind, no cleanup.

The same handler running as the **command action** cancels gracefully. So whether Ctrl+C is
cooperative or fatal depends purely on whether the async work runs in a pre-action or the
command action, which is surprising and undocumented.

## Repro

Minimal console app (`net10.0`) referencing `System.CommandLine`:

```csharp
using System.CommandLine;
using System.CommandLine.Invocation;

var slow = new Option<string>("--slow");
slow.Action = new SlowPreAction();          // non-terminating async action => runs as a PreAction

var root = new RootCommand("repro") { slow };
root.SetAction(async (parseResult, ct) => { // async command action
    Console.WriteLine("command action started");
    await Task.Delay(TimeSpan.FromSeconds(30), ct);
    Console.WriteLine("command action finished");
    return 0;
});

return await root.Parse(args).InvokeAsync();

sealed class SlowPreAction : AsynchronousCommandLineAction {
    public override bool Terminating => false;
    public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken ct) {
        Console.WriteLine("pre-action started");
        await Task.Delay(TimeSpan.FromSeconds(30), ct);
        Console.WriteLine("pre-action finished");
        return 0;
    }
}
```

### Case A — cancel during the command action (works as expected)

```
> repro
command action started
^C
```
The token is cancelled, `Task.Delay` throws `OperationCanceledException`, the process exits
cleanly.

### Case B — cancel during the pre-action (the bug)

```
> repro --slow x
pre-action started
^C
```
The process **exits immediately as if killed** — the token is never cancelled, no exception is
observed, and `pre-action finished` never prints. It behaves as though there were no Ctrl+C
handling installed at all.

## Expected behavior

Ctrl+C during an async pre-action should behave the same as during the command action: the
`CancellationToken` handed to the pre-action is cancelled, the OS default kill is suppressed,
and the pre-action is given the `ProcessTerminationTimeout` grace period to unwind.

## Actual behavior

Pre-actions run with no `ProcessTerminationHandler`. The token is inert and the process is
hard-terminated by the default signal.

## Root cause

In `src/System.CommandLine/Invocation/InvocationPipeline.cs`, `InvokeAsync`:

- **Pre-actions** are awaited in the loop with no termination handler:

  ```csharp
  case AsynchronousCommandLineAction asyncAction:
      result = await asyncAction.InvokeAsync(parseResult, cts.Token);   // no ProcessTerminationHandler
      break;
  ```

- The `ProcessTerminationHandler` — which registers the SIGINT/SIGTERM handler
  (`ProcessTerminationHandler.cs`, `PosixSignalRegistration.Create(...)`), sets
  `context.Cancel = true` to suppress the default kill, and cancels the linked `cts` — is
  created **only** for the main command action:

  ```csharp
  var timeout = parseResult.InvocationConfiguration.ProcessTerminationTimeout;
  if (timeout.HasValue) terminationHandler = new(cts, timeout.Value);
  var startedInvocation = asyncAction.InvokeAsync(parseResult, cts.Token);
  ...
  ```

Because no handler is installed during the pre-action phase, nothing ever cancels `cts` there,
and SIGINT/SIGTERM fall through to the runtime default (terminate the process).

Note: even setting aside the hard-kill, a perfectly cooperative pre-action could never observe
cancellation, since `cts` is not cancellable during that phase.

## Suggested fix

Install the process-termination handling around the entire async invocation (pre-actions +
command action), not just the command action — e.g. create the `ProcessTerminationHandler`
before the pre-action loop so SIGINT is intercepted and `cts` is cancellable throughout. At
minimum, pre-actions should receive a token that is actually cancelled on Ctrl+C and should not
be hard-killed mid-run.

## Environment

- `System.CommandLine` 3.0.0-preview.5.26302.115 (also confirmed present on `main`)
- .NET SDK 10.0.100
- Reproduced on macOS (darwin); the code path is platform-independent (both the
  `PosixSignalRegistration` and `Console.CancelKeyPress` branches are gated inside the
  handler that pre-actions never construct).

## Status

Filed upstream as [dotnet/command-line-api#2836](https://github.com/dotnet/command-line-api/issues/2836)
on 2026-07-13. As of 2026-11-24 it is still open with no response, labels, assignee or linked pull
request. The bug ships in the stable `System.CommandLine` 3.0.0: upstream `InvocationPipeline.cs`
still creates the `ProcessTerminationHandler` after the pre-action loop, and the file is unchanged
since 2026-04-09.

No action is planned on our side. The workaround below stays in Albatross.CommandLine 9.0.0 and is
not a release blocker.

## Workaround in Albatross.CommandLine

`PreActionTerminationHandler` (internal, core) mirrors System.CommandLine's
`ProcessTerminationHandler` around each `IAsyncOptionHandler` invocation in `AsyncOptionAction`:
it registers SIGINT/SIGTERM through `PosixSignalRegistration` (falling back to
`Console.CancelKeyPress` + `ProcessExit` on `netstandard2.1`), suppresses the OS kill, cancels a
token linked to the pre-action's, and abandons a handler still running
`ProcessTerminationTimeout` after the signal. It is skipped when that timeout is null, as
System.CommandLine does. Once one option handler is cancelled the remaining ones do not start
(`HasCancelledInputAction`).

Verified with a real Ctrl+C sent to `Sample.CommandLine long-running --slow x` in its own console:
before, the process died with `0xC000013A` (STATUS_CONTROL_C_EXIT) and the log ended at
"Invoking AsyncOptionHandler"; after, it exits 130 with "Option --slow was cancelled" logged and
the host disposed. The abandon-after-timeout path is covered by unit tests
(`PreActionTerminationHandler_RunAsync`).

Remove the workaround once a System.CommandLine release fixes #2836: the two handlers would
otherwise both be registered during pre-actions.
