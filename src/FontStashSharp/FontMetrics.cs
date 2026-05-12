namespace FontStashSharp
{
	/// <summary>
	/// Represents the metrics of a font at a specific size.
	/// </summary>
	internal struct FontMetrics
	{
		/// <summary>
		/// Gets the ascent in pixels (distance from baseline to top).
		/// </summary>
		public int Ascent { get; private set; }

		/// <summary>
		/// Gets the descent in pixels (distance from baseline to bottom).
		/// </summary>
		public int Descent { get; private set; }

		/// <summary>
		/// Gets the line height in pixels.
		/// </summary>
		public int LineHeight { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="FontMetrics"/> struct.
		/// </summary>
		/// <param name="ascent">The ascent in pixels.</param>
		/// <param name="descent">The descent in pixels.</param>
		/// <param name="lineHeight">The line height in pixels.</param>
		public FontMetrics(int ascent, int descent, int lineHeight)
		{
			Ascent = ascent;
			Descent = descent;
			LineHeight = lineHeight;
		}
	}
}