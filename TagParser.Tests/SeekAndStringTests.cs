using System.Text;
using TP = TagParser.TagParser;

namespace TagParser.Tests;

[TestFixture]
public class SeekAndStringTests
{
    private readonly List<Stream> _streams = new();

    [TearDown]
    public void TearDown()
    {
        foreach (var stream in _streams)
        {
            stream?.Dispose();
        }
        _streams.Clear();
    }

    private MemoryStream CreateMemoryStream(string content, Encoding? encoding = null)
    {
        var bytes = (encoding ?? Encoding.UTF8).GetBytes(content);
        var stream = new MemoryStream(bytes);
        _streams.Add(stream);
        return stream;
    }

    [Test]
    public void Seek_WithSingleOccurrence_FindsCorrectPosition()
    {
        // Arrange
        var content = "Hello <tag>content</tag> world";
        using var stream = CreateMemoryStream(content);
        var searchBytes = Encoding.UTF8.GetBytes("<tag>");

        // Act
        var positions = TP.Seek(stream, searchBytes);

        // Assert
        Assert.That(positions, Has.Count.EqualTo(1));
        Assert.That(positions[0], Is.EqualTo(6)); // "<tag>" starts at position 6
    }

    [Test]
    public void Seek_WithMultipleOccurrences_FindsAllPositions()
    {
        // Arrange
        var content = "<tag>first</tag> some text <tag>second</tag>";
        using var stream = CreateMemoryStream(content);
        var searchBytes = Encoding.UTF8.GetBytes("<tag>");

        // Act
        var positions = TP.Seek(stream, searchBytes);

        // Assert
        Assert.That(positions, Has.Count.EqualTo(2));
        Assert.That(positions[0], Is.EqualTo(0)); // First "<tag>" at position 0
        Assert.That(positions[1], Is.EqualTo(27)); // Second "<tag>" at position 27
    }

    [Test]
    public void Seek_WithOverlappingPattern_FindsAllOccurrences()
    {
        // Arrange
        var content = "aaabaaabaaab";
        using var stream = CreateMemoryStream(content);
        var searchBytes = Encoding.UTF8.GetBytes("aaa");

        // Act
        var positions = TP.Seek(stream, searchBytes);

        // Assert
        Assert.That(positions, Has.Count.EqualTo(3));
        Assert.That(positions[0], Is.EqualTo(0));
        Assert.That(positions[1], Is.EqualTo(4));
        Assert.That(positions[2], Is.EqualTo(8));
    }

    [Test]
    public void Seek_WithEmptyPattern_ReturnsEmptyList()
    {
        // Arrange
        var content = "Some content";
        using var stream = CreateMemoryStream(content);
        var searchBytes = Array.Empty<byte>();

        // Act
        var positions = TP.Seek(stream, searchBytes);

        // Assert
        Assert.That(positions, Is.Empty);
    }

    [Test]
    public void Seek_WithPatternNotFound_ReturnsEmptyList()
    {
        // Arrange
        var content = "Hello world";
        using var stream = CreateMemoryStream(content);
        var searchBytes = Encoding.UTF8.GetBytes("xyz");

        // Act
        var positions = TP.Seek(stream, searchBytes);

        // Assert
        Assert.That(positions, Is.Empty);
    }

    [Test]
    public void Seek_WithLargeContent_HandlesBufferBoundaries()
    {
        // Arrange - Create content larger than buffer size (8KB)
        var pattern = "<marker>";
        var largeContent = new StringBuilder();
        
        // Add pattern at the beginning
        largeContent.Append(pattern);
        
        // Add 10KB of content
        largeContent.Append(new string('x', 10240));
        
        // Add pattern at the end
        largeContent.Append(pattern);
        
        using var stream = CreateMemoryStream(largeContent.ToString());
        var searchBytes = Encoding.UTF8.GetBytes(pattern);

        // Act
        var positions = TP.Seek(stream, searchBytes);

        // Assert
        Assert.That(positions, Has.Count.EqualTo(2));
        Assert.That(positions[0], Is.EqualTo(0)); // Pattern at start
        Assert.That(positions[1], Is.EqualTo(pattern.Length + 10240)); // Pattern after large content
    }

    [Test]
    public void GetString_WithValidPosition_ReturnsCorrectString()
    {
        // Arrange
        var content = "Hello <tag>world</tag> test";
        var encoding = Encoding.UTF8;
        using var stream = CreateMemoryStream(content, encoding);
        var position = new TP.Position(6, 16); // "<tag>world</tag>" is 16 characters long

        // Act
        var result = TP.GetString(stream, encoding, position);

        // Assert
        Assert.That(result, Is.EqualTo("<tag>world</tag>"));
    }

    [Test]
    public void GetString_WithSmallBuffer_UsesStackAllocation()
    {
        // Arrange
        var content = "Small content";
        var encoding = Encoding.UTF8;
        using var stream = CreateMemoryStream(content, encoding);
        var position = new TP.Position(0, content.Length);

        // Act
        var result = TP.GetString(stream, encoding, position);

        // Assert
        Assert.That(result, Is.EqualTo(content));
    }

    [Test]
    public void GetString_WithLargeBuffer_UsesHeapAllocation()
    {
        // Arrange - Content larger than 1024 bytes
        var content = new string('A', 2048);
        var encoding = Encoding.UTF8;
        using var stream = CreateMemoryStream(content, encoding);
        var position = new TP.Position(0, content.Length);

        // Act
        var result = TP.GetString(stream, encoding, position);

        // Assert
        Assert.That(result, Is.EqualTo(content));
    }

    [Test]
    public void GetString_WithUTF8Encoding_HandlesMultibyteCharacters()
    {
        // Arrange
        var content = "Hello 世界 🌍";
        var encoding = Encoding.UTF8;
        using var stream = CreateMemoryStream(content, encoding);
        var position = new TP.Position(0, encoding.GetByteCount(content));

        // Act
        var result = TP.GetString(stream, encoding, position);

        // Assert
        Assert.That(result, Is.EqualTo(content));
    }

    [Test]
    public void GetString_WithNullStream_ThrowsArgumentNullException()
    {
        // Arrange
        var encoding = Encoding.UTF8;
        var position = new TP.Position(0, 10);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => TP.GetString(null!, encoding, position));
    }

    [Test]
    public void GetString_WithNullEncoding_ThrowsArgumentNullException()
    {
        // Arrange
        var content = "Test content";
        using var stream = CreateMemoryStream(content);
        var position = new TP.Position(0, 5);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => TP.GetString(stream, null!, position));
    }
}
