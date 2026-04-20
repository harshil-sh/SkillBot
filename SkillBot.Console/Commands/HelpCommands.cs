namespace SkillBot.Console.Commands;

public static class HelpCommands
{
    public static void ShowHelp()
    {
        System.Console.WriteLine();
        System.Console.WriteLine("Available Commands");
        System.Console.WriteLine(new string('─', 65));

        System.Console.WriteLine("  Authentication:");
        System.Console.WriteLine("    register <email> <password> <username>");
        System.Console.WriteLine("    login <username-or-email>  (prompts for masked password)");
        System.Console.WriteLine("    logout");
        System.Console.WriteLine();

        System.Console.WriteLine("  Chat:");
        System.Console.WriteLine("    chat <message>");
        System.Console.WriteLine("    multi-agent <message> --agents <agent1,agent2>");
        System.Console.WriteLine("    history [--limit <num>]");
        System.Console.WriteLine("    conversation <conversationId>");
        System.Console.WriteLine("    delete-conversation <conversationId>");
        System.Console.WriteLine();

        System.Console.WriteLine("  Search:");
        System.Console.WriteLine("    search <query> [--count <num>]");
        System.Console.WriteLine("    search-news <query> [--count <num>]");
        System.Console.WriteLine();

        System.Console.WriteLine("  Plugins:");
        System.Console.WriteLine("    plugins                   — list all registered plugins");
        System.Console.WriteLine("    plugins <name>            — show details for a specific plugin");
        System.Console.WriteLine();

        System.Console.WriteLine("  Multi-Agent:");
        System.Console.WriteLine("    agents                    — list available specialist agents");
        System.Console.WriteLine();

        System.Console.WriteLine("  Background Tasks:");
        System.Console.WriteLine("    tasks schedule <desc> --at <datetime> [--multi-agent]");
        System.Console.WriteLine("    tasks recurring <desc> --cron <expression> [--multi-agent]");
        System.Console.WriteLine("    tasks get <taskId>");
        System.Console.WriteLine("    tasks list");
        System.Console.WriteLine("    tasks cancel <taskId>");
        System.Console.WriteLine();

        System.Console.WriteLine("  Settings:");
        System.Console.WriteLine("    settings show");
        System.Console.WriteLine("    settings set-api-key --provider <openai|claude|gemini|serpapi|telegram> --key <key>");
        System.Console.WriteLine("    settings set-provider <openai|claude|gemini>");
        System.Console.WriteLine("    settings get <key>  |  settings set <key> <value>  |  settings list");
        System.Console.WriteLine();

        System.Console.WriteLine("  Usage / Statistics:");
        System.Console.WriteLine("    stats                     — overall usage statistics");
        System.Console.WriteLine("    stats-conversation <id>   — usage for a specific conversation");
        System.Console.WriteLine("    top-conversations [--limit <num>]");
        System.Console.WriteLine("    reset-stats               — clear all usage statistics");
        System.Console.WriteLine();

        System.Console.WriteLine("  Cache:");
        System.Console.WriteLine("    cache-stats");
        System.Console.WriteLine("    cache-health");
        System.Console.WriteLine("    cache-clear               — remove all cached entries");
        System.Console.WriteLine("    cache-invalidate <pattern>  e.g. llm_response_*");
        System.Console.WriteLine();

        System.Console.WriteLine("  Admin:");
        System.Console.WriteLine("    health");
        System.Console.WriteLine();

        System.Console.WriteLine("  Other:");
        System.Console.WriteLine("    help");
        System.Console.WriteLine("    exit");
        System.Console.WriteLine(new string('─', 65));
    }
}
