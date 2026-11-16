using System;
using System.Collections.Generic;

namespace Albatross.CommandLine {
	/// <summary>
	/// Identifies the pipeline stage that produced an error condition so the error handler can
	/// explain it appropriately. Every error raised during command execution is tagged with one
	/// of these sources.
	/// </summary>
	public enum ErrorSource {
		/// <summary>An exception escaped the command handler's execution.</summary>
		CommandHandler = 0,
		/// <summary>
		/// An option handler (pre-action) reported an error — either it threw, or it recorded a
		/// validation failure. <see cref="Error.Symbol"/> carries the option name.
		/// </summary>
		OptionHandler = 1,
		/// <summary>The command handler could not be resolved or created from the service container.</summary>
		ServiceRegistration = 2,
		/// <summary>
		/// The command handler was cancelled (an <see cref="OperationCanceledException"/>), typically
		/// from Ctrl+C. <see cref="Error.Symbol"/> carries the command key.
		/// </summary>
		CommandTaskCancellation = 3,
		/// <summary>
		/// An async option handler (pre-action) was cancelled — typically by Ctrl+C, or by an
		/// <see cref="OperationCanceledException"/> of its own. <see cref="Error.Symbol"/> carries the option name.
		/// Once one option handler is cancelled, the remaining ones do not run.
		/// </summary>
		OptionTaskCancellation = 4,
	}

	/// <summary>
	/// An error condition raised during command execution, tagged with the <see cref="ErrorSource"/>
	/// that produced it and the option name or command key it relates to. May or may not carry an
	/// underlying <see cref="Exception"/>.
	/// </summary>
	public record Error {
		/// <summary>The pipeline stage that produced this error.</summary>
		public ErrorSource Source { get; }
		/// <summary>The option name for option errors, or the command key for command-level errors.</summary>
		public string Symbol { get; }
		/// <summary>A user-facing description of the error.</summary>
		public string Message { get; }

		/// <summary>The exception that caused this error, or null when the error did not originate from one.</summary>
		public Exception? Exception { get; }

		/// <summary>Creates an error.</summary>
		/// <param name="source">The pipeline stage that produced the error.</param>
		/// <param name="symbol">The option name for option errors, or the command key for command-level errors.</param>
		/// <param name="message">A user-facing description of the error.</param>
		/// <param name="exception">The exception that caused the error, or null if none.</param>
		public Error(ErrorSource source, string symbol, string message, Exception? exception) {
			this.Source = source;
			this.Symbol = symbol;
			this.Message = message;
			this.Exception = exception;
		}
	}

	public interface ICommandErrorHandler {
		/// <summary>
		/// Called with one or more error conditions collected during command execution, from any
		/// <see cref="ErrorSource"/> — the command handler, option handlers, service resolution, or
		/// cancellation. Because several option handlers can fail in a single invocation, the errors
		/// arrive as a set. The handler's only job is to notify the user of the errors (e.g. print to
		/// stderr). Return a non-null exit code to override the default; return null to fall through
		/// to the default error exit code.
		/// </summary>
		/// <remarks>
		/// Logging is not necessary here: each error is already logged at the catch site before this
		/// handler is invoked. The handler's job is only to notify the user of the error (e.g. print
		/// to stderr).
		/// </remarks>
		int? Handle(params IEnumerable<Error> errors);
	}
}