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

using System.Drawing;
using System.Drawing.Imaging;

namespace IconLib.BitmapEncoders
{
    internal class PNGEncoder : ImageEncoder
    {
        public override IconImageFormat IconImageFormat => IconImageFormat.PNG;

        public override int ImageSize
        {
            get
            {
                // This is a fast and temporary solution,
                // Soon Ill implement a png cache, 
                // then the image will be generated just once between calls and writes
                MemoryStream ms = new MemoryStream();
                Icon.ToBitmap().Save(ms, ImageFormat.Png);
                return (int)ms.Length;
            }
        }

        public override void Read(Stream stream, int resourceSize)
        {
            // Buffer a PNG image
            byte[] buffer = new byte[resourceSize];
            _ = stream.Read(buffer, 0, buffer.Length);
            MemoryStream ms = new MemoryStream(buffer);
            Bitmap pngBitmap = new Bitmap(ms);

            // Set XOR and AND Image
            IconImage iconImage = new IconImage();
            iconImage.Set(pngBitmap, null, Color.Transparent);
            pngBitmap.Dispose();

            //Transfer the data from the BMPEncoder to the PNGEncoder
            CopyFrom(iconImage.Encoder);
        }

        public override void Write(Stream stream)
        {
            MemoryStream ms = new MemoryStream();
            Icon.ToBitmap().Save(ms, ImageFormat.Png);
            byte[] buffer = ms.GetBuffer();
            stream.Write(buffer, 0, (int)ms.Length);
        }
    }
}
