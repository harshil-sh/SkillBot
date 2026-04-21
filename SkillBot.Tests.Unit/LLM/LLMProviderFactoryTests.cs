using FluentAssertions;
using Moq;
using SkillBot.Core.Services;
using SkillBot.Infrastructure.LLM;

namespace SkillBot.Tests.Unit.LLM;

public class LLMProviderFactoryTests
{
    private static Mock<ILLMProvider> MakeProvider(string name)
    {
        var mock = new Mock<ILLMProvider>();
        mock.Setup(p => p.Name).Returns(name);
        return mock;
    }

    [Fact]
    public void GetProvider_OpenAI_ReturnsOpenAiProvider()
    {
        var openAiProvider = MakeProvider("openai");
        var factory = new LLMProviderFactory(new[] { openAiProvider.Object });

        var result = factory.GetProvider("openai");

        result.Should().BeSameAs(openAiProvider.Object);
    }

    [Fact]
    public void GetProvider_CaseInsensitive_Works()
    {
        var openAiProvider = MakeProvider("openai");
        var factory = new LLMProviderFactory(new[] { openAiProvider.Object });

        var result = factory.GetProvider("OpenAI");

        result.Should().BeSameAs(openAiProvider.Object);
    }

    [Fact]
    public void GetProvider_Claude_ThrowsNotSupportedException()
    {
        var factory = new LLMProviderFactory(Enumerable.Empty<ILLMProvider>());

        var act = () => factory.GetProvider("claude");

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void GetProvider_Gemini_ThrowsNotSupportedException()
    {
        var factory = new LLMProviderFactory(Enumerable.Empty<ILLMProvider>());

        var act = () => factory.GetProvider("gemini");

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void GetProvider_InvalidName_ThrowsArgumentException()
    {
        var factory = new LLMProviderFactory(Enumerable.Empty<ILLMProvider>());

        var act = () => factory.GetProvider("unknown-provider");

        act.Should().Throw<ArgumentException>();
    }
}
