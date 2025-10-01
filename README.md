# TagParser

A high-performance .NET library for efficiently parsing XML-like tags from large files. TagParser provides optimized methods to extract content between specified start and end tags with minimal memory footprint and excellent I/O performance.

## Features

- **🚀 High Performance**: Optimized buffered I/O with 8KB buffer size for efficient file processing
- **💾 Memory Efficient**: Stack allocation for small buffers (≤1024 bytes) to reduce GC pressure
- **🔍 Smart Encoding Detection**: Automatic encoding detection via BOM analysis and content heuristics
- **🛡️ Robust Error Handling**: Comprehensive validation and meaningful error messages
- **📊 Flexible API**: Multiple methods for different use cases (content extraction vs. position-only)
- **⚡ Streaming Support**: Works with any `Stream` implementation, not just files

## Quick Start

### Basic Usage

```csharp
using TagParser;

// Extract all content between <seg> tags
var tags = TagParser.TagParser.Tags("document.xml", "<seg>", "</seg>");
foreach (var content in tags)
{
    Console.WriteLine(content);
}

// Get only positions for memory-efficient processing
var positions = TagParser.TagParser.TagPositions("document.xml", "<seg>", "</seg>");
foreach (var pos in positions)
{
    Console.WriteLine($"Tag at position {pos.Start}, length {pos.Length}");
}
```

### Advanced Usage

```csharp
// Custom tags
var customTags = TagParser.TagParser.Tags("data.xml", "<translation>", "</translation>");

// Manual encoding specification with stream operations
var encoding = TagParser.TagParser.GuessEncoding("file.xml");
using var stream = File.OpenRead("file.xml");
var positions = TagParser.TagParser.TagPositions("file.xml", "<item>", "</item>");

// Read specific content using positions
foreach (var pos in positions)
{
    var content = TagParser.TagParser.GetString(stream, encoding, pos);
    // Process content...
}
```

## API Reference

### Core Methods

#### `Tags(string path, string start, string end)`
Extracts all content between specified start and end tags from a file.

**Parameters:**
- `path`: File path to parse
- `start`: Opening tag (e.g., `"<seg>"`)
- `end`: Closing tag (e.g., `"</seg>"`)

**Returns:** `IReadOnlyCollection<string>` containing all matched content including tags

**Throws:**
- `ArgumentException`: Invalid parameters
- `InvalidOperationException`: Mismatched start/end tag counts
- `FileNotFoundException`: File not found

#### `TagPositions(string path, string start, string end)`
Finds positions of all content between specified tags (more memory-efficient).

**Parameters:** Same as `Tags()`

**Returns:** `IReadOnlyCollection<Position>` with byte positions and lengths

#### `GuessEncoding(string path)`
Automatically detects file encoding by analyzing BOM and content patterns.

**Parameters:**
- `path`: File path to analyze

**Returns:** `Encoding` - The detected encoding or system default

**Supported Encodings:**
- UTF-8 (with/without BOM)
- UTF-16 LE/BE
- UTF-32 LE/BE
- System default (fallback)

#### `GetString(Stream stream, Encoding encoding, Position position)`
Efficiently reads and decodes string content from a specific stream position.

**Parameters:**
- `stream`: Source stream (must be seekable)
- `encoding`: Text encoding to use
- `position`: Position and length information

**Returns:** Decoded string content

### Utility Types

#### `Position` Record
```csharp
public record Position(long Start, int Length);
```
- `Start`: Byte offset from file beginning
- `Length`: Content length in bytes

## Performance Characteristics

### I/O Optimization
- **8KB Buffer Size**: Optimized for modern storage devices
- **Overlap Handling**: Ensures no matches are missed across buffer boundaries
- **Multiple Matches**: Finds all occurrences per buffer pass

### Memory Optimization
- **Stack Allocation**: Uses `stackalloc` for buffers ≤1024 bytes
- **Span Usage**: Leverages `ReadOnlySpan<byte>` for efficient operations
- **Resource Management**: Proper `using` statements prevent resource leaks

### Algorithm Complexity
- **Time Complexity**: O(n) where n is file size
- **Space Complexity**: O(1) relative to file size (plus result storage)
- **Search Algorithm**: Optimized pattern matching with efficient overlap handling

## Error Handling

The library provides comprehensive error handling with specific exceptions:

```csharp
try
{
    var tags = TagParser.TagParser.Tags("document.xml", "<seg>", "</seg>");
}
catch (ArgumentException ex)
{
    // Invalid parameters (null/empty strings)
}
catch (InvalidOperationException ex)
{
    // Mismatched start/end tag counts
}
catch (FileNotFoundException ex)
{
    // File doesn't exist
}
catch (UnauthorizedAccessException ex)
{
    // Access denied
}
```

## Best Practices

### For Large Files
```csharp
// Use TagPositions() for memory efficiency
var positions = TagParser.TagParser.TagPositions("large-file.xml", "<item>", "</item>");

// Process positions incrementally
using var stream = File.OpenRead("large-file.xml");
var encoding = TagParser.TagParser.GuessEncoding("large-file.xml");

foreach (var pos in positions)
{
    var content = TagParser.TagParser.GetString(stream, encoding, pos);
    // Process one item at a time
}
```

### For Small Files
```csharp
// Direct content extraction is fine for smaller files
var allTags = TagParser.TagParser.Tags("small-file.xml", "<seg>", "</seg>");
```

### Custom Encoding
```csharp
// When you know the encoding, avoid auto-detection
var encoding = Encoding.UTF8;
// Use streams directly for better control
```

## Requirements

- **.NET 8.0** or later
- **File System Access** for file-based operations
- **Seekable Streams** for stream-based operations

## Building

```bash
dotnet build
```

## Contributing

When contributing, please ensure:
1. All public methods have XML documentation
2. Performance-critical paths are optimized
3. Proper resource disposal patterns are followed
4. Comprehensive error handling is implemented

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

**MIT License Summary:**
- ✅ Commercial use allowed
- ✅ Modification allowed
- ✅ Distribution allowed
- ✅ Private use allowed
- ℹ️ License and copyright notice required
- ❌ No warranty provided
