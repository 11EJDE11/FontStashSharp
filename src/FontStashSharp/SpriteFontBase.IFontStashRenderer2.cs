﻿using FontStashSharp.Interfaces;
using System.Text;
using System;

#if MONOGAME || FNA
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
#elif STRIDE
using Stride.Core.Mathematics;
using Stride.Graphics;
#else
using System.Numerics;
using System.Drawing;
using Matrix = System.Numerics.Matrix3x2;
using Color = FontStashSharp.FSColor;
#endif

namespace FontStashSharp
{
	partial class SpriteFontBase
	{
		private void RenderStyle(IFontStashRenderer2 renderer, TextStyle textStyle, Vector2 pos,
			int lineHeight, int ascent, Color color, ref Matrix transformation, float layerDepth,
			ref VertexPositionColorTexture topLeft, ref VertexPositionColorTexture topRight,
			ref VertexPositionColorTexture bottomLeft, ref VertexPositionColorTexture bottomRight)
		{
			if (textStyle == TextStyle.None || pos.X == 0)
			{
				return;
			}

#if MONOGAME || FNA || STRIDE
			var white = GetWhite(renderer.GraphicsDevice);
#else
			var white = GetWhite(renderer.TextureManager);
#endif

			var start = Vector2.Zero;
			if (textStyle == TextStyle.Strikethrough)
			{
				start.Y = pos.Y - ascent  + lineHeight / 2 - (FontSystemDefaults.TextStyleLineHeight / 2) * RenderFontSizeMultiplicator;
			}
			else
			{
				start.Y = pos.Y + RenderFontSizeMultiplicator;
			}

			var size = new Vector2(pos.X, FontSystemDefaults.TextStyleLineHeight * RenderFontSizeMultiplicator);
			renderer.DrawQuad(white, color, start, ref transformation,
				layerDepth, size, new Rectangle(0, 0, 1, 1),
				ref topLeft, ref topRight, ref bottomLeft, ref bottomRight);
		}

		private float DrawText(IFontStashRenderer2 renderer, TextColorSource source,
			Vector2 position, float rotation, Vector2 origin, Vector2? sourceScale,
			float layerDepth, float characterSpacing, float lineSpacing,
			TextStyle textStyle, FontSystemEffect effect, int effectAmount)
		{
			if (renderer == null)
			{
				throw new ArgumentNullException(nameof(renderer));
			}

#if MONOGAME || FNA || STRIDE
			if (renderer.GraphicsDevice == null)
			{
				throw new ArgumentNullException("renderer.GraphicsDevice can't be null.");
			}
#else
			if (renderer.TextureManager == null)
			{
				throw new ArgumentNullException("renderer.TextureManager can't be null.");
			}
#endif

			if (source.IsNull) return 0.0f;

			// Check if we should use HarfBuzz text shaping
			var dynamicFont = this as DynamicSpriteFont;
			if (dynamicFont != null && dynamicFont.FontSystem.UseTextShaping)
			{
				return DrawShapedText(renderer, source, position, rotation, origin, sourceScale,
					layerDepth, characterSpacing, lineSpacing, textStyle, effect, effectAmount);
			}

			Matrix transformation;
			var scale = sourceScale ?? Utility.DefaultScale;
			Prepare(position, rotation, origin, ref scale, out transformation);

			int ascent, lineHeight;
			PreDraw(source.TextSource, effect, effectAmount, out ascent, out lineHeight);

			var pos = new Vector2(0, ascent);

			FontGlyph prevGlyph = null;
			var topLeft = new VertexPositionColorTexture();
			var topRight = new VertexPositionColorTexture();
			var bottomLeft = new VertexPositionColorTexture();
			var bottomRight = new VertexPositionColorTexture();
			Color? firstColor = null;

			while (true)
			{
				int codepoint;
				Color color;
				if (!source.GetNextCodepoint(out codepoint))
					break;

				if (codepoint == '\n')
				{
					if (textStyle != TextStyle.None && firstColor != null)
					{
						RenderStyle(renderer, textStyle, pos,
							lineHeight, ascent, firstColor.Value, ref transformation, layerDepth,
							ref topLeft, ref topRight, ref bottomLeft, ref bottomRight);
					}

					pos.X = 0.0f;
					pos.Y += lineHeight;
					prevGlyph = null;
					continue;
				}

#if MONOGAME || FNA || STRIDE
				var glyph = GetGlyph(renderer.GraphicsDevice, codepoint, effect, effectAmount);
#else
				var glyph = GetGlyph(renderer.TextureManager, codepoint, effect, effectAmount);
#endif
				if (glyph == null)
				{
					continue;
				}

				if (prevGlyph != null)
				{
					pos.X += characterSpacing;
					pos.X += GetKerning(glyph, prevGlyph);
				}

				if (!glyph.IsEmpty)
				{
					color = source.GetNextColor();
					firstColor = color;

					var baseOffset = new Vector2(glyph.RenderOffset.X, glyph.RenderOffset.Y) + pos;

					var size = new Vector2(glyph.Size.X, glyph.Size.Y);
					renderer.DrawQuad(glyph.Texture, color, baseOffset, ref transformation,
						layerDepth, size, glyph.TextureRectangle,
						ref topLeft, ref topRight, ref bottomLeft, ref bottomRight);
				}

				pos.X += glyph.XAdvance;
				prevGlyph = glyph;
			}

			if (textStyle != TextStyle.None && firstColor != null)
			{
				RenderStyle(renderer, textStyle, pos,
					lineHeight, ascent, firstColor.Value, ref transformation, layerDepth,
					ref topLeft, ref topRight, ref bottomLeft, ref bottomRight);
			}

			return position.X + position.X;
		}

