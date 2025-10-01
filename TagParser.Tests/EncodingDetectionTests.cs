using System.Text;
using TP = TagParser.TagParser;

namespace TagParser.Tests;

[TestFixture]
public class EncodingDetectionTests
{
    private readonly List<string> _tempFiles = new();

    [TearDown]
    public void TearDown()
    {
        foreach (var file in _tempFiles.Where(File.Exists))
        {
            File.Delete(file);
        }
        _tempFiles.Clear();
    }

    private string CreateTempFile(byte[] content)
    {
        var path = Path.GetTempFileName();
        _tempFiles.Add(path);
        File.WriteAllBytes(path, content);
        return path;
    }

    [Test]
    public void GuessEncoding_WithUTF8BOM_ReturnsUTF8()
    {
        // Arrange - UTF-8 BOM: EF BB BF
        var content = new byte[] { 0xEF, 0xBB, 0xBF, 0x3C }; // BOM + '<'
        var filePath = CreateTempFile(content);

        // Act
        var encoding = TP.GuessEncoding(filePath);

        // Assert
        Assert.That(encoding, Is.EqualTo(Encoding.UTF8));
    }

    [Test]
    public void GuessEncoding_WithUTF16LEBOM_ReturnsUnicode()
    {
        // Arrange - UTF-16 LE BOM: FF FE
        var content = new byte[] { 0xFF, 0xFE, 0x3C, 0x00 }; // BOM + '<' in UTF-16 LE
        var filePath = CreateTempFile(content);

        // Act
        var encoding = TP.GuessEncoding(filePath);

        // Assert
        Assert.That(encoding, Is.EqualTo(Encoding.Unicode));
    }

    [Test]
    public void GuessEncoding_WithUTF16BEBOM_ReturnsBigEndianUnicode()
    {
        // Arrange - UTF-16 BE BOM: FE FF
        var content = new byte[] { 0xFE, 0xFF, 0x00, 0x3C }; // BOM + '<' in UTF-16 BE
        var filePath = CreateTempFile(content);

        // Act
        var encoding = TP.GuessEncoding(filePath);

        // Assert
        Assert.That(encoding, Is.EqualTo(Encoding.BigEndianUnicode));
    }

    [Test]
    public void GuessEncoding_WithUTF32LEBOM_ReturnsUTF32()
    {
        // Arrange - UTF-32 LE BOM: FF FE 00 00
        var content = new byte[] { 0xFF, 0xFE, 0x00, 0x00 };
        var filePath = CreateTempFile(content);

        // Act
        var encoding = TP.GuessEncoding(filePath);

        // Assert
        Assert.That(encoding, Is.EqualTo(Encoding.UTF32));
    }

    [Test]
    public void GuessEncoding_WithUTF32BEBOM_ReturnsUTF32BigEndian()
    {
        // Arrange - UTF-32 BE BOM: 00 00 FE FF
        var content = new byte[] { 0x00, 0x00, 0xFE, 0xFF };
        var filePath = CreateTempFile(content);

        // Act
        var encoding = TP.GuessEncoding(filePath);

        // Assert
        Assert.That(encoding, Is.TypeOf<UTF32Encoding>());
        // Check if it's big-endian by checking the preamble or WebName
        var utf32 = (UTF32Encoding)encoding;
        Assert.That(utf32.WebName, Is.EqualTo("utf-32BE"));
    }

    [Test]
    public void GuessEncoding_WithXMLDeclarationUTF8_ReturnsUTF8()
    {
        // Arrange - XML declaration starting with '<?'
        var content = new byte[] { 0x3C, 0x3F, 0x78, 0x6D }; // "<?xm"
        var filePath = CreateTempFile(content);

        // Act
        var encoding = TP.GuessEncoding(filePath);

        // Assert
        Assert.That(encoding, Is.EqualTo(Encoding.UTF8));
    }

    [Test]
    public void GuessEncoding_WithXMLDeclarationUTF16_ReturnsUnicode()
    {
        // Arrange - XML declaration starting with '<' in UTF-16 LE
        var content = new byte[] { 0x3C, 0x00, 0x3F, 0x00 }; // "<?..." in UTF-16 LE
        var filePath = CreateTempFile(content);

        // Act
        var encoding = TP.GuessEncoding(filePath);

        // Assert
        Assert.That(encoding, Is.EqualTo(Encoding.Unicode));
    }

    [Test]
    public void GuessEncoding_WithNoBOM_ReturnsDefault()
    {
        // Arrange - No recognizable pattern
        var content = new byte[] { 0x48, 0x65, 0x6C, 0x6C }; // "Hell"
        var filePath = CreateTempFile(content);

        // Act
        var encoding = TP.GuessEncoding(filePath);

        // Assert
        Assert.That(encoding, Is.EqualTo(Encoding.Default));
    }

    [Test]
    public void GuessEncoding_WithEmptyFile_ReturnsDefault()
    {
        // Arrange
        var content = Array.Empty<byte>();
        var filePath = CreateTempFile(content);

        // Act
        var encoding = TP.GuessEncoding(filePath);

        // Assert
        Assert.That(encoding, Is.EqualTo(Encoding.Default));
    }

    [Test]
    public void GuessEncoding_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => TP.GuessEncoding("non-existent-file.xml"));
    }
}
