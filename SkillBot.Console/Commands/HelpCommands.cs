namespace SkillBot.Console.Commands;

public static class HelpCommands
{
    public static void ShowHelp()
    {
        System.Console.WriteLine();
        System.Console.WriteLine("Available Commands");
        System.Console.WriteLine(new string('─', 50));
        System.Console.WriteLine("  Authentication:");
        System.Console.WriteLine("    register <email> <password> <username>");
        System.Console.WriteLine("    login <username-or-email>  (prompts for masked password)");
        System.Console.WriteLine("    logout");
        System.Console.WriteLine();
        System.Console.WriteLine("  Chat:");
        System.Console.WriteLine("    chat <message>");
        System.Console.WriteLine("    multi-agent <message> --agents <agent1,agent2>");
        System.Console.WriteLine();
        System.Console.WriteLine("  Search:");
        System.Console.WriteLine("    search <query> [--count <num>]");
        System.Console.WriteLine("    search-news <query> [--count <num>]");
        System.Console.WriteLine();
        System.Console.WriteLine("  Settings:");
        System.Console.WriteLine("    settings get <key>");
        System.Console.WriteLine("    settings set <key> <value>");
        System.Console.WriteLine("    settings list");
        System.Console.WriteLine("    settings show");
        System.Console.WriteLine("    settings set-api-key --provider <openai|claude|gemini> --key <key>");
        System.Console.WriteLine("    settings set-provider <openai|claude|gemini>");
        System.Console.WriteLine();
        System.Console.WriteLine("  Admin:");
        System.Console.WriteLine("    stats");
        System.Console.WriteLine("    health");
        System.Console.WriteLine("    cache-stats");
        System.Console.WriteLine();
        System.Console.WriteLine("  Other:");
        System.Console.WriteLine("    help");
        System.Console.WriteLine("    exit");
        System.Console.WriteLine(new string('─', 50));
    }
}
