using FluentAssertions;
using FindRomCover.Models;
using MessagePack;
using Xunit;

namespace FindRomCover.Tests.Models;

public class MameDataAdditionalTests
{
    [Fact]
    public void DefaultValuesShouldBeEmpty()
    {
        var data = new MameData();

        data.MachineName.Should().BeEmpty();
        data.Description.Should().BeEmpty();
    }

    [Fact]
    public void PropertiesShouldBeSettable()
    {
        var data = new MameData
        {
            MachineName = "sf2",
            Description = "Street Fighter II"
        };

        data.MachineName.Should().Be("sf2");
        data.Description.Should().Be("Street Fighter II");
    }

    [Fact]
    public void MessagePackSerializationRoundTripShouldPreserveData()
    {
        var original = new MameData
        {
            MachineName = "puckman",
            Description = "PuckMan (Japan set 1)"
        };

        var bytes = MessagePackSerializer.Serialize(original);
        var deserialized = MessagePackSerializer.Deserialize<MameData>(bytes);

        deserialized.MachineName.Should().Be(original.MachineName);
        deserialized.Description.Should().Be(original.Description);
    }

    [Fact]
    public void MessagePackSerializationWithSpecialCharactersShouldWork()
    {
        var original = new MameData
        {
            MachineName = "test-game_v2.0",
            Description = "Test Game (c) 2025 - Special Edition!"
        };

        var bytes = MessagePackSerializer.Serialize(original);
        var deserialized = MessagePackSerializer.Deserialize<MameData>(bytes);

        deserialized.MachineName.Should().Be(original.MachineName);
        deserialized.Description.Should().Be(original.Description);
    }

    [Fact]
    public void MessagePackSerializationWithUnicodeShouldWork()
    {
        var original = new MameData
        {
            MachineName = "game_jp",
            Description = "ゲーム (Japan)"
        };

        var bytes = MessagePackSerializer.Serialize(original);
        var deserialized = MessagePackSerializer.Deserialize<MameData>(bytes);

        deserialized.MachineName.Should().Be(original.MachineName);
        deserialized.Description.Should().Be(original.Description);
    }

    [Fact]
    public void MessagePackSerializationWithEmptyFieldsShouldWork()
    {
        var original = new MameData();

        var bytes = MessagePackSerializer.Serialize(original);
        var deserialized = MessagePackSerializer.Deserialize<MameData>(bytes);

        deserialized.MachineName.Should().BeEmpty();
        deserialized.Description.Should().BeEmpty();
    }

    [Fact]
    public void MessagePackSerializationOfListShouldWork()
    {
        var list = new List<MameData>
        {
            new() { MachineName = "game1", Description = "Game One" },
            new() { MachineName = "game2", Description = "Game Two" },
            new() { MachineName = "game3", Description = "Game Three" }
        };

        var bytes = MessagePackSerializer.Serialize(list);
        var deserialized = MessagePackSerializer.Deserialize<List<MameData>>(bytes);

        deserialized.Should().HaveCount(3);
        deserialized[0].MachineName.Should().Be("game1");
        deserialized[1].MachineName.Should().Be("game2");
        deserialized[2].MachineName.Should().Be("game3");
    }

    [Fact]
    public void MessagePackSerializationWithLongStringsShouldWork()
    {
        var original = new MameData
        {
            MachineName = new string('n', 1000),
            Description = new string('d', 5000)
        };

        var bytes = MessagePackSerializer.Serialize(original);
        var deserialized = MessagePackSerializer.Deserialize<MameData>(bytes);

        deserialized.MachineName.Should().HaveLength(1000);
        deserialized.Description.Should().HaveLength(5000);
    }

    [Fact]
    public void MachineNameShouldBeSettable()
    {
        var data = new MameData
        {
            MachineName = "new_name"
        };

        data.MachineName.Should().Be("new_name");
    }

    [Fact]
    public void DescriptionShouldBeSettable()
    {
        var data = new MameData
        {
            Description = "New Description"
        };

        data.Description.Should().Be("New Description");
    }

    [Fact]
    public void MessagePackSerializationWithEmptyListShouldWork()
    {
        var list = new List<MameData>();

        var bytes = MessagePackSerializer.Serialize(list);
        var deserialized = MessagePackSerializer.Deserialize<List<MameData>>(bytes);

        deserialized.Should().BeEmpty();
    }

    [Fact]
    public void MessagePackSerializationWithSpecialCharsInMachineNameShouldWork()
    {
        var original = new MameData
        {
            MachineName = "game & knuckles (v1.0)",
            Description = "Game & Knuckles Version 1.0"
        };

        var bytes = MessagePackSerializer.Serialize(original);
        var deserialized = MessagePackSerializer.Deserialize<MameData>(bytes);

        deserialized.MachineName.Should().Be("game & knuckles (v1.0)");
    }
}
