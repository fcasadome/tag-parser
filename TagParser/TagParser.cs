using System.Text;

namespace TagParser;

/// <summary>
/// Static utility class for parsing XML-like tags from files efficiently.
/// Provides methods to extract tag positions and content from large files with optimized I/O operations.
/// </summary>
public static class TagParser
{
	/// <summary>
	/// Represents a position in a file with start offset and length.
	/// </summary>
	/// <param name="Start">The byte offset from the beginning of the file where the content starts</param>
	/// <param name="Length">The length in bytes of the content at this position</param>
	public record Position(long Start, int Length);

	/// <summary>
	/// Extracts all content between specified start and end tags from a file.
	/// This method reads the file with automatic encoding detection and returns the complete tag content including the tags themselves.
	/// </summary>
	/// <param name="path">The file path to parse. Must be a valid, readable file path.</param>
	/// <param name="start">The opening tag to search for (e.g., "&lt;seg&gt;"). Cannot be null or empty.</param>
	/// <param name="end">The closing tag to search for (e.g., "&lt;/seg&gt;"). Cannot be null or empty.</param>
	/// <returns>A read-only collection of strings containing all matched tag content, including the start and end tags.</returns>
	/// <exception cref="ArgumentException">Thrown when path, start, or end parameters are null or empty.</exception>
	/// <exception cref="InvalidOperationException">Thrown when the number of start tags doesn't match the number of end tags.</exception>
	/// <exception cref="FileNotFoundException">Thrown when the specified file doesn't exist.</exception>
	/// <exception cref="UnauthorizedAccessException">Thrown when access to the file is denied.</exception>
	public static IReadOnlyCollection<string> Tags(string path, string start, string end)
	{
		ArgumentException.ThrowIfNullOrEmpty(path);
		ArgumentException.ThrowIfNullOrEmpty(start);
		ArgumentException.ThrowIfNullOrEmpty(end);

		var encoding = GuessEncoding(path);
		using var stream = File.OpenRead(path);

		var startBytes = encoding.GetBytes(start);
		var endBytes = encoding.GetBytes(end);

		var startList = Seek(stream, startBytes);
		var endList = Seek(stream, endBytes);

		var positions = MatchTagPositions(startList, endList, endBytes.Length);

		var result = new List<string>(positions.Count);
		foreach (var position in positions)
		{
			result.Add(GetString(stream, encoding, position));
		}

		return result;
	}

	/// <summary>
	/// Finds the positions of all content between specified start and end tags in a file.
	/// This method is more memory-efficient than Tags() when you only need position information.
	/// </summary>
	/// <param name="path">The file path to parse. Must be a valid, readable file path.</param>
	/// <param name="start">The opening tag to search for (e.g., "&lt;seg&gt;"). Cannot be null or empty.</param>
	/// <param name="end">The closing tag to search for (e.g., "&lt;/seg&gt;"). Cannot be null or empty.</param>
	/// <returns>A read-only collection of Position objects indicating where each complete tag (start + content + end) is located in the file.</returns>
	/// <exception cref="ArgumentException">Thrown when path, start, or end parameters are null or empty.</exception>
	/// <exception cref="InvalidOperationException">Thrown when the number of start tags doesn't match the number of end tags.</exception>
	/// <exception cref="FileNotFoundException">Thrown when the specified file doesn't exist.</exception>
	/// <exception cref="UnauthorizedAccessException">Thrown when access to the file is denied.</exception>
	public static IReadOnlyCollection<Position> TagPositions(string path, string start, string end)
	{
		ArgumentException.ThrowIfNullOrEmpty(path);
		ArgumentException.ThrowIfNullOrEmpty(start);
		ArgumentException.ThrowIfNullOrEmpty(end);

		var encoding = GuessEncoding(path);
		using var stream = File.OpenRead(path);

		var startBytes = encoding.GetBytes(start);
		var endBytes = encoding.GetBytes(end);

		var startList = Seek(stream, startBytes);
		var endList = Seek(stream, endBytes);

		var positions = MatchTagPositions(startList, endList, endBytes.Length);

		return positions;
	}


