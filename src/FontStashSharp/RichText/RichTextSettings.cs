using System;

namespace FontStashSharp.RichText
{
	/// <summary>
	/// Configuration settings for rich text rendering.
	/// </summary>
	public class RichTextSettings
	{
		/// <summary>
		/// Gets or sets the function used to resolve font names to sprite fonts.
		/// </summary>
		public Func<string, SpriteFontBase> FontResolver { get; set; }
		/// <summary>
		/// Gets or sets the function used to resolve image names to renderable objects.
		/// </summary>
		public Func<string, IRenderable> ImageResolver { get; set; }

		/// <summary>
		/// Initializes a new instance of the RichTextSettings class with default values.
		/// </summary>
		public RichTextSettings()
		{
			FontResolver = RichTextDefaults.FontResolver;
			ImageResolver = RichTextDefaults.ImageResolver;
		}
	}
}