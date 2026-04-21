using FluentAssertions;
using SkillBot.Api.Services;

namespace SkillBot.Tests.Unit.Services;

public class InputValidatorTests
{
    private readonly InputValidator _sut = new();

    [Fact]
    public async Task ValidInput_ReturnsValid()
    {
        var result = await _sut.ValidateInputAsync("Hello, how are you?");

        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task EmptyInput_ReturnsInvalid()
    {
        var result = await _sut.ValidateInputAsync("");

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task WhitespaceInput_ReturnsInvalid()
    {
        var result = await _sut.ValidateInputAsync("   ");

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task TooLongInput_ReturnsInvalid()
    {
        var longInput = new string('a', 10001);

        var result = await _sut.ValidateInputAsync(longInput);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("10000");
    }

    [Theory]
    [InlineData("SELECT * FROM users")]
    [InlineData("DROP TABLE users")]
    [InlineData("DELETE FROM orders")]
    [InlineData("UNION SELECT password FROM admins")]
    public async Task SqlInjection_ReturnsInvalid(string input)
    {
        var result = await _sut.ValidateInputAsync(input);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("SQL");
    }

    [Theory]
    [InlineData("<script>alert('xss')</script>")]
    [InlineData("javascript:void(0)")]
    public async Task XssAttempt_ReturnsInvalid(string input)
    {
        var result = await _sut.ValidateInputAsync(input);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("script");
    }

    [Fact]
    public async Task NullInput_ReturnsInvalid()
    {
        var result = await _sut.ValidateInputAsync(null!);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }
}
