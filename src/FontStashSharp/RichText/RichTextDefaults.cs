using System;

namespace FontStashSharp.RichText
{
	/// <summary>
	/// Provides default resolvers for fonts and images in rich text.
	/// </summary>
	public static class RichTextDefaults
	{
		/// <summary>
		/// Gets or sets the function used to resolve font names to sprite fonts.
		/// </summary>
		public static Func<string, SpriteFontBase> FontResolver { get; set; }
		/// <summary>
		/// Gets or sets the function used to resolve image names to renderable objects.
		/// </summary>
		public static Func<string, IRenderable> ImageResolver { get; set; }
	}
}
