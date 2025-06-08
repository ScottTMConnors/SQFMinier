using System.Text.RegularExpressions;

namespace SQFMinier.Tools;

public class SQFMinifier
{
    // Regex to ensure pattern matches only outside string literals
    private const string NotInQuotesPattern =
        """(?=([^"\\]*(\\.|"([^"\\]*\\.)*[^"\\]*"))*[^"]*$)""";

    // Regex patterns for comments
    private static readonly Regex InlineComment = new(@"//+[^\n]+", RegexOptions.Compiled);
    private static readonly Regex BlockComment = new(@"/\*.*?\*/", RegexOptions.Singleline | RegexOptions.Compiled);

    /// <summary>
    ///     Minifies SQF code by stripping comments and redundant whitespace.
    /// </summary>
    private static string MinifyCode(string code)
    {
        code = StripComments(code);
        code = SafeReplacements(code);
        return code;
    }

    /// <summary>
    ///     Removes inline and block comments from the code.
    /// </summary>
    private static string StripComments(string text)
    {
        text = InlineComment.Replace(text, string.Empty);
        text = BlockComment.Replace(text, string.Empty);
        return text;
    }

    /// <summary>
    ///     Builds and applies regex-based whitespace trimming outside of string literals.
    /// </summary>
    private static string SafeReplacements(string text)
    {
        var rules = GetRegexRules();
        foreach (var kvp in rules) text = kvp.Key.Replace(text, kvp.Value);
        return text;
    }

    /// <summary>
    ///     Constructs regex rules to trim spaces around special characters, and remove tabs and newlines.
    /// </summary>
    private static Dictionary<Regex, string> GetRegexRules()
    {
        var dict = new Dictionary<Regex, string>();
        var specialChars = new[] { ",", "=", "[", "]", ";", "-", "/", "{", "}", "(`", ")", "<", ">", "+" };

        foreach (var ch in specialChars)
        {
            var esc = Regex.Escape(ch);
            var pattern = $" *{esc} *{NotInQuotesPattern}";
            dict[new Regex(pattern, RegexOptions.Compiled)] = ch;
        }

        // Remove tabs
        dict[new Regex("\n?\t" + NotInQuotesPattern, RegexOptions.Compiled)] = string.Empty;
        // Remove newlines
        dict[new Regex("\n" + NotInQuotesPattern, RegexOptions.Compiled)] = string.Empty;

        return dict;
    }

    /// <summary>
    ///     Minifies an SQF file, reading from inputPath and optionally writing to outputPath.
    /// </summary>
    /// <param name="inputPath">Path to the source SQF file.</param>
    /// <param name="outputPath">Optional path for the minified output. If null, appends "-min" before extension.</param>
    /// <returns>The minified code as a string.</returns>
    public static string MinifyFile(string inputPath, string outputPath = null)
    {
        if (string.IsNullOrWhiteSpace(inputPath))
            throw new ArgumentException("Input path must be provided.", nameof(inputPath));

        if (!File.Exists(inputPath))
            throw new FileNotFoundException($"Cannot find file: '{inputPath}'");

        var text = File.ReadAllText(inputPath);
        var minified = MinifyCode(text);

        if (outputPath == null)
        {
            var dir = Path.GetDirectoryName(inputPath) ?? string.Empty;
            var name = Path.GetFileNameWithoutExtension(inputPath);
            var ext = Path.GetExtension(inputPath);
            outputPath = Path.Combine(dir, name + "-min" + ext);
        }

        File.WriteAllText(outputPath, minified);
        return minified;
    }
}