	private static List<Position> MatchTagPositions(IReadOnlyList<long> startList, IReadOnlyList<long> endList, int endTagLength)
	{
		var positions = new List<Position>(Math.Min(startList.Count, endList.Count));
		var stack = new Stack<long>();

		var startIndex = 0;
		var endIndex = 0;

		while (startIndex < startList.Count || endIndex < endList.Count)
		{
			var hasStart = startIndex < startList.Count;
			var hasEnd = endIndex < endList.Count;

			if (!hasEnd)
			{
				stack.Push(startList[startIndex++]);
				continue;
			}

			if (!hasStart || endList[endIndex] < startList[startIndex])
			{
				if (stack.Count == 0)
					throw new InvalidOperationException("Mismatched start and end tags found.");

				var startPos = stack.Pop();
				var endPos = endList[endIndex++];

				var spanLength = endPos - startPos + endTagLength;
				if (spanLength < 0 || spanLength > int.MaxValue)
					throw new InvalidOperationException("Tag content exceeds supported length.");

				positions.Add(new Position(startPos, (int)spanLength));
				continue;
			}

			stack.Push(startList[startIndex++]);
		}

		if (stack.Count != 0)
			throw new InvalidOperationException("Mismatched start and end tags found.");

		positions.Sort((a, b) => a.Start.CompareTo(b.Start));
		return positions;
	}

	/// <summary>
	/// Automatically detects the text encoding of a file by examining its Byte Order Mark (BOM) and initial content.
	/// This method reads the first 4 bytes of the file to make an educated guess about the encoding.
	/// </summary>
	/// <param name="path">The file path to analyze. Must be a valid, readable file path.</param>
	/// <returns>
	/// The detected encoding. Common return values:
	/// - UTF-8 (with or without BOM)
	/// - UTF-16 (little-endian or big-endian)
	/// - UTF-32 (little-endian or big-endian)
	/// - System default encoding as fallback
	/// </returns>
	/// <exception cref="FileNotFoundException">Thrown when the specified file doesn't exist.</exception>
	/// <exception cref="UnauthorizedAccessException">Thrown when access to the file is denied.</exception>
	/// <remarks>
	/// Detection is based on:
	/// - Unicode BOM patterns for UTF-8, UTF-16, and UTF-32
	/// - XML declaration patterns for XML files
	/// - Falls back to system default encoding if no pattern matches
	/// </remarks>
	public static Encoding GuessEncoding(string path)
	{
		using var br = new BinaryReader(File.OpenRead(path));
		return br.ReadBytes(4) switch
		{
			[0xFF, 0xFE, 0x00, 0x00] => Encoding.UTF32, // BOM
			[0x00, 0x00, 0xFE, 0xFF] => new UTF32Encoding(true, true), // BOM
			[0xEF, 0xBB, 0xBF, _] => Encoding.UTF8, // BOM
			[0x3C, 0x00, 0x3F, _] => Encoding.Unicode, // '<'
			[0x3C, 0x3F, ..] => Encoding.UTF8, // '<?'
			[0xFE, 0xFF, ..] => Encoding.BigEndianUnicode, // BOM
			[0xFF, 0xFE, ..] => Encoding.Unicode, // BOM
			_ => Encoding.Default
		};
	}

