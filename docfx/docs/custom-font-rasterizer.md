### Overview
By default, FontStashSharp uses [StbTrueTypeSharp](https://github.com/StbSharp/StbTrueTypeSharp) for the font loading & rasterization.

An additional rasterizer is available in a separate assembly: [FontStashSharp.Rasterizers.FreeType](https://www.nuget.org/packages/FontStashSharp.Rasterizers.FreeType/).

### Using FontStashSharp.Rasterizers.FreeType
1. Add reference to [FontStashSharp.Rasterizers.FreeType](https://www.nuget.org/packages/FontStashSharp.Rasterizers.FreeType/)
2. Add following code before the creation of FontSystems:
```c#
FontSystemDefaults.FontLoader = new FreeTypeLoader();
```
3. You might also need to explicitly specify the platform bitness, since the rasterizer uses the native lib.

### Using Custom Font Rasterizers
It's possible to use custom rasterizer instead by implementing [IFontLoader](https://github.com/FontStashSharp/FontStashSharp.Base/blob/main/src/FontStashSharp.Base/IFontLoader.cs) interface.

The implementation should be passed to the FontSystemDefaults before the creation of FontSystems:
```c#
FontSystemDefaults.FontLoader = new MyFontLoader();
```

The [FontStashSharp.Samples.CustomRasterizers sample](https://github.com/rds1983/FontStashSharp/tree/main/samples/FontStashSharp.Samples.CustomRasterizers) demonstrates usage of 2 font rasterizers: StbTrueTypeSharp(default) and FreeType.

![alt text](~/images/custom-font-rasterizer.png)