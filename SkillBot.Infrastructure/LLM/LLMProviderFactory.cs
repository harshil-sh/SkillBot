using SkillBot.Core.Services;

namespace SkillBot.Infrastructure.LLM;

public class LLMProviderFactory
{
    private readonly IEnumerable<ILLMProvider> _providers;

    public LLMProviderFactory(IEnumerable<ILLMProvider> providers)
    {
        _providers = providers;
    }

    public ILLMProvider GetProvider(string providerName)
    {
        var provider = _providers.FirstOrDefault(p =>
            p.Name.Equals(providerName, StringComparison.OrdinalIgnoreCase));

        if (provider is not null)
            return provider;

        if (providerName.Equals("claude", StringComparison.OrdinalIgnoreCase) ||
            providerName.Equals("gemini", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException($"Provider '{providerName}' is not implemented yet.");

        throw new ArgumentException($"Unknown provider '{providerName}'.");
    }
}
