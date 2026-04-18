/// <summary>
/// Error response model
/// </summary>
public class ErrorResponse
{
    public required string Error { get; init; }
    public required string Message { get; init; }
    public string? Details { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public string? TraceId { get; init; }
}