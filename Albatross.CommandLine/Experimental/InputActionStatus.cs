using System;

namespace Albatross.CommandLine {
	public record class InputActionStatus {
		public string Name { get; }
		public bool Success { get; }
		public string? Message { get; }
		public Exception? Exception { get; }

		public InputActionStatus(string name, bool status, string? message, Exception? exception) {
			this.Name = name;
			this.Success = status;
			this.Message = message;
			this.Exception = exception;
		}
	}
}