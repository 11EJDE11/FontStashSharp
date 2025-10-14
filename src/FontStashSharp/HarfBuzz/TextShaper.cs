using System;
using System.Collections.Generic;
using System.Diagnostics;

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
			if (text.Contains("Hello"))
				Console.WriteLine("hi");
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
				//Debug.Print($"Shaping run with font source {run.FontSourceIndex}...");
				var hbFont = fontSystem.GetHarfBuzzFont(run.FontSourceIndex);

				if (hbFont == null)
				{
					//Debug.Print($"ERROR: HarfBuzz font is null for font source {run.FontSourceIndex}!");
					throw new InvalidOperationException($"HarfBuzz font not available for font source {run.FontSourceIndex}. Ensure font data is cached.");
				}

				//Debug.Print($"HarfBuzz font found for font source {run.FontSourceIndex}");

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

			//if (text.Contains("Hello"))
			//	Console.WriteLine("hi");

			//Debug.Print($"=== Segmenting text: \"{text}\" ===");
			//Debug.Print($"Total font sources: {fontSystem.FontSources.Count}");

			for (int i = 0; i < text.Length; )
			{
				// Get the codepoint at position i
				int codepoint = char.ConvertToUtf32(text, i);
				int charCount = char.IsSurrogatePair(text, i) ? 2 : 1;

				// Find which font source has this codepoint
				var glyphId = fontSystem.GetCodepointIndex(codepoint, out int fontSourceIndex);

				char displayChar = text[i];
				//Debug.Print($"  Char '{displayChar}' (U+{codepoint:X4}) -> Font source: {fontSourceIndex}, GlyphId: {glyphId?.ToString() ?? "null"}");

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

			//Debug.Print($"Created {runs.Count} font runs:");
			foreach (var run in runs)
			{
				var runText = text.Substring(run.Start, run.Length);
				//Debug.Print($"  Run: \"{runText}\" (start={run.Start}, length={run.Length}, fontSource={run.FontSourceIndex})");
			}

			return runs;
		}
	}
}
