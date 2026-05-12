using System.Text;

namespace FontStashSharp
{
	/// <summary>
	/// A ref struct for efficiently iterating through codepoints in text sources (string, StringSegment, or StringBuilder).
	/// </summary>
	internal ref struct TextSource
	{
		/// <summary>
		/// The string segment source, if applicable.
		/// </summary>
		public StringSegment StringText;

		/// <summary>
		/// The StringBuilder source, if applicable.
		/// </summary>
		public StringBuilder StringBuilderText;

		private int Position;

		/// <summary>
		/// Initializes a new instance of the <see cref="TextSource"/> struct from a string.
		/// </summary>
		/// <param name="text">The text to iterate through.</param>
		public TextSource(string text)
		{
			StringText = new StringSegment(text);
			StringBuilderText = null;
			Position = 0;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TextSource"/> struct from a StringSegment.
		/// </summary>
		/// <param name="text">The text segment to iterate through.</param>
		public TextSource(StringSegment text)
		{
			StringText = text;
			StringBuilderText = null;
			Position = 0;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TextSource"/> struct from a StringBuilder.
		/// </summary>
		/// <param name="text">The StringBuilder to iterate through.</param>
		public TextSource(StringBuilder text)
		{
			StringText = new StringSegment();
			StringBuilderText = text;
			Position = 0;
		}

		/// <summary>
		/// Gets a value indicating whether the text source is null or empty.
		/// </summary>
		public bool IsNull => StringText.IsNullOrEmpty && StringBuilderText == null;

		/// <summary>
		/// Gets the next Unicode codepoint from the text source.
		/// </summary>
		/// <param name="result">The codepoint value (output).</param>
		/// <returns>True if a codepoint was read, false if the end of text was reached.</returns>
		public bool GetNextCodepoint(out int result)
		{
			result = 0;

			if (!StringText.IsNullOrEmpty)
			{
				if (Position >= StringText.Length)
				{
					return false;
				}

				result = char.ConvertToUtf32(StringText.String, StringText.Offset + Position);
				Position += char.IsSurrogatePair(StringText.String, StringText.Offset + Position) ? 2 : 1;
				return true;
			}

			if (StringBuilderText != null)
			{
				if (Position >= StringBuilderText.Length)
				{
					return false;
				}

				result = StringBuilderConvertToUtf32(StringBuilderText, Position);
				Position += StringBuilderIsSurrogatePair(StringBuilderText, Position) ? 2 : 1;
				return true;
			}

			return false;
		}

		/// <summary>
		/// Resets the iterator to the beginning of the text source.
		/// </summary>
		public void Reset()
		{
			Position = 0;
		}

		/// <summary>
		/// Checks if a character at the specified index in a StringBuilder is a surrogate pair.
		/// </summary>
		private static bool StringBuilderIsSurrogatePair(StringBuilder sb, int index)
		{
			if (index + 1 < sb.Length)
				return char.IsSurrogatePair(sb[index], sb[index + 1]);
			return false;
		}

		/// <summary>
		/// Converts a character at the specified index in a StringBuilder to UTF-32.
		/// </summary>
		private static int StringBuilderConvertToUtf32(StringBuilder sb, int index)
		{
			if (!char.IsHighSurrogate(sb[index]))
				return sb[index];

			return char.ConvertToUtf32(sb[index], sb[index + 1]);
		}

		/// <summary>
		/// Calculates the number of codepoints in a string.
		/// </summary>
		/// <param name="text">The text to count.</param>
		/// <returns>The number of Unicode codepoints in the string.</returns>
		public static int CalculateLength(string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				return 0;
			}

			var pos = 0;
			var result = 0;
			while(pos < text.Length)
			{
				pos += char.IsSurrogatePair(text, pos) ? 2 : 1;
				++result;
			}

			return result;
		}
	}
}
