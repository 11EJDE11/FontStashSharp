using System;
using System.Collections.Generic;

namespace FontStashSharp.HarfBuzz
{
	/// <summary>
	/// Handles text shaping using HarfBuzz
	/// </summary>
	internal static class TextShaper
	{
		/// <summary>
		/// Shape text using HarfBuzz
		/// </summary>
		/// <param name="fontSystem">The font system containing font sources</param>
		/// <param name="text">The text to shape</param>
		/// <param name="fontSize">The font size</param>
		/// <returns>Shaped text with glyph information</returns>
		public static ShapedText Shape(FontSystem fontSystem, string text, float fontSize)
		{
			if (string.IsNullOrEmpty(text))
			{
				return new ShapedText
				{
					Glyphs = new ShapedGlyph[0],
					OriginalText = text ?? string.Empty,
					FontSize = fontSize
				};
			}

			// Segment text into runs based on font source
			var runs = SegmentTextIntoFontRuns(fontSystem, text);
			var allShapedGlyphs = new List<ShapedGlyph>();

			// Shape each run with its appropriate font
			foreach (var run in runs)
			{
				var hbFont = fontSystem.GetHarfBuzzFont(run.FontSourceIndex);

				if (hbFont == null)
				{
					throw new InvalidOperationException($"HarfBuzz font not available for font source {run.FontSourceIndex}. Ensure font data is cached.");
				}

				// Set the scale for this font size
				hbFont.SetScale(fontSize);

				// Create and configure HarfBuzz buffer
				using (var buffer = new HarfBuzzSharp.Buffer())
				{
					// Add text run to buffer
					buffer.AddUtf16(text, run.Start, run.Length);

					// Set buffer properties (auto-detect script, direction, language)
					buffer.GuessSegmentProperties();

					// Shape the text
					hbFont.Shape(buffer);

					// Get the shaped output
					var glyphInfos = buffer.GlyphInfos;
					var glyphPositions = buffer.GlyphPositions;

					// Convert to our ShapedGlyph format
					for (int i = 0; i < glyphInfos.Length; i++)
					{
						var info = glyphInfos[i];
						var pos = glyphPositions[i];

						allShapedGlyphs.Add(new ShapedGlyph
						{
							GlyphId = (int)info.Codepoint, // This is actually the glyph ID after shaping
							Cluster = (int)info.Cluster + run.Start, // Adjust cluster to global text position
							FontSourceIndex = run.FontSourceIndex,
							XAdvance = pos.XAdvance,
							YAdvance = pos.YAdvance,
							XOffset = pos.XOffset,
							YOffset = pos.YOffset
						});
					}
				}
			}

			return new ShapedText
			{
				Glyphs = allShapedGlyphs.ToArray(),
				OriginalText = text,
				FontSize = fontSize
			};
		}

		private struct FontRun
		{
			public int Start;
			public int Length;
			public int FontSourceIndex;
		}

		private static List<FontRun> SegmentTextIntoFontRuns(FontSystem fontSystem, string text)
		{
			var runs = new List<FontRun>();
			int currentRunStart = 0;
			int currentFontSourceIndex = -1;

			for (int i = 0; i < text.Length; )
			{
				// Get the codepoint at position i
				int codepoint = char.ConvertToUtf32(text, i);
				int charCount = char.IsSurrogatePair(text, i) ? 2 : 1;

				// Find which font source has this codepoint
				var glyphId = fontSystem.GetCodepointIndex(codepoint, out int fontSourceIndex);

				// If this is a new font source, start a new run
				if (fontSourceIndex != currentFontSourceIndex)
				{
					// Save the previous run if it exists
					if (currentFontSourceIndex >= 0)
					{
						runs.Add(new FontRun
						{
							Start = currentRunStart,
							Length = i - currentRunStart,
							FontSourceIndex = currentFontSourceIndex
						});
					}

					// Start new run
					currentRunStart = i;
					currentFontSourceIndex = fontSourceIndex;
				}

				i += charCount;
			}

			// Add the final run
			if (currentFontSourceIndex >= 0)
			{
				runs.Add(new FontRun
				{
					Start = currentRunStart,
					Length = text.Length - currentRunStart,
					FontSourceIndex = currentFontSourceIndex
				});
			}

			return runs;
		}
	}
}
