namespace PokeSharp.Engine.Debug.Commands.BuiltIn;

/// <summary>
/// Exports console output to clipboard.
/// </summary>
[ConsoleCommand("export", "Export console output to clipboard")]
public class ExportCommand : IConsoleCommand
{
    public string Name => "export";
    public string Description => "Export console output to clipboard";
    public string Usage => @"export [target]
  output            Export console output (default)
  logs              Export logs (text format)
  logs csv          Export logs as CSV
  watch             Export watches (text format)
  watch csv         Export watches as CSV";

    public Task ExecuteAsync(IConsoleContext context, string[] args)
    {
        var theme = context.Theme;

        // Default to output if no args
        var target = args.Length > 0 ? args[0].ToLowerInvariant() : "output";

        switch (target)
        {
            case "output":
            case "console":
                var (totalLines, filteredLines) = context.GetConsoleOutputStats();
                context.CopyConsoleOutputToClipboard();
                context.WriteLine($"Exported {filteredLines} lines to clipboard", theme.Success);
                if (totalLines != filteredLines)
                    context.WriteLine($"({totalLines} total lines, {filteredLines} after filtering)", theme.TextSecondary);
                break;

            case "logs":
                var useLogCsv = args.Length > 1 && args[1].Equals("csv", StringComparison.OrdinalIgnoreCase);
                if (useLogCsv)
                {
                    var csv = context.Logs.ExportToCsv();
                    var lineCount = csv.Split('\n').Length - 1;
                    PokeSharp.Engine.UI.Debug.Utilities.ClipboardManager.SetText(csv);
                    context.WriteLine($"Exported {lineCount} logs to clipboard (CSV format)", theme.Success);
                }
                else
                {
                    context.Logs.CopyToClipboard();
                    var (total, filtered, _, _, _, _) = context.Logs.GetStatistics();
                    context.WriteLine($"Exported {filtered} logs to clipboard", theme.Success);
                }
                break;

            case "watch":
            case "watches":
                var useWatchCsv = args.Length > 1 && args[1].Equals("csv", StringComparison.OrdinalIgnoreCase);
                var (watchTotal, watchPinned, watchErrors, _, watchGroups) = context.Watches.GetStatistics();
                context.Watches.CopyToClipboard(useWatchCsv);
                var format = useWatchCsv ? "CSV" : "text";
                context.WriteLine($"Exported {watchTotal} watches to clipboard ({format} format)", theme.Success);
                if (watchPinned > 0 || watchGroups > 0)
                    context.WriteLine($"({watchPinned} pinned, {watchGroups} groups)", theme.TextSecondary);
                break;

            default:
                context.WriteLine($"Unknown export target: '{target}'", theme.Error);
                context.WriteLine(Usage, theme.TextSecondary);
                break;
        }

        return Task.CompletedTask;
    }
}

