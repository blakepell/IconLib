### IconLib

A library to handle writing to `.ico` files.

### Frameworks Supported

 - .NET 7

### Usage

``` csharp
    public void Convert(string pngPath, string icoPath)
    {
        MultiIcon mIcon = new MultiIcon();
        SingleIcon sIcon = mIcon.Add("Icon1");
        sIcon.CreateFrom(pngPath, IconOutputFormat.FromWin95);
        mIcon.SelectedIndex = 0;
        mIcon.Save(icoPath, MultiIconFormat.ICO);
    }
```

CreateFrom is a method exposed on SingleIcon class, this method will take a input image that must be 256x256 pixels and it must be a 32bpp (alpha channel must be included), the perfect candidate for this method are PNG24 images created for PhotoShop or any Image editing software.

The first parameter can be either the path to the png, or a `Bitmap` object. The second parameter in the API is a flag enumeration that target the OS which we want to create the icon, in the previous example it will take the input image and it will create the following IconImage formats.

256x256x32bpp (PNG compression)
48x48x32bpp 
48x48x8bpp
48x48x4bpp
32x32x32bpp
32x32x8bpp
16x16x32bpp
16x16x8bpp
