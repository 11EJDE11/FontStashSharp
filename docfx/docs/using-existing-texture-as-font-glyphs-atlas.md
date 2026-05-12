Sometimes it makes sense to use existing texture as font glyphs atlas.

For example, if you make a GUI library that uses FontStashSharp, it makes sense to pass an existing texture that holds GUI images to the `FontSystem`. This approach is good for performance because it minimizes the amount of texture swaps, since both GUI images and glyphs will be stored on a single texture.

This can be achieved using the following code:
```c#
  var settings = new FontSystemSettings
  {
    ExistingTexture = texture,
    ExistingTextureUsedSpace = new Rectangle(0, 0, 160, 1024) // Rectangle that covers area already used by the GUI images
  };

  var fontSystem = new FontSystem(settings);
```

Now this `fontSystem` will use `texture` to store the font glyphs. It'll place them outside of the `ExistingTextureUsedSpace`.
If the space on the texture will run out, then FontStashSharp will create new texture of same size and place the new glyphs there.