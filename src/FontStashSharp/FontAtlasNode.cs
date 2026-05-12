using System.Runtime.InteropServices;

namespace FontStashSharp
{
	/// <summary>
	/// Represents a node in the font atlas skyline for bin-packing glyphs.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	internal struct FontAtlasNode
	{
		/// <summary>
		/// The X coordinate of the node.
		/// </summary>
		public int X;

		/// <summary>
		/// The Y coordinate of the node.
		/// </summary>
		public int Y;

		/// <summary>
		/// The width of the node.
		/// </summary>
		public int Width;
	}
}
