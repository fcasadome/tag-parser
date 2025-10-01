using System.Text;
using TP = TagParser.TagParser;

namespace TagParser.Tests;

[TestFixture]
public class TagParserTests
{
    private string _testFilePath = string.Empty;
    private string _testContent = string.Empty;

    [SetUp]
    public void Setup()
    {
        _testFilePath = Path.GetTempFileName();
        _testContent = """
            <?xml version="1.0" encoding="UTF-8"?>
            <document>
                <seg id="1">First segment content</seg>
                <seg id="2">Second segment with <b>bold</b> text</seg>
                <seg id="3">Third segment</seg>
                <other>Not a segment</other>
                <seg id="4">Fourth segment</seg>
            </document>
            """;
        File.WriteAllText(_testFilePath, _testContent, Encoding.UTF8);
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_testFilePath))
            File.Delete(_testFilePath);
    }

    [Test]
    public void Tags_WithValidInput_ReturnsAllSegments()
    {
        // Act
        var result = TP.Tags(_testFilePath, "<seg", "</seg>");

        // Assert
        Assert.That(result, Has.Count.EqualTo(4));
        Assert.That(result, Contains.Item("""<seg id="1">First segment content</seg>"""));
        Assert.That(result, Contains.Item("""<seg id="2">Second segment with <b>bold</b> text</seg>"""));
        Assert.That(result, Contains.Item("""<seg id="3">Third segment</seg>"""));
        Assert.That(result, Contains.Item("""<seg id="4">Fourth segment</seg>"""));
    }

    [Test]
    public void Tags_WithSpecificPattern_ReturnsFilteredSegments()
    {
        // This test uses TagPositions to find a specific segment first, then validates the content
        // Act - Use TagPositions to find all segments, then filter for the one we want
        var positions = TP.TagPositions(_testFilePath, "<seg", "</seg>");
        var encoding = TP.GuessEncoding(_testFilePath);
        
        using var stream = File.OpenRead(_testFilePath);
        var results = new List<string>();
        
        foreach (var position in positions)
        {
            var content = TP.GetString(stream, encoding, position);
            if (content.Contains("""id="2">"""))
            {
                results.Add(content);
            }
        }

        // Assert
        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results, Contains.Item("""<seg id="2">Second segment with <b>bold</b> text</seg>"""));
    }

    [Test]
    public void Tags_WithNestedSegments_ReturnsInnerAndOuterSegments()
    {
        const string nestedContent = "<document><seg id=\"outer\">Outer<seg id=\"inner\">Inner</seg>Tail</seg></document>";
        const string outerSegment = "<seg id=\"outer\">Outer<seg id=\"inner\">Inner</seg>Tail</seg>";
        const string innerSegment = "<seg id=\"inner\">Inner</seg>";

        File.WriteAllText(_testFilePath, nestedContent, Encoding.UTF8);

        var tags = TP.Tags(_testFilePath, "<seg", "</seg>").ToList();

        Assert.That(tags, Has.Count.EqualTo(2));
        Assert.That(tags[0], Is.EqualTo(outerSegment));
        Assert.That(tags[1], Is.EqualTo(innerSegment));

        var positions = TP.TagPositions(_testFilePath, "<seg", "</seg>").ToList();

        Assert.That(positions, Has.Count.EqualTo(2));
        Assert.That(positions[0].Length, Is.EqualTo(Encoding.UTF8.GetByteCount(outerSegment)));
        Assert.That(positions[1].Length, Is.EqualTo(Encoding.UTF8.GetByteCount(innerSegment)));
    }


    [TestCase("", "<seg>", "</seg>")]
    [TestCase("file.xml", "", "</seg>")]
    [TestCase("file.xml", "<seg>", "")]
    public void Tags_WithInvalidParameters_ThrowsArgumentException(string path, string start, string end)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => TP.Tags(path, start, end));
    }

    [Test]
    public void Tags_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => TP.Tags("non-existent-file.xml", "<seg>", "</seg>"));
    }

    [Test]
    public void TagPositions_WithValidInput_ReturnsCorrectPositions()
    {
        // Act
        var positions = TP.TagPositions(_testFilePath, "<seg", "</seg>");

        // Assert
        Assert.That(positions, Has.Count.EqualTo(4));
        
        // Verify positions are in ascending order
        var positionList = positions.ToList();
        for (int i = 1; i < positionList.Count; i++)
        {
            Assert.That(positionList[i].Start, Is.GreaterThan(positionList[i - 1].Start));
        }

        // Verify each position has reasonable length
        foreach (var pos in positions)
        {
            Assert.That(pos.Length, Is.GreaterThan(0));
            Assert.That(pos.Start, Is.GreaterThanOrEqualTo(0));
        }
    }

    [Test]
    public void TagPositions_ConsistentWithTags_ReturnsSameCount()
    {
        // Act
        var tags = TP.Tags(_testFilePath, "<seg", "</seg>");
        var positions = TP.TagPositions(_testFilePath, "<seg", "</seg>");

        // Assert
        Assert.That(positions, Has.Count.EqualTo(tags.Count));
    }
}
