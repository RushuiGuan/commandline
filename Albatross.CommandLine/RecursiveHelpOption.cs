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
/// This option terminates execution after displaying help.
/// </summary>
public class RecursiveHelpOption : Option<bool> {
/// <summary>
/// Creates a new recursive help option with alias --help-all.
/// </summary>
public RecursiveHelpOption() : base("--help-all") {
Description = "Show help for this command and all subcommands";
DefaultValueFactory = _ => false;
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
var writer = new StringWriter();

WriteRecursiveHelp(command, writer, 0);

Console.Out.Write(writer.ToString());
}

private void WriteRecursiveHelp(Command command, TextWriter writer, int depth) {
var indent = new string(' ', depth * 2);

// Write command name
var commandName = GetFullCommandName(command);
writer.WriteLine($"{indent}{commandName}");

// Write description if available
if (!string.IsNullOrWhiteSpace(command.Description)) {
writer.WriteLine($"{indent}  {command.Description}");
}

// Write options for this command
var commandOptions = command.Options
.Where(o => !o.Recursive && o != this) // Exclude global options and self
.ToList();

if (commandOptions.Any()) {
writer.WriteLine($"{indent}  Options:");
foreach (var option in commandOptions) {
WriteOptionHelp(option, writer, depth + 2);
}
}

// Write arguments for this command
if (command.Arguments.Any()) {
writer.WriteLine($"{indent}  Arguments:");
foreach (var argument in command.Arguments) {
WriteArgumentHelp(argument, writer, depth + 2);
}
}

// Add spacing between commands
if (command.Subcommands.Any()) {
writer.WriteLine();
}

// Recursively write help for subcommands
foreach (var subcommand in command.Subcommands.OrderBy(c => c.Name)) {
WriteRecursiveHelp(subcommand, writer, depth);
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

private void WriteOptionHelp(Option option, TextWriter writer, int depth) {
var indent = new string(' ', depth * 2);
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

private void WriteArgumentHelp(Argument argument, TextWriter writer, int depth) {
var indent = new string(' ', depth * 2);
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
