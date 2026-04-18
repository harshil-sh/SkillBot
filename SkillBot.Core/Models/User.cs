namespace SkillBot.Core.Models;

public record User
{
    public required string Id { get; init; }
    public required string Email { get; init; }
    public required string PasswordHash { get; init; }
    public required string Username { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required bool IsActive { get; init; }
}
