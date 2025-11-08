using System.Reflection;

namespace PokeSharp.Tests.TestHelpers;

/// <summary>
///     Helper for resolving test data file paths across different execution contexts.
/// </summary>
public static class TestPaths
{
    /// <summary>
    ///     Gets the path to a test data file, resolving from multiple possible locations.
    /// </summary>
    public static string GetTestDataPath(string filename)
    {
        // Try relative to current directory first (works with CopyToOutputDirectory)
        var relativePath = Path.Combine("TestData", filename);
        if (File.Exists(relativePath))
            return relativePath;

        // Try relative to assembly location (test output directory)
        var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (assemblyDir != null)
        {
            var assemblyRelativePath = Path.Combine(assemblyDir, "TestData", filename);
            if (File.Exists(assemblyRelativePath))
                return assemblyRelativePath;
        }

        // Try from project root patterns (for different working directories)
        var patterns = new[]
        {
            Path.Combine("PokeSharp.Tests", "TestData", filename),
            Path.Combine("PokeSHarp.Tests", "TestData", filename), // WSL case variation
            Path.Combine("..", "..", "..", "TestData", filename), // Up from bin/Debug/net9.0
        };

        foreach (var pattern in patterns)
        {
            if (File.Exists(pattern))
                return pattern;
        }

        // Throw clear error with all attempted paths
        throw new FileNotFoundException(
            $"Test data file '{filename}' not found. Tried:\n" +
            $"  - {relativePath}\n" +
            $"  - {Path.Combine(assemblyDir ?? "?", "TestData", filename)}\n" +
            $"  - {string.Join("\n  - ", patterns)}"
        );
    }

    /// <summary>
    ///     Gets the full path to the TestData directory.
    /// </summary>
    public static string GetTestDataDirectory()
    {
        // Try relative to current directory first
        if (Directory.Exists("TestData"))
            return Path.GetFullPath("TestData");

        // Try relative to assembly location
        var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (assemblyDir != null)
        {
            var assemblyTestDataDir = Path.Combine(assemblyDir, "TestData");
            if (Directory.Exists(assemblyTestDataDir))
                return assemblyTestDataDir;
        }

        // Try from project root
        var projectPatterns = new[]
        {
            Path.Combine("PokeSharp.Tests", "TestData"),
            Path.Combine("PokeSHarp.Tests", "TestData"),
        };

        foreach (var pattern in projectPatterns)
        {
            if (Directory.Exists(pattern))
                return Path.GetFullPath(pattern);
        }

        throw new DirectoryNotFoundException(
            "TestData directory not found. Ensure test data files are copied to output."
        );
    }
}
