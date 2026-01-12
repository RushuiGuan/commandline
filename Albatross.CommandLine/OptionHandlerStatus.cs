using System;

namespace Albatross.CommandLine {
	/// <summary>
	/// Records the execution status of an option handler, including success/failure and any error information.
	/// </summary>
	public record class OptionHandlerStatus {
		/// <summary>
		/// Gets the name of the option or argument that was processed.
		/// </summary>
		public string Name { get; }
		/// <summary>
		/// Gets a value indicating whether the handler executed successfully.
		/// </summary>
		public bool Success { get; }
		/// <summary>
		/// Gets an optional message describing the result or error.
		/// </summary>
		public string? Message { get; }
		/// <summary>
		/// Gets the exception that occurred during handler execution, if any.
		/// </summary>
		public Exception? Exception { get; }

		/// <summary>
		/// Creates a new option handler status.
		/// </summary>
		/// <param name="name">The name of the option or argument.</param>
		/// <param name="status">Whether the handler succeeded.</param>
		/// <param name="message">An optional descriptive message.</param>
		/// <param name="exception">The exception that occurred, if any.</param>
		public OptionHandlerStatus(string name, bool status, string? message, Exception? exception) {
			this.Name = name;
			this.Success = status;
			this.Message = message;
			this.Exception = exception;
		}
	}
}