	/// <summary>
	/// Efficiently searches for all occurrences of a byte pattern within a stream using an optimized buffered approach.
	/// This method uses a sliding window technique with overlap handling to ensure no matches are missed across buffer boundaries.
	/// </summary>
	/// <param name="stream">The stream to search within. The stream position will be reset to the beginning.</param>
	/// <param name="search">The byte pattern to search for. Cannot be empty.</param>
	/// <returns>
	/// A list of absolute byte positions (from stream start) where the search pattern begins.
	/// Returns an empty list if no matches are found or if the search pattern is empty.
	/// </returns>
	/// <exception cref="ArgumentNullException">Thrown when stream is null.</exception>
	/// <remarks>
	/// Performance characteristics:
	/// - Uses 8KB buffer for efficient I/O operations
	/// - Handles pattern overlaps across buffer boundaries
	/// - Finds multiple occurrences per buffer pass
	/// - Time complexity: O(n) where n is the stream size
	/// - Memory usage: O(1) relative to stream size
	/// </remarks>
	public static List<long> Seek(Stream stream, ReadOnlySpan<byte> search)
	{
		ArgumentNullException.ThrowIfNull(stream);
		
		if (search.IsEmpty)
			return [];

		stream.Seek(0, SeekOrigin.Begin);

		var bufferSize = 8192;
		var buffer = new byte[bufferSize];
		var result = new List<long>();

		var totalBytesRead = 0L;
		var overlap = search.Length - 1;
		var bytesInBuffer = 0;

		while (true)
		{
			// Read new data, keeping overlap from previous read
			var bytesToRead = bufferSize - bytesInBuffer;
			var bytesRead = stream.Read(buffer, bytesInBuffer, bytesToRead);

			if (bytesRead == 0)
				break;

			bytesInBuffer += bytesRead;
			var searchSpan = buffer.AsSpan(0, bytesInBuffer);

			// Find all occurrences in current buffer
			var startIndex = 0;
			while (true)
			{
				var foundIndex = searchSpan.Slice(startIndex).IndexOf(search);
				if (foundIndex < 0)
					break;

				var absoluteIndex = startIndex + foundIndex;
				result.Add(totalBytesRead + absoluteIndex);
				startIndex = absoluteIndex + 1; // Continue search from next position
			}

			// If we didn't read full buffer, we're at end of stream
			if (bytesRead < bytesToRead)
				break;

			// Prepare for next iteration: keep overlap at beginning of buffer
			if (overlap > 0 && bytesInBuffer > overlap)
			{
				Array.Copy(buffer, bytesInBuffer - overlap, buffer, 0, overlap);
				totalBytesRead += bytesInBuffer - overlap;
				bytesInBuffer = overlap;
			}
			else
			{
				totalBytesRead += bytesInBuffer;
				bytesInBuffer = 0;
			}
		}

		return result;
	}

	/// <summary>
	/// Efficiently reads and decodes a string from a specific position in a stream.
	/// Uses stack allocation for small buffers (≤1024 bytes) to minimize heap allocations and improve performance.
	/// </summary>
	/// <param name="stream">The stream to read from. Must be seekable.</param>
	/// <param name="encoding">The text encoding to use for converting bytes to string. Cannot be null.</param>
	/// <param name="p">Position information specifying where to read and how many bytes to read.</param>
	/// <returns>The decoded string content from the specified position.</returns>
	/// <exception cref="ArgumentNullException">Thrown when stream or encoding is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when position is beyond stream bounds.</exception>
	/// <exception cref="EndOfStreamException">Thrown when there aren't enough bytes to read at the specified position.</exception>
	/// <remarks>
	/// Performance optimizations:
	/// - Uses stack allocation for buffers ≤1024 bytes to reduce GC pressure
	/// - Direct stream operations avoid intermediate buffering
	/// - Efficient for reading many small strings from the same stream
	/// </remarks>
	public static string GetString(Stream stream, Encoding encoding, Position p)
	{
		ArgumentNullException.ThrowIfNull(stream);
		ArgumentNullException.ThrowIfNull(encoding);

		stream.Seek(p.Start, SeekOrigin.Begin);

		// Use stack allocation for small buffers to reduce heap allocation
		if (p.Length <= 1024)
		{
			Span<byte> buffer = stackalloc byte[p.Length];
			stream.ReadExactly(buffer);
			return encoding.GetString(buffer);
		}
		else
		{
			var buffer = new byte[p.Length];
			stream.ReadExactly(buffer, 0, p.Length);
			return encoding.GetString(buffer);
		}
	}

	/// <summary>
	/// Reads a string from a given reader at a given position (legacy method)
	/// </summary>
	/// <param name="reader">reader where to read from</param>
	/// <param name="p">position information</param>
	/// <returns>read string</returns>
	[Obsolete("Use GetString(Stream, Encoding, Position) for better performance")]
	public static string GetString(StreamReader reader, Position p)
	{
		ArgumentNullException.ThrowIfNull(reader);
		return GetString(reader.BaseStream, reader.CurrentEncoding, p);
	}
}
