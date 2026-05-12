using Xunit;
using System.Linq;

namespace FontStashSharp.Tests
{
	public class StaticSpriteFontTests
	{
		[Fact]
		public void Load()
		{
			var assembly = TestsEnvironment.Assembly;
			var data = assembly.ReadResourceAsString("Resources.arial64.fnt");

			var font = StaticSpriteFont.FromBMFont(data, fileName => assembly.OpenResourceStream("Resources." + fileName), TestsEnvironment.GraphicsDevice);

			Assert.Equal(63, font.FontSize);
			Assert.Equal(191, font.Glyphs.Count);

			var texture = font.Glyphs.First().Value.Texture;

			Assert.NotNull(texture);
			Assert.Equal(512, texture.Width);
			Assert.Equal(512, texture.Height);
		}
	}
}
