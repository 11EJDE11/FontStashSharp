FontStashSharp renders glyphs on demand to the underlying texture atlas.
This means that when a glyph with the specified codepoint and size is rendered for the first time, the texture atlas is updated with it.

This animation demonstrates how a sample texture atlas is being updated during the run-time:
![alt text](~/images/dealing-with-texture-atlas-overflow.gif)

If there is no more space on the texture atlas, a new one is created. `FontSystem` has a special event `CurrentAtlasFull` that is fired when that happens. 

`FontSystem`'s texture atlases can be accessed through the `Atlases` property:
```c#
foreach (var atlas in fontSystem.Atlases)
{
  Texture2D texture = atlas.Texture;
  // Use the texture
}
```

Unfortunately, if a `FontSystem` has multiple textures, then the rendering performance would slightly go down, since it would need to swap between different textures.

One way of addressing the performance drop would be to reset(remove all texture atlases) `FontSystem` on `CurrentAtlasFull`:
```
  fontSystem.CurrentAtlasFull += (e, a) => fontSystem.Reset();
```
This approach would require FontSystem to start filling the texture atlas from scratch; however, it would eliminate the texture swaps.

