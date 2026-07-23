namespace BankOfZ.Setup;

internal sealed class SetupCommandOptions
{
    private SetupCommandOptions(string command, IReadOnlyDictionary<string, string> values)
    {
        Command = command;
        Values = values;
    }

    public string Command { get; }
    private IReadOnlyDictionary<string, string> Values { get; }

    public string Required(string name) =>
        Values.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new ArgumentException($"--{name} is required for '{Command}'.");

    public static SetupCommandOptions Parse(string[] args)
    {
        if (args.Length == 0)
        {
            throw new ArgumentException(
                "Use provision-access, inspect-migrations, migrate, import, verify-import, or reset-demo.");
        }

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 1; index < args.Length; index += 2)
        {
            if (!args[index].StartsWith("--", StringComparison.Ordinal) || index + 1 >= args.Length)
            {
                throw new ArgumentException($"Expected --name value near argument {index + 1}.");
            }
            values.Add(args[index][2..], args[index + 1]);
        }

        return new SetupCommandOptions(args[0].ToLowerInvariant(), values);
    }
}
