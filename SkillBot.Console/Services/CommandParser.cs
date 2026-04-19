namespace SkillBot.Console.Services;

public class CommandParser : ICommandParser
{
    public Task<CommandResult> ParseAsync(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Task.FromResult(new CommandResult { IsValid = false, ErrorMessage = "Input is empty." });

        var tokens = Tokenize(input);

        if (tokens.Count == 0)
            return Task.FromResult(new CommandResult { IsValid = false, ErrorMessage = "No command found." });

        var result = new CommandResult
        {
            Command = tokens[0].ToLowerInvariant(),
            IsValid = true
        };

        int i = 1;
        int positional = 0;

        while (i < tokens.Count)
        {
            if (tokens[i].StartsWith("--") && i + 1 < tokens.Count)
            {
                result.Arguments[tokens[i][2..]] = tokens[i + 1];
                i += 2;
            }
            else if (tokens[i].StartsWith("--"))
            {
                result.Arguments[tokens[i][2..]] = "true";
                i++;
            }
            else
            {
                result.Arguments[positional.ToString()] = tokens[i];
                positional++;
                i++;
            }
        }

        return Task.FromResult(result);
    }

    private static List<string> Tokenize(string input)
    {
        var tokens = new List<string>();
        var current = new System.Text.StringBuilder();
        bool inQuotes = false;
        char quoteChar = '"';

        foreach (char c in input)
        {
            if (inQuotes)
            {
                if (c == quoteChar)
                    inQuotes = false;
                else
                    current.Append(c);
            }
            else if (c == '\'' || c == '"')
            {
                inQuotes = true;
                quoteChar = c;
            }
            else if (c == ' ')
            {
                if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0)
            tokens.Add(current.ToString());

        return tokens;
    }
}
