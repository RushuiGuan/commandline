using System;
using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;

namespace Albatross.CommandLine {
/// <summary>
/// A command-line option that displays help for a command and all of its subcommands recursively.
/// Supports "compact" (default) and "detailed" modes.
/// This option terminates execution after displaying help.
/// </summary>
public class RecursiveHelpOption : Option<string> {
/// <summary>
/// Creates a new recursive help option with alias --help-all.
/// </summary>
public RecursiveHelpOption() : base("--help-all") {
Description = "Show help for this command and all subcommands (compact|detailed)";
Arity = ArgumentArity.ZeroOrOne;
DefaultValueFactory = _ => "compact";
// Set up the action to display recursive help when the option is specified
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

// Get the mode value, defaulting to "compact"
var mode = parseResult.GetValue(this) ?? "compact";
var isDetailed = mode.Equals("detailed", StringComparison.OrdinalIgnoreCase);

var writer = new StringWriter();
WriteRecursiveHelp(command, writer, 0, isDetailed, isCurrentCommand: true);
Console.Out.Write(writer.ToString());
}

private void WriteRecursiveHelp(Command command, TextWriter writer, int depth, bool isDetailed, bool isCurrentCommand) {
var indent = new string(' ', depth * 4);

// For current command (depth 0), show full details
// For child commands, show name and description on same line with alignment
if (isCurrentCommand) {
// Write current command in detail (like standard help)
var commandName = GetFullCommandName(command);
writer.WriteLine($"{commandName}");

if (!string.IsNullOrWhiteSpace(command.Description)) {
writer.WriteLine($"  {command.Description}");
}

// Write options for current command
var commandOptions = command.Options
.Where(o => !o.Recursive && o != this)
.ToList();

if (commandOptions.Any()) {
writer.WriteLine($"  Options:");
foreach (var option in commandOptions) {
WriteOptionHelp(option, writer, 4);
}
}

// Write arguments for current command
if (command.Arguments.Any()) {
writer.WriteLine($"  Arguments:");
foreach (var argument in command.Arguments) {
WriteArgumentHelp(argument, writer, 4);
}
}

// Add blank line before subcommands
if (command.Subcommands.Any()) {
writer.WriteLine();
}
} else {
// For child commands: write name and description on same line with alignment
var commandName = command.Name;
var description = command.Description ?? string.Empty;

// Calculate alignment (standard System.CommandLine uses column ~30 for descriptions)
const int alignColumn = 30;
var nameWithIndent = $"{indent}{commandName}";

if (string.IsNullOrWhiteSpace(description)) {
writer.WriteLine(nameWithIndent);
} else {
var padding = Math.Max(1, alignColumn - nameWithIndent.Length);
writer.WriteLine($"{nameWithIndent}{new string(' ', padding)}{description}");
}

// If detailed mode, show parameters for child commands
if (isDetailed) {
var commandOptions = command.Options
.Where(o => !o.Recursive && o != this)
.ToList();

if (commandOptions.Any()) {
writer.WriteLine($"{indent}    Options:");
foreach (var option in commandOptions) {
WriteOptionHelp(option, writer, depth * 4 + 8);
}
}

if (command.Arguments.Any()) {
writer.WriteLine($"{indent}    Arguments:");
foreach (var argument in command.Arguments) {
WriteArgumentHelp(argument, writer, depth * 4 + 8);
}
}
}
}

// Recursively write help for subcommands
foreach (var subcommand in command.Subcommands.OrderBy(c => c.Name)) {
WriteRecursiveHelp(subcommand, writer, depth + 1, isDetailed, isCurrentCommand: false);
}
}

private string GetFullCommandName(Command command) {
if (command is RootCommand) {
return command.Name;
}

var parts = new System.Collections.Generic.List<string>();
var current = command;

while (current != null && current is not RootCommand) {
parts.Insert(0, current.Name);
current = current.Parents.OfType<Command>().FirstOrDefault();
}

return string.Join(" ", parts);
}

private void WriteOptionHelp(Option option, TextWriter writer, int indentSpaces) {
var indent = new string(' ', indentSpaces);
var aliases = string.Join(", ", option.Aliases);
var valueHint = GetValueHint(option);

writer.Write($"{indent}{aliases}");
if (!string.IsNullOrWhiteSpace(valueHint)) {
writer.Write($" {valueHint}");
}

if (!string.IsNullOrWhiteSpace(option.Description)) {
writer.Write($"  {option.Description}");
}

writer.WriteLine();
}

private void WriteArgumentHelp(Argument argument, TextWriter writer, int indentSpaces) {
var indent = new string(' ', indentSpaces);
writer.Write($"{indent}<{argument.Name}>");

if (!string.IsNullOrWhiteSpace(argument.Description)) {
writer.Write($"  {argument.Description}");
}

writer.WriteLine();
}

private string GetValueHint(Option option) {
var valueType = option.ValueType;
if (valueType == typeof(bool)) {
return string.Empty;
}

// Try to get a meaningful parameter name from the option
var parameterName = option.Name.TrimStart('-');
return $"<{parameterName}>";
}
}
}