		private float DrawShapedText(IFontStashRenderer2 renderer, TextColorSource source, Vector2 position,
			float rotation, Vector2 origin, Vector2? sourceScale,
			float layerDepth, float characterSpacing, float lineSpacing,
			TextStyle textStyle, FontSystemEffect effect, int effectAmount)
		{
			var dynamicFont = this as DynamicSpriteFont;
			if (dynamicFont == null)
			{
				throw new InvalidOperationException("Text shaping is only supported with DynamicSpriteFont");
			}

			// Get the text from source
			var text = source.TextSource.StringText.String ?? source.TextSource.StringBuilderText?.ToString();
			if (string.IsNullOrEmpty(text))
			{
				return 0.0f;
			}

			Matrix transformation;
			var scale = sourceScale ?? Utility.DefaultScale;
			Prepare(position, rotation, origin, ref scale, out transformation);

			// Split text into lines
			var lines = text.Split('\n');

			// Get metrics for line height (use first font source as default)
			int ascent = 0, lineHeight = 0;
			if (dynamicFont.FontSystem.FontSources.Count > 0)
			{
				int descent, lh;
				dynamicFont.FontSystem.FontSources[0].GetMetricsForSize(FontSize * dynamicFont.FontSystem.FontResolutionFactor, out ascent, out descent, out lh);
				lineHeight = lh;
			}

			var pos = new Vector2(0, ascent);
			float maxX = 0;
			Color? firstColor = null;
			var topLeft = new VertexPositionColorTexture();
			var topRight = new VertexPositionColorTexture();
			var bottomLeft = new VertexPositionColorTexture();
			var bottomRight = new VertexPositionColorTexture();

			for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
			{
				var line = lines[lineIndex];

				if (lineIndex > 0)
				{
					// Render text style for previous line if needed
					if (textStyle != TextStyle.None && firstColor != null)
					{
						RenderStyle(renderer, textStyle, pos,
							lineHeight, ascent, firstColor.Value, ref transformation, layerDepth,
							ref topLeft, ref topRight, ref bottomLeft, ref bottomRight);
					}

					// Move to next line
					pos.X = 0.0f;
					pos.Y += lineHeight + lineSpacing;
					firstColor = null;
				}

				if (string.IsNullOrEmpty(line))
				{
					continue;
				}

				// Shape the line at scaled fontSize to match glyph metrics (uses cache)
				var shapedText = dynamicFont.GetShapedText(line, FontSize * dynamicFont.FontSystem.FontResolutionFactor);

				// Render the shaped line
				float lineStartX = pos.X;
				for (int i = 0; i < shapedText.Glyphs.Length; i++)
				{
					var shapedGlyph = shapedText.Glyphs[i];

					// Add character spacing between glyphs
					if (i > 0 && characterSpacing > 0)
					{
						pos.X += characterSpacing;
					}

					// Get the font glyph
#if MONOGAME || FNA || STRIDE
					var glyph = dynamicFont.GetGlyphByGlyphId(renderer.GraphicsDevice, shapedGlyph.GlyphId, shapedGlyph.FontSourceIndex, effect, effectAmount);
#else
					var glyph = dynamicFont.GetGlyphByGlyphId(renderer.TextureManager, shapedGlyph.GlyphId, shapedGlyph.FontSourceIndex, effect, effectAmount);
#endif

					if (glyph != null && !glyph.IsEmpty)
					{
						var color = source.GetNextColor();
						firstColor = color;

						// Apply HarfBuzz positioning
						var glyphPos = pos + new Vector2(
							glyph.RenderOffset.X + (shapedGlyph.XOffset / 64.0f),
							glyph.RenderOffset.Y + (shapedGlyph.YOffset / 64.0f)
						);

						var size = new Vector2(glyph.Size.X, glyph.Size.Y);
						renderer.DrawQuad(glyph.Texture, color, glyphPos, ref transformation,
							layerDepth, size, glyph.TextureRectangle,
							ref topLeft, ref topRight, ref bottomLeft, ref bottomRight);
					}

					// Use glyph advance from font metrics instead of HarfBuzz advance
					// We use the font's native metrics but keep HarfBuzz's positioning/shaping for complex scripts
					if (glyph != null)
					{
						pos.X += glyph.XAdvance;
						pos.Y += (shapedGlyph.YAdvance / 64.0f);
					}
					else
					{
						// Fallback to HarfBuzz advance if glyph is null
						pos.X += (shapedGlyph.XAdvance / 64.0f);
						pos.Y += (shapedGlyph.YAdvance / 64.0f);
					}
				}

				// Track maximum X position
				if (pos.X > maxX)
				{
					maxX = pos.X;
				}
			}

			// Render text style for the last line if needed
			if (textStyle != TextStyle.None && firstColor != null)
			{
				RenderStyle(renderer, textStyle, pos,
					lineHeight, ascent, firstColor.Value, ref transformation, layerDepth,
					ref topLeft, ref topRight, ref bottomLeft, ref bottomRight);
			}

			return position.X + maxX;
		}

