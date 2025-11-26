using PokeSharp.Engine.UI.Debug.Core;

namespace PokeSharp.Engine.Debug.Commands.BuiltIn;

/// <summary>
/// Command for switching between console tabs.
/// </summary>
[ConsoleCommand("tab", "Switch between console tabs")]
public class TabCommand : IConsoleCommand
{
    public string Name => "tab";
    public string Description => "Switch between console tabs";
    public string Usage => @"tab [name|index]

Switches to the specified tab by name or index.

Arguments:
  name    Tab name: console, watch, logs, variables
  index   Tab index: 0-3

Examples:
  tab console     Switch to Console tab
  tab watch       Switch to Watch tab
  tab logs        Switch to Logs tab
  tab variables   Switch to Variables tab
  tab 0           Switch to Console tab (by index)
  tab 1           Switch to Watch tab (by index)
  tab             Show current tab and list all tabs";

    // Tab name mappings
    private static readonly Dictionary<string, int> TabNameToIndex = new(StringComparer.OrdinalIgnoreCase)
    {
        { "console", 0 },
        { "con", 0 },
        { "0", 0 },
        { "watch", 1 },
        { "w", 1 },
        { "1", 1 },
        { "logs", 2 },
        { "log", 2 },
        { "l", 2 },
        { "2", 2 },
        { "variables", 3 },
        { "vars", 3 },
        { "var", 3 },
        { "v", 3 },
        { "3", 3 },
    };

    private static readonly string[] TabNames = { "Console", "Watch", "Logs", "Variables" };

    public Task ExecuteAsync(IConsoleContext context, string[] args)
    {
        var theme = context.Theme;

        if (args.Length == 0)
        {
            // Show current tab and list all tabs
            var currentTab = context.GetActiveTab();
            context.WriteLine("Console Tabs:", theme.Info);
            context.WriteLine("─────────────────────────────", theme.BorderPrimary);

            for (int i = 0; i < TabNames.Length; i++)
            {
                var indicator = i == currentTab ? " → " : "   ";
                var status = i == currentTab ? "(active)" : "";
                var color = i == currentTab ? theme.Success : theme.TextSecondary;
                context.WriteLine($"{indicator}{i}. {TabNames[i]} {status}", color);
            }

            context.WriteLine("", theme.TextPrimary);
            context.WriteLine("Use 'tab <name>' or 'tab <index>' to switch", theme.TextDim);
            return Task.CompletedTask;
        }

        var target = args[0];

        if (TabNameToIndex.TryGetValue(target, out var tabIndex))
        {
            context.SwitchToTab(tabIndex);
            context.WriteLine($"Switched to {TabNames[tabIndex]} tab", theme.Success);
        }
        else
        {
            context.WriteLine($"Unknown tab: '{target}'", theme.Error);
            context.WriteLine("Valid tabs: console, watch, logs, variables (or 0-3)", theme.TextDim);
        }

        return Task.CompletedTask;
    }
}

