using System;

namespace Albatross.CommandLine {
	public interface ICommandErrorHandler {
		/// <summary>
		/// Called when an unhandled exception escapes a command handler.
		/// Return a non-null exit code to suppress default error logging.
		/// Return null to fall through to default behavior.
		/// </summary>
		int? Handle(Exception exception);
	}
}