//  Copyright (c) 2006, Gustavo Franco
//  Email:  gustavo_franco@hotmail.com
//  All rights reserved.

//  Redistribution and use in source and binary forms, with or without modification, 
//  are permitted provided that the following conditions are met:

//  Redistributions of source code must retain the above copyright notice, 
//  this list of conditions and the following disclaimer. 
//  Redistributions in binary form must reproduce the above copyright notice, 
//  this list of conditions and the following disclaimer in the documentation 
//  and/or other materials provided with the distribution. 

//  THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
//  KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//  IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR
//  PURPOSE. IT CAN BE DISTRIBUTED FREE OF CHARGE AS LONG AS THIS HEADER 
//  REMAINS UNCHANGED.

using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using IconLib.ColorProcessing;
using IconLib.Exceptions;
using IconLib.LibraryFormats;

namespace IconLib
{
    public class SingleIcon : IEnumerable<IconImage>
    {
        #region Variables Declaration

        private string mName = "";
        private List<IconImage> mIconImages = new();

        #endregion

        #region Constructors

        internal SingleIcon(string name)
        {
            mName = name;
        }

        #endregion

        #region Properties

        public int Count => mIconImages.Count;

        public string Name
        {
            get => mName;
            set => mName = value != null ? value : string.Empty;
        }

        public Icon Icon
        {
            get
            {
                if (mIconImages.Count == 0)
                {
                    return null;
                }

                var ms = new MemoryStream();
                this.Save(ms);
                ms.Position = 0;
                var icon = new Icon(ms);
                return icon;
            }
        }

        #endregion

        #region Public Methods

        public void Clear()
        {
            mIconImages.Clear();
        }

        public IconImage RemoveAt(int index)
        {
            if (index < 0 || index >= mIconImages.Count)
            {
                return null;
            }

            var iconImage = mIconImages[index];

            mIconImages.RemoveAt(index);

            return iconImage;
        }

        public IEnumerator<IconImage> GetEnumerator()
        {
            return new Enumerator(this);
        }

        public void Load(string fileName)
        {
            var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            try
            {
                this.Load(fs);
            }
            finally
            {
                if (fs != null)
                {
                    fs.Close();
                }
            }
        }

        public void Load(Stream stream)
        {
            var iconFormat = new IconFormat();
            if (!iconFormat.IsRecognizedFormat(stream))
            {
                throw new InvalidFileException();
            }

            var multiIcon = iconFormat.Load(stream);
            if (multiIcon.Count < 1)
            {
                return;
            }

            this.CopyFrom(multiIcon[0]);
        }

        public void Save(string fileName)
        {
            var fs = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite);
            try
            {
                this.Save(fs);
            }
            finally
            {
                if (fs != null)
                {
                    fs.Close();
                }
            }
        }

        public void Save(Stream stream)
        {
            new IconFormat().Save(new MultiIcon(this), stream);
        }

        public IconImage Add(Bitmap bitmap)
        {
            if (bitmap == null)
            {
                throw new ArgumentNullException(nameof(bitmap));
            }

            if (bitmap.PixelFormat == PixelFormat.Format32bppArgb || bitmap.PixelFormat == PixelFormat.Format32bppPArgb)
            {
                var iconImage = this.Add(bitmap, null, Color.Transparent);
                if (bitmap.RawFormat.Guid == ImageFormat.Png.Guid)
                {
                    iconImage.IconImageFormat = IconImageFormat.PNG;
                }

                return iconImage;
            }

            return this.Add(bitmap, null, bitmap.GetPixel(0, 0));
        }

        public IconImage Add(Bitmap bitmap, Color transparentColor)
        {
            return this.Add(bitmap, null, transparentColor);
        }

        public IconImage Add(Bitmap bitmap, Bitmap bitmapMask)
        {
            if (bitmapMask == null)
            {
                throw new ArgumentNullException(nameof(bitmapMask));
            }

            return this.Add(bitmap, bitmapMask, Color.Empty);
        }

