using FontStashSharp.Interfaces;
using System;
using System.Collections.Generic;


#if MONOGAME || FNA
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
#elif STRIDE
using Stride.Core.Mathematics;
using Stride.Graphics;
#else
using System.Drawing;
using System.Numerics;
#endif

namespace FontStashSharp
{
	public partial class DynamicSpriteFont : SpriteFontBase
	{
		private class GlyphStorage
		{
			public Int32Map<DynamicFontGlyph> Glyphs = new Int32Map<DynamicFontGlyph>();
			public Int32Map<DynamicFontGlyph> ShapedGlyphs = new Int32Map<DynamicFontGlyph>(); // For HarfBuzz glyph IDs
			public FontSystemEffect Effect;
			public int EffectAmount;
		}

		private struct ShapedTextCacheKey : IEquatable<ShapedTextCacheKey>
		{
			public readonly string Text;
			public readonly float FontSize;

			public ShapedTextCacheKey(string text, float fontSize)
			{
				Text = text;
				FontSize = fontSize;
			}

			public bool Equals(ShapedTextCacheKey other)
			{
				return Text == other.Text && FontSize.Equals(other.FontSize);
			}

			public override bool Equals(object obj)
			{
				return obj is ShapedTextCacheKey other && Equals(other);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					return ((Text?.GetHashCode() ?? 0) * 397) ^ FontSize.GetHashCode();
				}
			}
		}

		private class ShapedTextCache
		{
			private readonly Dictionary<ShapedTextCacheKey, LinkedListNode<(ShapedTextCacheKey Key, HarfBuzz.ShapedText Value)>> _cache =
				new Dictionary<ShapedTextCacheKey, LinkedListNode<(ShapedTextCacheKey Key, HarfBuzz.ShapedText Value)>>();
			private readonly LinkedList<(ShapedTextCacheKey Key, HarfBuzz.ShapedText Value)> _lruList =
				new LinkedList<(ShapedTextCacheKey Key, HarfBuzz.ShapedText Value)>();
			private readonly int _maxCacheEntries;

			public ShapedTextCache(int maxCacheEntries)
			{
				_maxCacheEntries = maxCacheEntries;
			}

			public bool TryGet(ShapedTextCacheKey key, out HarfBuzz.ShapedText shapedText)
			{
				if (_cache.TryGetValue(key, out var node))
				{
					// Move to front (most recently used)
					_lruList.Remove(node);
					_lruList.AddFirst(node);
					shapedText = node.Value.Value;
					return true;
				}

				shapedText = null;
				return false;
			}

			public void Add(ShapedTextCacheKey key, HarfBuzz.ShapedText shapedText)
			{
				// Check if already exists
				if (_cache.ContainsKey(key))
				{
					return;
				}

				// Evict oldest entry if cache is full
				if (_cache.Count >= _maxCacheEntries)
				{
					var oldest = _lruList.Last;
					if (oldest != null)
					{
						_cache.Remove(oldest.Value.Key);
						_lruList.RemoveLast();
					}
				}

				// Add new entry
				var node = _lruList.AddFirst((key, shapedText));
				_cache[key] = node;
			}

			public void Clear()
			{
				_cache.Clear();
				_lruList.Clear();
			}
		}

		private readonly Int32Map<GlyphStorage> _storages = new Int32Map<GlyphStorage>();
		private GlyphStorage _lastStorage;
		private readonly Int32Map<int> Kernings = new Int32Map<int>();
		private FontMetrics[] IndexedMetrics;
		private readonly ShapedTextCache _shapedTextCache;

		public FontSystem FontSystem { get; private set; }

		internal DynamicSpriteFont(FontSystem system, float size, int lineHeight) : base(size, lineHeight)
		{
			if (system == null)
			{
				throw new ArgumentNullException(nameof(system));
			}

			FontSystem = system;
			RenderFontSizeMultiplicator = FontSystem.FontResolutionFactor;

			// Initialize shaped text cache with configured size from FontSystem settings
			_shapedTextCache = new ShapedTextCache(system.ShapedTextCacheSize);
		}

		internal Int32Map<DynamicFontGlyph> GetGlyphs(FontSystemEffect effect, int effectAmount)
		{
			return GetGlyphStorage(effect, effectAmount).Glyphs;
		}

