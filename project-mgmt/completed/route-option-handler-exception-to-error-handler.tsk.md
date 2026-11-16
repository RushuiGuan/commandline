# Route option-handler exceptions through the global error handler

status: completed
created: 2026-07-11T18:40:33-04:00
priority: normal
tags: pipeline error-handling bug
----

## Objective

When an exception is thrown inside an async option handler, `GlobalCommandAction`
should hand it to the registered `ICommandErrorHandler` (the global error handler) —
the same treatment command-handler exceptions get — instead of silently
short-circuiting to exit code 253 without ever invoking the handler.

## Reasoning

The exception flow diverges by pipeline stage:

- **Command handler** exceptions are caught in `GlobalCommandAction.InvokeAsync`
  (`Albatross.CommandLine/GlobalCommandAction.cs:67`) and routed through `HandleError`
  → `ICommandErrorHandler.Handle(...)`. The global handler sees them.
- **Option handler** exceptions are caught locally in `AsyncOptionAction.InvokeAsync`
  (`Albatross.CommandLine/AsyncOptionAction.cs:39-43` for the 2-generic overload and
  `:79-83` for the 3-generic overload). Each catch logs the exception and stashes it via
  `context.SetInputActionStatus(new OptionHandlerStatus(Symbol.Name, false, msg, err))`,
  which flips `HasInputActionError` to `true`. `GlobalCommandAction.cs:42-44` then sees
  the flag and does `return InputActionErrorExitCode;` (253) — returning *before* the
  command handler is resolved, so `HandleError` / `ICommandErrorHandler` is **never**
  invoked for option-handler exceptions. The exception is swallowed into a status object.

Relevant files:
- `Albatross.CommandLine/GlobalCommandAction.cs` — the short-circuit at `:42-44` and the
  `HandleError` helper at `:25-35`. `HandleError` is where `ICommandErrorHandler` is
  invoked; it currently defaults to `ErrorExitCode` (255) when the handler returns null.
- `Albatross.CommandLine/AsyncOptionAction.cs` — both generic overloads catch and store.
- `Albatross.CommandLine/CommandContext.cs` — `ICommandContext` / `CommandContext`. The
  failed statuses live in the private `inputStatus` dictionary; only the derived
  `HasInputActionError` bool (`:76`) is public.
- `Albatross.CommandLine/OptionHandlerStatus.cs` — carries `Name`, `Success`, `Message`,
  and the nullable `Exception`.

Blocker (prerequisite change): `ICommandContext` exposes only `HasInputActionError` —
there is no accessor to retrieve the failed `OptionHandlerStatus` entries or their
exceptions. Expose the failed statuses (e.g. an
`IEnumerable<OptionHandlerStatus> FailedInputActions` accessor on `ICommandContext`) so
`GlobalCommandAction` can pull the exception(s) to pass into `HandleError`.

Validation-failure vs thrown-exception distinction (important): `OptionHandlerStatus` is
used two ways —
1. a thrown exception (`Exception != null`, set by the `AsyncOptionAction` catch blocks), and
2. a deliberate business validation failure (`Exception == null`), e.g.
   `Sample.CommandLine/ParameterPreProcessing/InstrumentIdOption.cs:32`, where the handler
   records `new OptionHandlerStatus(option.Name, false, "...not valid.", null)`.

Only the exception-bearing statuses should be routed to `ICommandErrorHandler`. A
null-exception validation failure has no exception to hand the error handler and should
keep the current 253 short-circuit.

Option-handler execution order (behavior, relevant to the multiple-exception case):
option PreActions fire in the order the options appear on the **command line**, not in
declaration/registration order. When no options are supplied and handlers run only off
their default values, they fall back to declaration/registration order. So when more than
one option handler throws, which exception is "first" is user-controlled and not
deterministic across invocations.

Decided — responsibility split for the error handler (maintainer, 2026-07-11):
- **Logging happens at the catch site**, right after the exception is caught (i.e. in the
  `AsyncOptionAction` catch blocks and the command-handler catch in `GlobalCommandAction`),
  not inside the error handler.
- **`ICommandErrorHandler`'s sole job is to notify the user of the errors** (e.g. print to
  stderr). It no longer logs.
- The error handler **cannot stop or change the command from exiting** — it only reports
  exceptions; it does not control the exit outcome.

Consequence: `Albatross.CommandLine.Outputs/GlobalErrorHandler.cs` currently both logs
(`logger.LogError`) and prints the `CommandOutput` envelope to stderr. Per the decision
above, the logging call is removed from it, leaving only the user-facing print.

## Open decisions for the executor

1. **Multiple failed handlers** — if several option handlers threw, pass the first
   exception or wrap them in an `AggregateException`? (Note the execution-order behavior
   above: "first" depends on the user's argument order.)
2. **Exit code** — `HandleError` currently defaults to 255 (`ErrorExitCode`) when the
   handler returns null; for this path the default should stay 253 (the reserved
   option-handler code — see the reserved-exit-code contract in `project.md`). So
   `HandleError` needs a parameterized default exit code, or a dedicated path. If the
   handler returns a code, honor it (consistent with the command-handler path).

## Conclusion

Resolved by replacing the ad-hoc status/exit-code handling with a **single unified error
contract**, rather than the narrow "route option exceptions to the handler" fix originally
scoped. Committed on `main` as `90efc69`.

What was done:
- **New contract:** added `ErrorSource` (CommandHandler, OptionHandler, ServiceRegistration,
  CommandTaskCancellation, OptionTaskCancellation) and an `Error` record (`Source`, `Key`,
  `Message`, nullable `Exception`); **deleted `OptionHandlerStatus`**.
  `ICommandErrorHandler.Handle` is now `int? Handle(params IEnumerable<Error> errors)`.
- **Every** error branch in `GlobalCommandAction` now flows through `HandleError` →
  `ICommandErrorHandler.Handle`: option-handler errors (via `CommandContext.InputActionErrors`),
  command-handler exceptions, service resolution / no-handler-registered, and both command- and
  option-handler cancellation. The original 253 short-circuit is gone.
- **Responsibility split honored:** each error is logged at the catch site; the error handler
  only notifies the user. `Outputs.GlobalErrorHandler` no longer logs — it renders the `Error`
  set into a `CommandOutput` envelope on stderr.

How the open decisions landed:
- **#1 Multiple failed handlers** — neither "first" nor `AggregateException`: `Handle` receives
  the **whole set** of `Error`s, so the handler explains each without any lossy collapse. This
  also sidesteps the command-line-order non-determinism.
- **#2 Exit code** — the reserved codes (255/254/253) were **removed**; the default is now a
  single `ErrorExitCode = 1`, or whatever the handler returns. ⚠️ `project.md` still documents the
  reserved codes as a Key Design Decision and Constraint, so it now contradicts the code — left as
  a follow-up doc update.
- **Validation-failure vs thrown-exception** — no longer a conflict: `Error.Exception` is nullable,
  so a message-only failure is a first-class error. `InstrumentIdOption` records
  `new Error(ErrorSource.OptionHandler, option.Name, "...", null)`.

Related work spun off during this task (not part of closing it):
- Added configurable `CommandHost.ProcessTerminationTimeout` (default 30s) because
  System.CommandLine's 2s Ctrl+C grace window is too short for handlers doing real I/O.
- Discovered and documented a System.CommandLine bug — async **pre-actions** get no
  process-termination handling, so Ctrl+C during an async option handler hard-kills the process.
  Captured in `project-mgmt/scommandline-preaction-cancellation-bug.md` and in the
  `OptionTaskCancellation` XML doc as a known limitation.
