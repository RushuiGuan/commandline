using System;

namespace Albatross.CommandLine {
	public record class OptionHandlerStatus {
		public string Name { get; }
		public bool Success { get; }
		public string? Message { get; }
		public Exception? Exception { get; }

		public OptionHandlerStatus(string name, bool status, string? message, Exception? exception) {
			this.Name = name;
			this.Success = status;
			this.Message = message;
			this.Exception = exception;
		}
	}
}