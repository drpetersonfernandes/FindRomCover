using FluentAssertions;
using FindRomCover.Models;
using MessagePack;
using Xunit;

namespace FindRomCover.Tests.Models;

public class MameDataTests
{
    [Fact]
    public void DefaultMachineNameShouldBeEmpty()
    {
        var data = new MameData();

        data.MachineName.Should().BeEmpty();
    }

    [Fact]
    public void DefaultDescriptionShouldBeEmpty()
    {
        var data = new MameData();

        data.Description.Should().BeEmpty();
    }

    [Fact]
    public void PropertiesShouldBeSettable()
    {
        var data = new MameData
        {
            MachineName = "pacman",
            Description = "Puck Man"
        };

        data.MachineName.Should().Be("pacman");
        data.Description.Should().Be("Puck Man");
    }

    [Fact]
    public void ShouldSerializeAndDeserializeWithMessagePack()
    {
        var original = new MameData
        {
            MachineName = "dkong",
            Description = "Donkey Kong"
        };

        var bytes = MessagePackSerializer.Serialize(original);
        var deserialized = MessagePackSerializer.Deserialize<MameData>(bytes);

        deserialized.MachineName.Should().Be("dkong");
        deserialized.Description.Should().Be("Donkey Kong");
    }

    [Fact]
    public void ShouldSerializeAndDeserializeWithEmptyStrings()
    {
        var original = new MameData();

        var bytes = MessagePackSerializer.Serialize(original);
        var deserialized = MessagePackSerializer.Deserialize<MameData>(bytes);

        deserialized.MachineName.Should().BeEmpty();
        deserialized.Description.Should().BeEmpty();
    }

    [Fact]
    public void ShouldSerializeAndDeserializeWithSpecialCharacters()
    {
        var original = new MameData
        {
            MachineName = "sf2ce",
            Description = "Street Fighter II': Champion Edition (World 920513)"
        };

        var bytes = MessagePackSerializer.Serialize(original);
        var deserialized = MessagePackSerializer.Deserialize<MameData>(bytes);

        deserialized.MachineName.Should().Be("sf2ce");
        deserialized.Description.Should().Contain("Street Fighter II");
    }

    [Fact]
    public void ShouldSerializeAndDeserializeList()
    {
        var original = new List<MameData>
        {
            new() { MachineName = "pacman", Description = "Puck Man" },
            new() { MachineName = "galaga", Description = "Galaga" },
            new() { MachineName = "dkong", Description = "Donkey Kong" }
        };

        var bytes = MessagePackSerializer.Serialize(original);
        var deserialized = MessagePackSerializer.Deserialize<List<MameData>>(bytes);

        deserialized.Should().HaveCount(3);
        deserialized[0].MachineName.Should().Be("pacman");
        deserialized[1].MachineName.Should().Be("galaga");
        deserialized[2].MachineName.Should().Be("dkong");
    }
}