		/// <summary>
		/// Draws a text
		/// </summary>
		/// <param name="renderer">A renderer</param>
		/// <param name="text">The text which will be drawn</param>
		/// <param name="position">The drawing location on screen</param>
		/// <param name="color">A color mask</param>
		/// <param name="rotation">A rotation of this text in radians</param>
		/// <param name="origin">Center of the rotation</param>
		/// <param name="scale">A scaling of this text. Null means the scaling is (1, 1)</param>
		/// <param name="layerDepth">A depth of the layer of this string</param>
		/// <param name="characterSpacing">A character spacing</param>
		/// <param name="lineSpacing">A line spacing</param>
		public float DrawText(IFontStashRenderer2 renderer, string text, Vector2 position, Color color,
			float rotation = 0, Vector2 origin = default(Vector2), Vector2? scale = null,
			float layerDepth = 0.0f, float characterSpacing = 0.0f, float lineSpacing = 0.0f,
			TextStyle textStyle = TextStyle.None, FontSystemEffect effect = FontSystemEffect.None, int effectAmount = 0) =>
				DrawText(renderer, new TextColorSource(text, color), position, rotation, origin, scale, layerDepth,
					characterSpacing, lineSpacing, textStyle, effect, effectAmount);

		/// <summary>
		/// Draws a text
		/// </summary>
		/// <param name="renderer">A renderer</param>
		/// <param name="text">The text which will be drawn</param>
		/// <param name="position">The drawing location on screen</param>
		/// <param name="colors">Colors of glyphs</param>
		/// <param name="rotation">A rotation of this text in radians</param>
		/// <param name="origin">Center of the rotation</param>
		/// <param name="scale">A scaling of this text. Null means the scaling is (1, 1)</param>
		/// <param name="layerDepth">A depth of the layer of this string</param>
		/// <param name="characterSpacing">A character spacing</param>
		/// <param name="lineSpacing">A line spacing</param>
		public float DrawText(IFontStashRenderer2 renderer, string text, Vector2 position, Color[] colors,
			float rotation = 0, Vector2 origin = default(Vector2), Vector2? scale = null,
			float layerDepth = 0.0f, float characterSpacing = 0.0f, float lineSpacing = 0.0f,
			TextStyle textStyle = TextStyle.None, FontSystemEffect effect = FontSystemEffect.None, int effectAmount = 0) =>
				DrawText(renderer, new TextColorSource(text, colors), position, rotation, origin, scale, layerDepth,
					characterSpacing, lineSpacing, textStyle, effect, effectAmount);

