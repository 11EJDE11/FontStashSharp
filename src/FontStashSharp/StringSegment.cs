using System;

namespace FontStashSharp
{
	/// <summary>
	/// Represents a lightweight, read-only view of a substring without allocating a new string.
	/// </summary>
	public readonly ref struct StringSegment
	{
		/// <summary>
		/// The underlying string containing the segment.
		/// </summary>
		public readonly string String;
		/// <summary>
		/// The starting index of the segment within the string.
		/// </summary>
		public readonly int Offset;
		/// <summary>
		/// The length of the segment in characters.
		/// </summary>
		public readonly int Length;

		/// <summary>
		/// Gets the character at the specified index within this segment.
		/// </summary>
		/// <param name="index">The index relative to the start of the segment.</param>
		/// <returns>The character at the specified index.</returns>
		public char this[int index] => String[Offset + index];

		/// <summary>
		/// Gets a value indicating whether this segment is null or empty.
		/// </summary>
		public bool IsNullOrEmpty
		{
			get
			{
				if (String == null) return true;

				return Offset >= String.Length;
			}
		}

		/// <summary>
		/// Initializes a new instance of the StringSegment struct from an entire string.
		/// </summary>
		/// <param name="value">The string to segment.</param>
		public StringSegment(string value)
		{
			String = value;
			Offset = 0;
			Length = value != null ? value.Length : 0;
		}

		/// <summary>
		/// Initializes a new instance of the StringSegment struct from a string starting at an offset.
		/// </summary>
		/// <param name="value">The string to segment.</param>
		/// <param name="offset">The starting index of the segment.</param>
		public StringSegment(string value, int offset)
		{
			String = value;
			Offset = offset;
			Length = value != null ? value.Length - offset : 0;
		}

		/// <summary>
		/// Initializes a new instance of the StringSegment struct with specified offset and length.
		/// </summary>
		/// <param name="value">The string to segment.</param>
		/// <param name="offset">The starting index of the segment.</param>
		/// <param name="length">The length of the segment.</param>
		public StringSegment(string value, int offset, int length)
		{
			String = value;
			Offset = offset;
			Length = length;
		}

		/// <summary>
		/// Determines whether this segment is equal to another segment.
		/// </summary>
		/// <param name="b">The segment to compare with.</param>
		/// <returns>true if the segments reference the same string with the same offset and length; otherwise, false.</returns>
		public bool Equals(StringSegment b) => String == b.String && Offset == b.Offset && Length == b.Length;

		/// <summary>
		/// Determines whether this segment equals the specified object.
		/// </summary>
		/// <param name="obj">The object to compare with.</param>
		/// <returns>This method is not supported for ref structs.</returns>
		/// <exception cref="NotSupportedException">This operation is not supported for ref structs.</exception>
		public override bool Equals(object obj) => throw new NotSupportedException();

		/// <summary>
		/// Gets the hash code for this segment.
		/// </summary>
		/// <returns>The hash code of this segment.</returns>
		public override int GetHashCode() => IsNullOrEmpty ? 0 : String.GetHashCode() ^ Offset ^ Length;

		/// <summary>
		/// Returns the substring represented by this segment.
		/// </summary>
		/// <returns>A new string containing the characters in this segment, or an empty string if empty.</returns>
		public override string ToString() => IsNullOrEmpty ? string.Empty : String.Substring(Offset, Length);

		/// <summary>
		/// Determines whether two segments are equal.
		/// </summary>
		/// <param name="a">The first segment.</param>
		/// <param name="b">The second segment.</param>
		/// <returns>true if the segments are equal; otherwise, false.</returns>
		public static bool operator ==(StringSegment a, StringSegment b) => a.Equals(b);

		/// <summary>
		/// Determines whether two segments are not equal.
		/// </summary>
		/// <param name="a">The first segment.</param>
		/// <param name="b">The second segment.</param>
		/// <returns>true if the segments are not equal; otherwise, false.</returns>
		public static bool operator !=(StringSegment a, StringSegment b) => !a.Equals(b);
	}
}