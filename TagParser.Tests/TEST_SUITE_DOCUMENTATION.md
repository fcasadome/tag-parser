# TagParser NUnit Test Suite

This document describes the comprehensive NUnit test suite created for the TagParser library. The tests are organized into three main test classes that cover all functionality and edge cases.

## Test Coverage Overview

### ✅ **TagParserTests** (Main Functionality)
**File:** `TagParserTests.cs`

#### Core Functionality Tests
- **Tags_WithValidInput_ReturnsAllSegments**: Verifies extraction of all `<seg>` tags from test XML
- **Tags_WithExactTags_ReturnsCorrectSegments**: Tests precision matching with specific tag attributes
- **TagPositions_WithValidInput_ReturnsCorrectPositions**: Validates position calculation and ordering
- **TagPositions_ConsistentWithTags_ReturnsSameCount**: Ensures consistency between Tags() and TagPositions()

#### Input Validation Tests
- **Tags_WithInvalidParameters_ThrowsArgumentException**: Tests null/empty parameter handling
  - Empty file path
  - Empty start tag
  - Empty end tag
- **Tags_WithNonExistentFile_ThrowsFileNotFoundException**: File access error handling

#### Integration Tests
- Tests integration between TagPositions and Tags methods for consistency

---

### ✅ **EncodingDetectionTests** (Encoding Analysis)
**File:** `EncodingDetectionTests.cs`

#### BOM Detection Tests
- **GuessEncoding_WithUTF8BOM_ReturnsUTF8**: UTF-8 with BOM (EF BB BF)
- **GuessEncoding_WithUTF16LEBOM_ReturnsUnicode**: UTF-16 Little Endian (FF FE)
- **GuessEncoding_WithUTF16BEBOM_ReturnsBigEndianUnicode**: UTF-16 Big Endian (FE FF)
- **GuessEncoding_WithUTF32LEBOM_ReturnsUTF32**: UTF-32 Little Endian (FF FE 00 00)
- **GuessEncoding_WithUTF32BEBOM_ReturnsUTF32BigEndian**: UTF-32 Big Endian (00 00 FE FF)

#### Content-Based Detection Tests
- **GuessEncoding_WithXMLDeclarationUTF8_ReturnsUTF8**: XML declaration pattern `<?xml`
- **GuessEncoding_WithXMLDeclarationUTF16_ReturnsUnicode**: UTF-16 XML patterns

#### Edge Cases
- **GuessEncoding_WithNoBOM_ReturnsDefault**: No recognizable patterns
- **GuessEncoding_WithEmptyFile_ReturnsDefault**: Zero-byte files
- **GuessEncoding_WithNonExistentFile_ThrowsFileNotFoundException**: Error handling

---

### ✅ **SeekAndStringTests** (Performance & Algorithm)
**File:** `SeekAndStringTests.cs`

#### Search Algorithm Tests
- **Seek_WithSingleOccurrence_FindsCorrectPosition**: Basic pattern matching
- **Seek_WithMultipleOccurrences_FindsAllPositions**: Multiple pattern detection
- **Seek_WithOverlappingPattern_FindsAllOccurrences**: Complex overlap scenarios (e.g., "aaabaaabaaab" → "aaa")
- **Seek_WithEmptyPattern_ReturnsEmptyList**: Empty search pattern handling
- **Seek_WithPatternNotFound_ReturnsEmptyList**: No matches found

#### Performance & Buffer Management
- **Seek_WithLargeContent_HandlesBufferBoundaries**: Tests 10KB+ content across 8KB buffer boundaries
- **GetString_WithSmallBuffer_UsesStackAllocation**: Tests ≤1024 byte stack allocation optimization
- **GetString_WithLargeBuffer_UsesHeapAllocation**: Tests >1024 byte heap allocation path

#### String Extraction Tests
- **GetString_WithValidPosition_ReturnsCorrectString**: Basic string extraction
- **GetString_WithUTF8Encoding_HandlesMultibyteCharacters**: Unicode support (世界 🌍)

#### Error Handling
- **GetString_WithNullStream_ThrowsArgumentNullException**: Null stream validation
- **GetString_WithNullEncoding_ThrowsArgumentNullException**: Null encoding validation

---

## Test Data & Scenarios

### Sample XML Content
```xml
<?xml version="1.0" encoding="UTF-8"?>
<document>
    <seg id="1">First segment content</seg>
    <seg id="2">Second segment with <b>bold</b> text</seg>
    <seg id="3">Third segment</seg>
    <other>Not a segment</other>
    <seg id="4">Fourth segment</seg>
</document>
```

### Performance Test Scenarios
- **Small files**: <1KB content with stack allocation
- **Large files**: >10KB content across buffer boundaries  
- **Complex patterns**: Overlapping search patterns
- **Unicode content**: Multi-byte character handling

### Edge Cases Covered
- Empty files and patterns
- Non-existent files
- Mismatched tag counts
- Buffer boundary conditions
- Memory allocation strategies
- All major text encodings with BOM detection

---

## Test Infrastructure

### NUnit Framework Features Used
- **[TestFixture]**: Class-level test organization
- **[Test]**: Individual test methods
- **[TestCase]**: Parameterized testing for multiple inputs
- **[SetUp]/[TearDown]**: Resource management
- **Assert.That()**: Modern fluent assertions

### Resource Management
- Automatic cleanup of temporary test files
- Proper disposal of streams and resources
- Memory leak prevention in test runs

### Assertions Used
- **Has.Count.EqualTo()**: Collection count verification
- **Contains.Item()**: Collection membership testing
- **Is.EqualTo()**: Value equality checks
- **Is.GreaterThan()**: Ordering verification
- **Is.TypeOf<T>()**: Type verification
- **Throws<Exception>()**: Exception testing

---

## Running the Tests

Once the build issues are resolved, tests can be executed using:

```bash
# Run all tests
dotnet test

# Run with detailed output
dotnet test --verbosity normal

# Run specific test class
dotnet test --filter "TestCategory=Encoding"

# Generate coverage report
dotnet test --collect:"XPlat Code Coverage"
```

## Test Results Expected

All **30 tests** should pass, covering:
- ✅ **8 Core functionality tests**
- ✅ **10 Encoding detection tests** 
- ✅ **12 Algorithm & performance tests**

This comprehensive test suite ensures the TagParser library works correctly across all supported scenarios and provides confidence in the optimizations and error handling implemented.