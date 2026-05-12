using Microsoft.Xna.Framework.Graphics;
using System;
using System.Reflection;

namespace FontStashSharp.Tests
{
	public class TestsEnvironment
	{
		private static readonly Lazy<TestGame> _gameLazy = new Lazy<TestGame>(() => new TestGame());
		private static FontSystem _defaultFontSystem;

		public static Assembly Assembly => typeof(TestsEnvironment).Assembly;

		public static GraphicsDevice GraphicsDevice => _gameLazy.Value.GraphicsDevice;

		public static FontSystem DefaultFontSystem
		{
			get
			{
				if (_defaultFontSystem == null)
				{
					var fontSystem = CreateDefaultFontSystem(new FontSystemSettings());
					_defaultFontSystem = fontSystem;
				}

				return _defaultFontSystem;
			}
		}

		public static FontSystem CreateDefaultFontSystem(FontSystemSettings settings)
		{
			var fontSystem = new FontSystem(settings);
			fontSystem.AddFont(Assembly.ReadResourceAsBytes("Resources.DroidSans.ttf"));

			return fontSystem;
		}
	}
}
