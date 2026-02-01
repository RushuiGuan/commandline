using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;

namespace Albatross.CommandLine.Inputs {
/// <summary>
/// A command-line option that displays help for a command and all of its subcommands recursively.
/// This is an optional option that can be added to any command as needed.
/// Supports "compact" (default) and "detailed" modes.
/// Uses a data structure approach for organizing help information before rendering.
/// </summary>
public class RecursiveHelpOption : Option<string> {
/// <summary>
/// Creates a new recursive help option with alias --help-all.
/// </summary>
public RecursiveHelpOption() : base("--help-all") {
Description = "Show help for this command and all subcommands (compact|detailed)";
Arity = ArgumentArity.ZeroOrOne;
DefaultValueFactory = _ => "compact";
this.Action = new RecursiveHelpAction(this);
}

private class RecursiveHelpAction : SynchronousCommandLineAction {
private readonly RecursiveHelpOption option;

public RecursiveHelpAction(RecursiveHelpOption option) {
this.option = option;
}

public override int Invoke(ParseResult parseResult) {
option.DisplayHelp(parseResult);
return 0;
}
}

/// <summary>
/// Displays recursive help for the specified command and all its subcommands.
/// </summary>
/// <param name="parseResult">The parse result containing the command context.</param>
public void DisplayHelp(ParseResult parseResult) {
var command = parseResult.CommandResult.Command;
var mode = parseResult.GetValue(this) ?? "compact";
var isDetailed = mode.Equals("detailed", StringComparison.OrdinalIgnoreCase);

// Build data structure for help information
var helpData = BuildHelpData(command, isDetailed, isRoot: true);

// Print help
PrintHelp(helpData, isDetailed);
}

private HelpData BuildHelpData(Command command, bool isDetailed, bool isRoot, int depth = 0) {
var data = new HelpData {
Name = GetCommandName(command, isRoot),
Description = command.Description,
Depth = depth,
IsRoot = isRoot
};

// Collect options (excluding global/recursive options)
var options = command.Options
.Where(o => !o.Recursive && o != this)
.Select(o => new ParameterInfo {
Aliases = string.Join(", ", o.Aliases),
ValueHint = GetValueHint(o),
Description = o.Description
})
.ToList();

if (options.Any()) {
data.Options = options;
}

// Collect arguments
var arguments = command.Arguments
.Select(a => new ParameterInfo {
Aliases = $"<{a.Name}>",
Description = a.Description
})
.ToList();

if (arguments.Any()) {
data.Arguments = arguments;
}

// Recursively collect subcommands
data.Subcommands = command.Subcommands
.OrderBy(c => c.Name)
.Select(c => BuildHelpData(c, isDetailed, isRoot: false, depth: depth + 1))
.ToList();

return data;
}

private void PrintHelp(HelpData data, bool isDetailed) {
var writer = new StringWriter();

// Print root command header
writer.WriteLine(data.Name);
if (!string.IsNullOrWhiteSpace(data.Description)) {
writer.WriteLine($"  {data.Description}");
}

// Print root command options
if (data.Options != null && data.Options.Any()) {
writer.WriteLine("  Options:");
foreach (var opt in data.Options) {
PrintParameter(writer, opt, 4);
}
}

// Print root command arguments
if (data.Arguments != null && data.Arguments.Any()) {
writer.WriteLine("  Arguments:");
foreach (var arg in data.Arguments) {
PrintParameter(writer, arg, 4);
}
}

if (data.Subcommands.Any()) {
writer.WriteLine();
}

// Print subcommands
PrintSubcommands(writer, data.Subcommands, isDetailed);

Console.Out.Write(writer.ToString());
}

private void PrintSubcommands(TextWriter writer, List<HelpData> subcommands, bool isDetailed) {
foreach (var subcmd in subcommands) {
PrintSubcommand(writer, subcmd, isDetailed);
}
}

private void PrintSubcommand(TextWriter writer, HelpData data, bool isDetailed) {
var indent = new string(' ', data.Depth * 4);

// Format command name and description on same line (aligned at column 30)
const int alignColumn = 30;
var nameWithIndent = $"{indent}{data.Name}";

if (string.IsNullOrWhiteSpace(data.Description)) {
writer.WriteLine(nameWithIndent);
} else {
var padding = Math.Max(1, alignColumn - nameWithIndent.Length);
writer.WriteLine($"{nameWithIndent}{new string(' ', padding)}{data.Description}");
}

// In detailed mode, show parameters
if (isDetailed) {
if (data.Options != null && data.Options.Any()) {
writer.WriteLine($"{indent}    Options:");
foreach (var opt in data.Options) {
PrintParameter(writer, opt, data.Depth * 4 + 8);
}
}

if (data.Arguments != null && data.Arguments.Any()) {
writer.WriteLine($"{indent}    Arguments:");
foreach (var arg in data.Arguments) {
PrintParameter(writer, arg, data.Depth * 4 + 8);
}
}
}

// Recursively print nested subcommands
if (data.Subcommands.Any()) {
PrintSubcommands(writer, data.Subcommands, isDetailed);
}
}

private void PrintParameter(TextWriter writer, ParameterInfo param, int indentSpaces) {
var indent = new string(' ', indentSpaces);
writer.Write($"{indent}{param.Aliases}");

if (!string.IsNullOrWhiteSpace(param.ValueHint)) {
writer.Write($" {param.ValueHint}");
}

if (!string.IsNullOrWhiteSpace(param.Description)) {
writer.Write($"  {param.Description}");
}

writer.WriteLine();
}

private string GetCommandName(Command command, bool isRoot) {
if (isRoot) {
return GetFullCommandName(command);
}
return command.Name;
}

private string GetFullCommandName(Command command) {
if (command is RootCommand) {
return command.Name;
}

var parts = new List<string>();
var current = command;

while (current != null && current is not RootCommand) {
parts.Insert(0, current.Name);
current = current.Parents.OfType<Command>().FirstOrDefault();
}

return string.Join(" ", parts);
}

private string GetValueHint(Option option) {
if (option.ValueType == typeof(bool)) {
return string.Empty;
}

var parameterName = option.Name.TrimStart('-');
return $"<{parameterName}>";
}

/// <summary>
/// Internal data structure to hold help information for a command.
/// </summary>
private class HelpData {
/// <summary>
/// The name of the command.
/// </summary>
public string Name { get; set; } = string.Empty;

/// <summary>
/// The description of the command.
/// </summary>
public string? Description { get; set; }

/// <summary>
/// The depth level in the command hierarchy (0 for root).
/// </summary>
public int Depth { get; set; }

/// <summary>
/// Indicates whether this is the root command.
/// </summary>
public bool IsRoot { get; set; }

/// <summary>
/// The list of options for this command.
/// </summary>
public List<ParameterInfo>? Options { get; set; }

/// <summary>
/// The list of arguments for this command.
/// </summary>
public List<ParameterInfo>? Arguments { get; set; }

/// <summary>
/// The list of subcommands.
/// </summary>
public List<HelpData> Subcommands { get; set; } = new();
}

/// <summary>
/// Internal data structure to hold parameter (option or argument) information.
/// </summary>
private class ParameterInfo {
/// <summary>
/// The aliases for the parameter (e.g., "-o, --output").
/// </summary>
public string Aliases { get; set; } = string.Empty;

/// <summary>
/// The value hint for the parameter (e.g., "<file>").
/// </summary>
public string? ValueHint { get; set; }

/// <summary>
/// The description of the parameter.
/// </summary>
public string? Description { get; set; }
}
}
}
