namespace FindRomCover.models;

// Define the expected structure of the API's JSON response
// This matches the Smtp2GoResponse and Smtp2GoData classes from the API code
public sealed record Smtp2GoResponse
{
    public Smtp2GoData? Data { get; init; }
}