		/// <summary>
		/// Draws a text
		/// </summary>
		/// <param name="renderer">A renderer</param>
		/// <param name="text">The text which will be drawn</param>
		/// <param name="position">The drawing location on screen</param>
		/// <param name="color">A color mask</param>
		/// <param name="rotation">A rotation of this text in radians</param>
		/// <param name="origin">Center of the rotation</param>
		/// <param name="scale">A scaling of this text. Null means the scaling is (1, 1)</param>
		/// <param name="layerDepth">A depth of the layer of this string</param>
		/// <param name="characterSpacing">A character spacing</param>
		/// <param name="lineSpacing">A line spacing</param>
		public float DrawText(IFontStashRenderer2 renderer, StringSegment text, Vector2 position, Color color,
			float rotation = 0, Vector2 origin = default(Vector2), Vector2? scale = null,
			float layerDepth = 0.0f, float characterSpacing = 0.0f, float lineSpacing = 0.0f,
			TextStyle textStyle = TextStyle.None, FontSystemEffect effect = FontSystemEffect.None, int effectAmount = 0) =>
				DrawText(renderer, new TextColorSource(text, color), position, rotation, origin, scale, layerDepth,
					characterSpacing, lineSpacing, textStyle, effect, effectAmount);

		/// <summary>
		/// Draws a text
		/// </summary>
		/// <param name="renderer">A renderer</param>
		/// <param name="text">The text which will be drawn</param>
		/// <param name="position">The drawing location on screen</param>
		/// <param name="colors">Colors of glyphs</param>
		/// <param name="rotation">A rotation of this text in radians</param>
		/// <param name="origin">Center of the rotation</param>
		/// <param name="scale">A scaling of this text. Null means the scaling is (1, 1)</param>
		/// <param name="layerDepth">A depth of the layer of this string</param>
		/// <param name="characterSpacing">A character spacing</param>
		/// <param name="lineSpacing">A line spacing</param>
		public float DrawText(IFontStashRenderer2 renderer, StringSegment text, Vector2 position, Color[] colors,
			float rotation = 0, Vector2 origin = default(Vector2), Vector2? scale = null,
			float layerDepth = 0.0f, float characterSpacing = 0.0f, float lineSpacing = 0.0f,
			TextStyle textStyle = TextStyle.None, FontSystemEffect effect = FontSystemEffect.None, int effectAmount = 0) =>
				DrawText(renderer, new TextColorSource(text, colors), position, rotation, origin, scale, layerDepth,
					characterSpacing, lineSpacing, textStyle, effect, effectAmount);

		/// <summary>
		/// Draws a text
		/// </summary>
		/// <param name="renderer">A renderer</param>
		/// <param name="text">The text which will be drawn</param>
		/// <param name="position">The drawing location on screen</param>
		/// <param name="color">A color mask</param>
		/// <param name="rotation">A rotation of this text in radians</param>
		/// <param name="origin">Center of the rotation</param>
		/// <param name="scale">A scaling of this text. Null means the scaling is (1, 1)</param>
		/// <param name="layerDepth">A depth of the layer of this string</param>
		/// <param name="characterSpacing">A character spacing</param>
		/// <param name="lineSpacing">A line spacing</param>
		public float DrawText(IFontStashRenderer2 renderer, StringBuilder text, Vector2 position, Color color,
			float rotation = 0, Vector2 origin = default(Vector2), Vector2? scale = null,
			float layerDepth = 0.0f, float characterSpacing = 0.0f, float lineSpacing = 0.0f,
			TextStyle textStyle = TextStyle.None, FontSystemEffect effect = FontSystemEffect.None, int effectAmount = 0) =>
				DrawText(renderer, new TextColorSource(text, color), position, rotation, origin, scale, layerDepth,
					characterSpacing, lineSpacing, textStyle, effect, effectAmount);

		/// <summary>
		/// Draws a text
		/// </summary>
		/// <param name="renderer">A renderer</param>
		/// <param name="text">The text which will be drawn</param>
		/// <param name="position">The drawing location on screen</param>
		/// <param name="colors">Colors of glyphs</param>
		/// <param name="rotation">A rotation of this text in radians</param>
		/// <param name="origin">Center of the rotation</param>
		/// <param name="scale">A scaling of this text. Null means the scaling is (1, 1)</param>
		/// <param name="layerDepth">A depth of the layer of this string</param>
		/// <param name="characterSpacing">A character spacing</param>
		/// <param name="lineSpacing">A line spacing</param>
		public float DrawText(IFontStashRenderer2 renderer, StringBuilder text, Vector2 position, Color[] colors,
			float rotation = 0, Vector2 origin = default(Vector2), Vector2? scale = null,
			float layerDepth = 0.0f, float characterSpacing = 0.0f, float lineSpacing = 0.0f,
			TextStyle textStyle = TextStyle.None, FontSystemEffect effect = FontSystemEffect.None, int effectAmount = 0) =>
				DrawText(renderer, new TextColorSource(text, colors), position, rotation, origin, scale, layerDepth,
					characterSpacing, lineSpacing, textStyle, effect, effectAmount);
	}
}
