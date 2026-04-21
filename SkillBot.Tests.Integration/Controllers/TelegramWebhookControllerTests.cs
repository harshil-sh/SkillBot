using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace SkillBot.Tests.Integration.Controllers;

public class TelegramWebhookControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TelegramWebhookControllerTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HandleUpdate_NoMessage_ReturnsOk()
    {
        // Update with no Message property
        var update = new { update_id = 1 };

        var response = await _client.PostAsJsonAsync("/api/webhook/telegram", update);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HandleUpdate_WithEmptyTextMessage_ReturnsOk()
    {
        // Message with no text
        var update = new
        {
            update_id = 2,
            message = new
            {
                message_id = 1,
                chat = new { id = 12345, type = "private" },
                date = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            }
        };

        var response = await _client.PostAsJsonAsync("/api/webhook/telegram", update);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HandleUpdate_WithTextMessage_ReturnsOk()
    {
        var update = new
        {
            update_id = 3,
            message = new
            {
                message_id = 2,
                text = "Hello bot!",
                chat = new { id = 99999, type = "private" },
                from = new { id = 99999, is_bot = false, first_name = "Test" },
                date = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            }
        };

        var response = await _client.PostAsJsonAsync("/api/webhook/telegram", update);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