		private GlyphStorage GetGlyphStorage(FontSystemEffect effect, int effectAmount)
		{
			if (_lastStorage != null && _lastStorage.Effect == effect && _lastStorage.EffectAmount == effectAmount)
			{
				return _lastStorage;
			}

			var key = (int)effect << 16 | effectAmount;

			GlyphStorage result;
			if (!_storages.TryGetValue(key, out result))
			{
				result = new GlyphStorage
				{
					Effect = effect,
					EffectAmount = effectAmount
				};

				_storages[key] = result;
			}

			_lastStorage = result;

			return result;
		}

		private DynamicFontGlyph GetGlyphWithoutBitmap(int codepoint, FontSystemEffect effect, int effectAmount)
		{
			if (effect == FontSystemEffect.None)
			{
			}
			else if (effectAmount == 0)
			{
				effect = FontSystemEffect.None;
			}

			var storage = GetGlyphs(effect, effectAmount);

			DynamicFontGlyph glyph;
			if (storage.TryGetValue(codepoint, out glyph))
			{
				return glyph;
			}

			int fontSourceIndex;
			var g = FontSystem.GetCodepointIndex(codepoint, out fontSourceIndex);
			if (g == null)
			{
				storage[codepoint] = null;
				return null;
			}

			var fontSize = FontSize * FontSystem.FontResolutionFactor;
			var font = FontSystem.FontSources[fontSourceIndex];

			int advance, x0, y0, x1, y1;
			font.GetGlyphMetrics(g.Value, fontSize, out advance, out x0, out y0, out x1, out y1);

			var gw = x1 - x0 + effectAmount * 2;
			var gh = y1 - y0 + effectAmount * 2;

			glyph = new DynamicFontGlyph
			{
				Codepoint = codepoint,
				Id = g.Value,
				FontSize = fontSize,
				FontSourceIndex = fontSourceIndex,
				RenderOffset = new Point(x0, y0),
				Size = new Point(gw, gh),
				XAdvance = advance,
				Effect = effect,
				EffectAmount = effectAmount
			};

			storage[codepoint] = glyph;

			return glyph;
		}

#if MONOGAME || FNA || STRIDE
		private DynamicFontGlyph GetGlyphInternal(GraphicsDevice device, int codepoint, FontSystemEffect effect, int effectAmount)
#else
		private DynamicFontGlyph GetGlyphInternal(ITexture2DManager device, int codepoint, FontSystemEffect effect, int effectAmount)
#endif
		{
			var glyph = GetGlyphWithoutBitmap(codepoint, effect, effectAmount);
			if (glyph == null)
			{
				return null;
			}

			if (device == null || glyph.Texture != null)
				return glyph;

			FontSystem.RenderGlyphOnAtlas(device, glyph);

			return glyph; 
		}

#if MONOGAME || FNA || STRIDE
		private DynamicFontGlyph GetDynamicGlyph(GraphicsDevice device, int codepoint, FontSystemEffect effect, int effectAmount)
#else
		private DynamicFontGlyph GetDynamicGlyph(ITexture2DManager device, int codepoint, FontSystemEffect effect, int effectAmount)
#endif
		{
			var result = GetGlyphInternal(device, codepoint, effect, effectAmount);
			if (result == null && FontSystem.DefaultCharacter != null)
			{
				result = GetGlyphInternal(device, FontSystem.DefaultCharacter.Value, effect, effectAmount);
			}

			return result;
		}

		/// <summary>
		/// Get a glyph by its glyph ID (for HarfBuzz shaped text)
		/// </summary>
#if MONOGAME || FNA || STRIDE
		internal DynamicFontGlyph GetGlyphByGlyphId(GraphicsDevice device, int glyphId, int fontSourceIndex, FontSystemEffect effect, int effectAmount)
#else
		internal DynamicFontGlyph GetGlyphByGlyphId(ITexture2DManager device, int glyphId, int fontSourceIndex, FontSystemEffect effect, int effectAmount)
#endif
		{
			if (effect == FontSystemEffect.None)
			{
			}
			else if (effectAmount == 0)
			{
				effect = FontSystemEffect.None;
			}

			var storage = GetGlyphStorage(effect, effectAmount);

			// Create a key that combines glyph ID and font source index
			var key = (fontSourceIndex << 24) | glyphId;

			DynamicFontGlyph glyph;
			if (storage.ShapedGlyphs.TryGetValue(key, out glyph))
			{
				// Render to atlas if device is available and glyph hasn't been rendered yet
				if (device != null && glyph.Texture == null)
				{
					FontSystem.RenderGlyphOnAtlas(device, glyph);
				}
				return glyph;
			}

			// Get glyph metrics from font source
			var fontSize = FontSize * FontSystem.FontResolutionFactor;
			var font = FontSystem.FontSources[fontSourceIndex];

			int advance, x0, y0, x1, y1;
			font.GetGlyphMetrics(glyphId, fontSize, out advance, out x0, out y0, out x1, out y1);

			var gw = x1 - x0 + effectAmount * 2;
			var gh = y1 - y0 + effectAmount * 2;

			glyph = new DynamicFontGlyph
			{
				Codepoint = 0, // Not applicable for shaped glyphs
				Id = glyphId,
				FontSize = fontSize,
				FontSourceIndex = fontSourceIndex,
				RenderOffset = new Point(x0, y0),
				Size = new Point(gw, gh),
				XAdvance = advance,
				Effect = effect,
				EffectAmount = effectAmount
			};

			storage.ShapedGlyphs[key] = glyph;

			// Render to atlas if device is available
			if (device != null && glyph.Texture == null)
			{
				FontSystem.RenderGlyphOnAtlas(device, glyph);
			}

			return glyph;
		}

#if MONOGAME || FNA || STRIDE
		protected internal override FontGlyph GetGlyph(GraphicsDevice device, int codepoint, FontSystemEffect effect, int effectAmount)
#else
		protected internal override FontGlyph GetGlyph(ITexture2DManager device, int codepoint, FontSystemEffect effect, int effectAmount)
#endif
		{
			return GetDynamicGlyph(device, codepoint, effect, effectAmount);
		}

