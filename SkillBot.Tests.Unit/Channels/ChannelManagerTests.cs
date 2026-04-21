using FluentAssertions;
using Moq;
using SkillBot.Core.Models;
using SkillBot.Core.Services;
using SkillBot.Infrastructure.Channels;

namespace SkillBot.Tests.Unit.Channels;

public class ChannelManagerTests
{
    private static Mock<IMessagingChannel> MakeChannel(string name, bool isConfigured = true)
    {
        var mock = new Mock<IMessagingChannel>();
        mock.Setup(c => c.Name).Returns(name);
        mock.Setup(c => c.IsConfigured).Returns(isConfigured);
        return mock;
    }

    [Fact]
    public void RegisterChannel_AddsChannel()
    {
        var manager = new ChannelManager();
        var channel = MakeChannel("telegram");

        manager.RegisterChannel(channel.Object);

        manager.GetChannel("telegram").Should().BeSameAs(channel.Object);
    }

    [Fact]
    public void RegisterChannel_DuplicateName_Replaces()
    {
        var manager = new ChannelManager();
        var first = MakeChannel("telegram");
        var second = MakeChannel("telegram");

        manager.RegisterChannel(first.Object);
        manager.RegisterChannel(second.Object);

        manager.GetChannel("telegram").Should().BeSameAs(second.Object);
    }

    [Fact]
    public void GetChannel_ExistingName_ReturnsChannel()
    {
        var manager = new ChannelManager();
        var channel = MakeChannel("slack");
        manager.RegisterChannel(channel.Object);

        var result = manager.GetChannel("slack");

        result.Should().BeSameAs(channel.Object);
    }

    [Fact]
    public void GetChannel_NonExistentName_ReturnsNull()
    {
        var manager = new ChannelManager();

        var result = manager.GetChannel("nonexistent");

        result.Should().BeNull();
    }

    [Fact]
    public void GetAllChannels_ReturnsAll()
    {
        var manager = new ChannelManager();
        manager.RegisterChannel(MakeChannel("telegram").Object);
        manager.RegisterChannel(MakeChannel("slack").Object);

        var all = manager.GetAllChannels().ToList();

        all.Should().HaveCount(2);
    }

    [Fact]
    public void GetEnabledChannels_OnlyEnabled()
    {
        var manager = new ChannelManager();
        manager.RegisterChannel(MakeChannel("telegram", isConfigured: true).Object);
        manager.RegisterChannel(MakeChannel("slack", isConfigured: false).Object);

        var enabled = manager.GetEnabledChannels().ToList();

        enabled.Should().HaveCount(1);
        enabled[0].Name.Should().Be("telegram");
    }
}
