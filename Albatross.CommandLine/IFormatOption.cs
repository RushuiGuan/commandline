namespace Albatross.CommandLine {
	public interface IHasFormatOption {
		string? Format { get; }
	}
	public interface IHasBenchmarkOption {
		bool Benchmark { get; }
	}
	public interface IHasShowStackOption {
		bool ShowStack { get; }
	}
}
