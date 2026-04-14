namespace FindRomCover.Models;

/// <summary>
/// Contains the result data from a SMTP2GO API send operation.
/// </summary>
/// <remarks>
/// This record contains the detailed results of an email send attempt through the SMTP2GO API.
/// The Succeeded field indicates how many emails were successfully sent (1 for success, 0 for failure).
/// </remarks>
public sealed record Smtp2GoData
{
    /// <summary>
    /// Gets the number of emails successfully sent.
    /// </summary>
    /// <value>
    /// 1 if the email was sent successfully, 0 otherwise.
    /// </value>
    public int Succeeded { get; init; }

    /// <summary>
    /// Gets the number of emails that failed to send.
    /// </summary>
    public int Failed { get; init; }

    /// <summary>
    /// Gets the list of error messages if the send operation failed.
    /// </summary>
    /// <value>
    /// A list of error descriptions, or null if the operation succeeded.
    /// </value>
    public List<string>? Errors { get; init; }
}