		private void GetMetrics(int fontSourceIndex, out FontMetrics result)
		{
			if (IndexedMetrics == null || IndexedMetrics.Length != FontSystem.FontSources.Count)
			{
				IndexedMetrics = new FontMetrics[FontSystem.FontSources.Count];
				for (var i = 0; i < IndexedMetrics.Length; ++i)
				{
					int ascent, descent, lineHeight;
					FontSystem.FontSources[i].GetMetricsForSize(FontSize * RenderFontSizeMultiplicator, out ascent, out descent, out lineHeight);

					IndexedMetrics[i] = new FontMetrics(ascent, descent, lineHeight);
				}
			}

			result = IndexedMetrics[fontSourceIndex];
		}

		internal override void PreDraw(TextSource source, FontSystemEffect effect, int effectAmount,
			out int ascent, out int lineHeight)
		{
			// Determine ascent and lineHeight from first character
			ascent = 0;
			lineHeight = 0;
			while (true)
			{
				int codepoint;
				if (!source.GetNextCodepoint(out codepoint))
				{
					break;
				}

				var glyph = GetDynamicGlyph(null, codepoint, effect, effectAmount);
				if (glyph == null)
				{
					continue;
				}

				FontMetrics metrics;
				GetMetrics(glyph.FontSourceIndex, out metrics);
				ascent = metrics.Ascent;
				lineHeight = metrics.LineHeight;
				break;
			}

			source.Reset();
		}

		internal override Bounds InternalTextBounds(TextSource source, Vector2 position,
			float characterSpacing, float lineSpacing, FontSystemEffect effect, int effectAmount)
		{
			if (source.IsNull)
				return Bounds.Empty;

			// Use HarfBuzz shaping for measurement if enabled
			if (FontSystem.UseTextShaping)
			{
				return InternalShapedTextBounds(source, position, characterSpacing, lineSpacing, effect, effectAmount);
			}

			var result = base.InternalTextBounds(source, position, characterSpacing, lineSpacing, effect, effectAmount);
			if (effect != FontSystemEffect.None)
			{
				result.X2 += effectAmount * 2;
			}

			return result;
		}

