# TrackerOption

The `TrackerOption` is a reusable option that enables resumable processing for long-running jobs. It maintains a persistent record of successfully processed items, allowing commands to skip already-completed work when restarted.

## Use Cases

- **Batch file processing**: Process thousands of files across multiple runs, using file paths as identifiers
- **Data migration**: Track migrated records by ID to avoid duplicates if the job is interrupted
- **ETL pipelines**: Resume data extraction/transformation after failures, tracking by record key
- **API synchronization**: Track synced items by URL or resource ID to avoid redundant API calls

## How It Works

1. The option accepts a file path that serves as the tracking store
2. On initialization, the tracker loads all previously processed items into memory and opens the file for appending
3. During execution, use `ProcessIfNew()` to conditionally process items
4. Successfully processed items are immediately written to the file (no data loss if process is killed)
5. On disposal, the file stream is closed

## Basic Usage

### 1. Add the Option to Your Parameters

```csharp
using Albatross.CommandLine.Inputs;

[Verb<MyBatchProcessor>("process-files")]
public class ProcessFilesParams {
    [UseArgument<InputDirectoryArgument>]
    public required DirectoryInfo Directory { get; init; }

    [UseOption<TrackerOption>]
    public Tracker? Tracker { get; init; }
}
```

> [!NOTE]
> The property type is `Tracker`, not `FileInfo`. The `TrackerOption` uses input transformation to automatically load the tracker file and return an initialized `Tracker` instance.

### 2. Use the Tracker in Your Handler

```csharp
public class MyBatchProcessor : IAsyncCommandHandler {
    private readonly ProcessFilesParams parameters;
    private readonly ILogger<MyBatchProcessor> logger;

    public MyBatchProcessor(ProcessFilesParams parameters, ILogger<MyBatchProcessor> logger) {
        this.parameters = parameters;
        this.logger = logger;
    }

    public async Task<int> InvokeAsync(CancellationToken cancellationToken) {
        // Create a no-op tracker if none was provided
        var tracker = parameters.Tracker ?? new Tracker(null);

        var files = parameters.Directory.GetFiles("*.*", SearchOption.AllDirectories);
        foreach (var file in files) {
            await tracker.ProcessIfNew(file.FullName,
                (ct) => ProcessFile(file, ct), logger, cancellationToken);
        }
        return 0;
    }
}
```

## Tracker API

The tracker uses a string identifier to represent each job. This can be any text value that uniquely identifies the work item - a file path, record ID, URL, or any other identifier relevant to your use case.

### `IsNew(string item)`
Returns `true` if the item identifier has not been tracked yet.

### `ProcessIfNew(string item, Func<CancellationToken, Task> func, ILogger logger, CancellationToken cancellationToken)`
Executes the function only if the item identifier is new. On success, the item is automatically added to the tracker.

**Error handling behavior:**
- **Cancellation**: `OperationCanceledException` is re-thrown, allowing cancellation to propagate and stop the job gracefully
- **Other exceptions**: Logged via the provided logger and processing continues with the next item

This design ensures that user-initiated cancellation (Ctrl+C) stops the job immediately while individual item failures don't halt the entire batch.

### `Add(string item)`
Manually add an item identifier to the tracker without processing it.

## Examples with Different Identifiers

The tracker works with any string identifier. Here are some examples:

```csharp
// Track by file path
await tracker.ProcessIfNew(file.FullName, (ct) => ProcessFile(file, ct), logger, cancellationToken);

// Track by record ID
await tracker.ProcessIfNew($"record-{record.Id}", (ct) => MigrateRecord(record, ct), logger, cancellationToken);

// Track by URL
await tracker.ProcessIfNew(url, (ct) => FetchAndStore(url, ct), logger, cancellationToken);

// Track by composite key
await tracker.ProcessIfNew($"{date:yyyy-MM-dd}:{customerId}", (ct) => SyncCustomer(date, customerId, ct), logger, cancellationToken);
```

## Case Sensitivity

By default, `TrackerOption` uses case-insensitive comparison. For case-sensitive tracking, use `CaseSensitiveTrackerOption`:

```csharp
[UseOption<CaseSensitiveTrackerOption>]
public Tracker? Tracker { get; init; }
```

## Batch Processing Pattern

For improved performance with large datasets, combine the tracker with batch processing:

```csharp
public async Task<int> InvokeAsync(CancellationToken cancellationToken) {
    var tracker = parameters.Tracker ?? new Tracker(null);
    var files = parameters.Directory.GetFiles("*.*", SearchOption.AllDirectories);

    foreach (var batch in files.Batch(parameters.BatchSize)) {
        var tasks = new List<Task>();
        foreach (var file in batch) {
            tasks.Add(tracker.ProcessIfNew(file.FullName, (ct) => Process(file, ct), logger, cancellationToken));
        }
        await Task.WhenAll(tasks);
    }
    return 0;
}
```

## Command Line Usage

```bash
# First run - processes all files, saves progress to tracker.txt
myapp process-files ./data --tracker-file tracker.txt

# Second run - skips already processed files, continues from where it left off
myapp process-files ./data --tracker-file tracker.txt
```

Aliases: `--tracker-file`, `--tf`, `--tracker`

## Cancellation and Process Termination

The tracker is designed to be resilient to interruptions:

- **Immediate persistence**: Each successfully processed item is written to the file immediately, so progress is never lost
- **Graceful cancellation**: When Ctrl+C is pressed, `OperationCanceledException` propagates up and stops the job
- **Hard termination**: Even if the process is killed (Task Manager, SIGKILL, power failure), all completed items are already saved

On the next run, processing resumes from where it left off.

## Resource Management

The `Tracker` class implements `IDisposable`. When used with `TrackerOption`, the tracker is automatically disposed by the `CommandContext` when the command completes, closing the file stream. Since items are written immediately on success, disposal is only needed to release the file handle.

## See Also

- [Reusable Parameters](../reusable-parameter.md) - Overview of the reusable parameter pattern
- [Command Context](../command-context.md#3-automatic-resource-disposal) - Automatic disposal of resources
- [Sample: TestLongRunningJobWithTracking](https://github.com/RushuiGuan/commandline/blob/main/Sample.CommandLine/TestLongRunningJobWithTracking.cs) - Complete working example
