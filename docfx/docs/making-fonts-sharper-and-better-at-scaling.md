`FontSystemDefaults` contains 3 parameters that can be used to make fonts sharper and better at scaling: `FontResolutionFactor`, `KernelWidth`, and `KernelHeight`.

`FontResolutionFactor` (default value 1.0f) contains the scale at which glyphs are rendered to the texture atlas. For example, setting it to 2.0f will make the font better at scaling. However, this approach has a downside as well—font glyphs occupy more space in the texture atlas and thus it runs out of space faster.

`KernelWidth`/`KernelHeight` (default value is 0) are passed to stb_truetype methods `stbtt__h_prefilter`/`stbtt__v_prefilter`.

Empirically, it has been discovered that setting all three properties `FontResolutionFactor`, `KernelWidth`, and `KernelHeight` to 2 results in fonts becoming significantly better at scaling:
```c#
FontSystemDefaults.FontResolutionFactor = 2.0f;
FontSystemDefaults.KernelWidth = 2;
FontSystemDefaults.KernelHeight = 2;
```

There is also a sample [FontStashSharp.Samples.Scaling](https://github.com/rds1983/FontStashSharp/tree/main/samples/FontStashSharp.Samples.Scaling) that can be used to tune these properties:
[[images/Scaling.png]]