		private Bounds InternalShapedTextBounds(TextSource source, Vector2 position,
			float characterSpacing, float lineSpacing, FontSystemEffect effect, int effectAmount)
		{
			// Get the text from source
			var text = source.StringText.String ?? source.StringBuilderText?.ToString();
			if (string.IsNullOrEmpty(text))
			{
				return Bounds.Empty;
			}

			// Split text into lines
			var lines = text.Split('\n');

			// Get metrics for line height
			int ascent = 0, lineHeight = 0;
			if (FontSystem.FontSources.Count > 0)
			{
				int descent, lh;
				FontSystem.FontSources[0].GetMetricsForSize(FontSize * FontSystem.FontResolutionFactor, out ascent, out descent, out lh);
				lineHeight = lh;
			}

			var x = position.X;
			var y = position.Y + ascent;

			float minx = x, maxx = x, miny = y, maxy = y;

			for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
			{
				var line = lines[lineIndex];

				if (lineIndex > 0)
				{
					x = position.X;
					y += lineHeight + lineSpacing;
				}

				if (string.IsNullOrEmpty(line))
				{
					continue;
				}

				// Shape the line
				var shapedText = GetShapedText(line, FontSize);

				float lineStartX = x;
				for (int i = 0; i < shapedText.Glyphs.Length; i++)
				{
					var shapedGlyph = shapedText.Glyphs[i];

					// Add character spacing between glyphs
					if (i > 0 && characterSpacing > 0)
					{
						x += characterSpacing;
					}

					// Get the glyph (without rendering to atlas)
					var glyph = GetGlyphByGlyphId(null, shapedGlyph.GlyphId, shapedGlyph.FontSourceIndex, effect, effectAmount);

					if (glyph != null)
					{
						// Calculate glyph bounds with HarfBuzz positioning
						var glyphX = x + glyph.RenderOffset.X + (shapedGlyph.XOffset / 64.0f) * FontSystem.FontResolutionFactor;
						var glyphY = y + glyph.RenderOffset.Y + (shapedGlyph.YOffset / 64.0f) * FontSystem.FontResolutionFactor;
						var glyphX2 = glyphX + glyph.Size.X;
						var glyphY2 = glyphY + glyph.Size.Y;

						if (glyphX < minx) minx = glyphX;
						if (glyphY < miny) miny = glyphY;
						if (glyphX2 > maxx) maxx = glyphX2;
						if (glyphY2 > maxy) maxy = glyphY2;
					}

					// Use glyph advance from font metrics instead of HarfBuzz advance
					// HarfBuzz advances can be incorrect for some fonts, so we use the font's native metrics
					// but keep HarfBuzz's positioning/shaping for complex scripts
					if (glyph != null)
					{
						x += glyph.XAdvance;
						y += (shapedGlyph.YAdvance / 64.0f) * FontSystem.FontResolutionFactor;
					}
					else
					{
						// Fallback to HarfBuzz advance if glyph is null
						x += (shapedGlyph.XAdvance / 64.0f) * FontSystem.FontResolutionFactor;
						y += (shapedGlyph.YAdvance / 64.0f) * FontSystem.FontResolutionFactor;
					}
				}

				// Update maxx with final x position
				if (x > maxx) maxx = x;
			}

			if (effect != FontSystemEffect.None)
			{
				maxx += effectAmount * 2;
			}

			return new Bounds(minx, miny, maxx, maxy);
		}

		private static int GetKerningsKey(int glyph1, int glyph2)
		{
			return ((glyph1 << 16) | (glyph1 >> 16)) ^ glyph2;
		}

		internal override float GetKerning(FontGlyph glyph, FontGlyph prevGlyph)
		{
			if (!FontSystem.UseKernings)
			{
				return 0.0f;
			}


			var dynamicGlyph = (DynamicFontGlyph)glyph;
			var dynamicPrevGlyph = (DynamicFontGlyph)prevGlyph;
			if (dynamicGlyph.FontSourceIndex != dynamicPrevGlyph.FontSourceIndex)
			{
				return 0.0f;
			}

			var key = GetKerningsKey(prevGlyph.Id, dynamicGlyph.Id);
			var result = 0;
			if (!Kernings.TryGetValue(key, out result))
			{
				var fontSource = FontSystem.FontSources[dynamicGlyph.FontSourceIndex];
				result = fontSource.GetGlyphKernAdvance(prevGlyph.Id, dynamicGlyph.Id, dynamicGlyph.FontSize);

				Kernings[key] = result;
			}

			return result;
		}

		/// <summary>
		/// Shape text using HarfBuzz with caching
		/// </summary>
		internal HarfBuzz.ShapedText GetShapedText(string text, float fontSize)
		{
			if (string.IsNullOrEmpty(text))
			{
				return new HarfBuzz.ShapedText
				{
					Glyphs = new HarfBuzz.ShapedGlyph[0],
					OriginalText = text ?? string.Empty,
					FontSize = fontSize
				};
			}

			var cacheKey = new ShapedTextCacheKey(text, fontSize);

			// Try to get from cache
			if (_shapedTextCache.TryGet(cacheKey, out var cachedResult))
			{
				return cachedResult;
			}

			// Not in cache, shape the text
			var shapedText = FontSystem.ShapeText(text, fontSize);

			// Add to cache
			_shapedTextCache.Add(cacheKey, shapedText);

			return shapedText;
		}

		/// <summary>
		/// Clear the shaped text cache (call when fonts are added/removed)
		/// </summary>
		internal void ClearShapedTextCache()
		{
			_shapedTextCache.Clear();
		}
	}
}