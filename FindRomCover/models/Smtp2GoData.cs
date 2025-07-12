namespace FindRomCover.models;

public sealed record Smtp2GoData
{
    public int Succeeded { get; init; }
    public int Failed { get; init; }
    public List<string>? Errors { get; init; }
}