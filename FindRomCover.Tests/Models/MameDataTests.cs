using FindRomCover.Models;
using FluentAssertions;
using MessagePack;

namespace FindRomCover.Tests.Models;

public class MameDataTests
{
    [Fact]
    public void SerializeDeserializeRoundTripPreservesData()
    {
        var original = new List<MameData>
        {
            new() { MachineName = "sf2", Description = "Street Fighter II" },
            new() { MachineName = "pacman", Description = "Pac-Man" }
        };

        var bytes = MessagePackSerializer.Serialize(original);
        var deserialized = MessagePackSerializer.Deserialize<List<MameData>>(bytes);

        deserialized.Should().HaveCount(2);
        deserialized[0].MachineName.Should().Be("sf2");
        deserialized[0].Description.Should().Be("Street Fighter II");
        deserialized[1].MachineName.Should().Be("pacman");
        deserialized[1].Description.Should().Be("Pac-Man");
    }

    [Fact]
    public void DefaultConstructorSetsEmptyStrings()
    {
        var data = new MameData();

        data.MachineName.Should().BeEmpty();
        data.Description.Should().BeEmpty();
    }

    [Fact]
    public void SerializeDeserializeEmptyListReturnsEmptyList()
    {
        var original = new List<MameData>();
        var bytes = MessagePackSerializer.Serialize(original);
        var deserialized = MessagePackSerializer.Deserialize<List<MameData>>(bytes);

        deserialized.Should().BeEmpty();
    }

    [Fact]
    public void SerializeDeserializeSpecialCharactersPreservesData()
    {
        var original = new List<MameData>
        {
            new() { MachineName = "game-with-dashes", Description = "Game: The Sequel (World 900101)" }
        };

        var bytes = MessagePackSerializer.Serialize(original);
        var deserialized = MessagePackSerializer.Deserialize<List<MameData>>(bytes);

        deserialized[0].MachineName.Should().Be("game-with-dashes");
        deserialized[0].Description.Should().Be("Game: The Sequel (World 900101)");
    }
}