        public IconImage Add(Icon icon)
        {
            if (icon == null)
            {
                throw new ArgumentNullException(nameof(icon));
            }

            bool bResult = Win32.GetIconInfo(icon.Handle, out var iconInfo);

            if (!bResult)
            {
                throw new InvalidMultiIconFileException();
            }

            Bitmap XORImage = null;
            Bitmap ANDImage = null;
            try
            {
                XORImage = Image.FromHbitmap(iconInfo.hbmColor);
                ANDImage = Image.FromHbitmap(iconInfo.hbmMask);

                // Bitmap.FromHbitmap will give a DDB and not a DIB, if the screen is 16 bits Icon with 16bits are not supported
                // then make them XP format Icons
                if (Tools.BitsFromPixelFormat(XORImage.PixelFormat) == 16)
                {
                    XORImage.Dispose();
                    ANDImage.Dispose();

                    return this.Add(icon.ToBitmap(), Color.Transparent);
                }
                else
                {
                    return this.Add(XORImage, ANDImage, Color.Empty);
                }
            }
            finally
            {
                if (XORImage != null)
                {
                    XORImage.Dispose();
                }

                if (ANDImage != null)
                {
                    ANDImage.Dispose();
                }
            }
        }

        private Bitmap CreateSmoothBitmap(Bitmap bmp, int width, int height)
        {
            var newBitmap = new Bitmap(width, height);
            var g = Graphics.FromImage(newBitmap);
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.DrawImage(bmp, new Rectangle(0, 0, newBitmap.Width, newBitmap.Height),
                new Rectangle(0, 0, bmp.Width, bmp.Height), GraphicsUnit.Pixel);
            g.Dispose();
            return newBitmap;
        }

        public void CreateFrom(string fileName)
        {
            this.CreateFrom(fileName, IconOutputFormat.FromWin95);
        }

        public void CreateFrom(string fileName, IconOutputFormat format)
        {
            var bmp = (Bitmap)Image.FromFile(fileName);

            if (bmp == null)
            {
                throw new InvalidFileException();
            }

            try
            {
                this.CreateFrom(bmp, format);
            }
            finally
            {
                bmp.Dispose();
            }
        }

        public void CreateFrom(Bitmap bitmap)
        {
            this.CreateFrom(bitmap, IconOutputFormat.FromWin95);
        }

        public void CreateFrom(Bitmap bitmap, IconOutputFormat format)
        {
            Bitmap bmp = null;
            IconImage iconImage = null;
            IColorQuantizer colorQuantizer = new EuclideanQuantizer();

            if (bitmap.PixelFormat != PixelFormat.Format32bppArgb)
            {
                throw new InvalidPixelFormatException(PixelFormat.Undefined, PixelFormat.Format32bppArgb);
            }

            mIconImages.Clear();

            // Vista
            // 256x256x32
            if ((format & IconOutputFormat.Vista) == IconOutputFormat.Vista)
            {
                this.Add(bitmap);
            }

            if ((format & IconOutputFormat.WinXPUnpopular) == IconOutputFormat.WinXPUnpopular)
            {
                bmp = new Bitmap(bitmap, 64, 64);
                iconImage = this.Add(bmp); // XP
                bmp.Dispose();
            }

            bmp = new Bitmap(bitmap, 48, 48);
            if ((format & IconOutputFormat.WinXP) == IconOutputFormat.WinXP)
            {
                iconImage = this.Add(bmp); // XP
            }

            if ((format & IconOutputFormat.Win95) == IconOutputFormat.Win95)
            {
                this.Add(colorQuantizer.Convert(bmp, PixelFormat.Format8bppIndexed), iconImage.Mask); // W95
            }

            if ((format & IconOutputFormat.Win31) == IconOutputFormat.Win31)
            {
                this.Add(colorQuantizer.Convert(bmp, PixelFormat.Format4bppIndexed), iconImage.Mask); // W95
            }

            bmp.Dispose();

            bmp = new Bitmap(bitmap, 32, 32);
            if ((format & IconOutputFormat.WinXP) == IconOutputFormat.WinXP)
            {
                iconImage = this.Add(bmp); // XP
            }

            if ((format & IconOutputFormat.Win95) == IconOutputFormat.Win95)
            {
                this.Add(colorQuantizer.Convert(bmp, PixelFormat.Format8bppIndexed), iconImage.Mask); // W95
            }

            if ((format & IconOutputFormat.Win31) == IconOutputFormat.Win31)
            {
                this.Add(colorQuantizer.Convert(bmp, PixelFormat.Format4bppIndexed), iconImage.Mask); // W31
            }

            if ((format & IconOutputFormat.Win30) == IconOutputFormat.Win30)
            {
                this.Add(colorQuantizer.Convert(bmp, PixelFormat.Format1bppIndexed), iconImage.Mask); // W30
            }

            bmp.Dispose();

            bmp = new Bitmap(bitmap, 24, 24);
            if ((format & IconOutputFormat.WinXPUnpopular) == IconOutputFormat.WinXPUnpopular)
            {
                iconImage = this.Add(bmp); // XP
            }

            if ((format & IconOutputFormat.Win95Unpopular) == IconOutputFormat.Win95Unpopular)
            {
                this.Add(colorQuantizer.Convert(bmp, PixelFormat.Format8bppIndexed), iconImage.Mask); // W95
            }

            if ((format & IconOutputFormat.Win31Unpopular) == IconOutputFormat.Win31Unpopular)
            {
                this.Add(colorQuantizer.Convert(bmp, PixelFormat.Format4bppIndexed), iconImage.Mask); // W31
            }

            if ((format & IconOutputFormat.Win30) == IconOutputFormat.Win30)
            {
                this.Add(colorQuantizer.Convert(bmp, PixelFormat.Format1bppIndexed), iconImage.Mask); // W30
            }

            bmp.Dispose();

            bmp = new Bitmap(bitmap, 16, 16);
            if ((format & IconOutputFormat.WinXP) == IconOutputFormat.WinXP)
            {
                iconImage = this.Add(bmp); // XP
            }

            if ((format & IconOutputFormat.Win95) == IconOutputFormat.Win95)
            {
                this.Add(colorQuantizer.Convert(bmp, PixelFormat.Format8bppIndexed), iconImage.Mask); // W95
            }

            if ((format & IconOutputFormat.Win31) == IconOutputFormat.Win31)
            {
                this.Add(colorQuantizer.Convert(bmp, PixelFormat.Format4bppIndexed), iconImage.Mask); // W31
            }

            if ((format & IconOutputFormat.Win30) == IconOutputFormat.Win30)
            {
                this.Add(colorQuantizer.Convert(bmp, PixelFormat.Format1bppIndexed), iconImage.Mask); // W30
            }

            bmp.Dispose();
        }

