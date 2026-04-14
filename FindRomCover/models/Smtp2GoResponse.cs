namespace FindRomCover.Models;

/// <summary>
/// Represents the response from the SMTP2GO API when sending bug reports.
/// </summary>
/// <remarks>
/// This record matches the expected JSON response structure from the bug reporting API.
/// It indicates whether the email was successfully sent and includes any error messages.
/// </remarks>
public sealed record Smtp2GoResponse
{
    /// <summary>
    /// Gets the data payload containing the result of the send operation.
    /// </summary>
    public Smtp2GoData? Data { get; init; }
}