        internal IconImage Add(IconImage iconImage)
        {
            mIconImages.Add(iconImage);

            return iconImage;
        }

        internal void CopyFrom(SingleIcon singleIcon)
        {
            mName = singleIcon.mName;
            mIconImages = singleIcon.mIconImages;
        }

        #endregion

        #region Private Methods

        private IconImage Add(Bitmap bitmap, Bitmap bitmapMask, Color transparentColor)
        {
            if (bitmap == null)
            {
                throw new ArgumentNullException(nameof(bitmap));
            }

            if (this.IndexOf(bitmap.Size, Tools.BitsFromPixelFormat(bitmap.PixelFormat)) != -1)
            {
                throw new ImageAlreadyExistsException();
            }

            if (bitmap.Width > 256 || bitmap.Height > 256)
            {
                throw new ImageTooBigException();
            }

            var iconImage = new IconImage();
            iconImage.Set(bitmap, bitmapMask, transparentColor);
            mIconImages.Add(iconImage);

            return iconImage;
        }

        private int IndexOf(Size size, int bitCount)
        {
            for (int i = 0; i < this.Count; i++)
            {
                if (this[i].Size == size && Tools.BitsFromPixelFormat(this[i].PixelFormat) == bitCount)
                {
                    return i;
                }
            }

            return -1;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion

        #region Overrides

        public override string ToString()
        {
            return this.Name;
        }

        #endregion

        #region Indexers

        public IconImage this[int index] => mIconImages[index];

        #endregion

        #region Helper Classes

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct Enumerator : IEnumerator<IconImage>, IDisposable, IEnumerator
        {
            #region Variables Declaration

            private SingleIcon mList;
            private int mIndex;
            private IconImage mCurrent;

            #endregion

            #region Constructors

            internal Enumerator(SingleIcon list)
            {
                mList = list;
                mIndex = 0;
                mCurrent = null;
            }

            #endregion

            #region Properties

            public IconImage Current => mCurrent;

            #endregion

            #region Methods

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (mIndex < mList.Count)
                {
                    mCurrent = mList[mIndex];
                    mIndex++;
                    return true;
                }

                mIndex = mList.Count + 1;
                mCurrent = null;
                return false;
            }

            object IEnumerator.Current => this.Current;

            void IEnumerator.Reset()
            {
                mIndex = 0;
                mCurrent = null;
            }

            #endregion
        }

        #endregion
    }
}