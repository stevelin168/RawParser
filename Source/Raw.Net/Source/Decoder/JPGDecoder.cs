﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace RawNet
{
    // Using the COM interface IMemoryBufferByteAccess allows us to access the underlying byte array in an AudioFrame
    [ComImport]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }

    /*
     * This will decode all image supportedby the windows parser 
     * Should be a last resort parser
     */
    internal class JPGDecoder : RawDecoder
    {
        BitmapPropertiesView meta;

        public JPGDecoder(ref Stream file) : base(ref file)
        {
        }

        protected override void checkSupportInternal()
        {            

        }

        protected override void decodeMetaDataInternal()
        {
            //fill useless metadata
            rawImage.metadata.wbCoeffs = new float[] { 1, 1, 1, 1 };
            List<string> list = new List<string>();
            list.Add("/app1/ifd/{ushort=271}");
            var metaList = meta.GetPropertiesAsync(list);
            metaList.AsTask().Wait();
            if (metaList.GetResults() != null)
            {
                metaList.GetResults().TryGetValue("", out var make);
                rawImage.metadata.make = make?.Value.ToString();
            }
        }

        protected override void decodeRawInternal()
        {
            rawImage.ColorDepth = 8;
            rawImage.cpp = 3;
            rawImage.bpp = 8;
            var decoder = BitmapDecoder.CreateAsync(BitmapDecoder.JpegDecoderId, stream.AsRandomAccessStream()).AsTask();

            decoder.Wait();

            var bitmapasync = decoder.Result.GetSoftwareBitmapAsync().AsTask();
            meta = decoder.Result.BitmapProperties;
            bitmapasync.Wait();
            var image = bitmapasync.Result;
            using (BitmapBuffer buffer = image.LockBuffer(BitmapBufferAccessMode.Write))
            {
                using (var reference = buffer.CreateReference())
                {
                    BitmapPlaneDescription bufferLayout = buffer.GetPlaneDescription(0);
                    rawImage.dim = new Point2D(bufferLayout.Width, bufferLayout.Height);
                    rawImage.Init();
                    unsafe
                    {
                        ((IMemoryBufferByteAccess)reference).GetBuffer(out var temp, out uint capacity);

                        for (int y = 0; y < rawImage.dim.y; y++)
                        {
                            int realY = y * rawImage.dim.x * 3;
                            int bufferY = y * rawImage.dim.x * 4 + +bufferLayout.StartIndex;
                            for (int x = 0; x < rawImage.dim.x; x++)
                            {
                                int realPix = realY + (3 * x);
                                int bufferPix = bufferY + (4 * x);
                                rawImage.rawData[realPix] = temp[bufferPix + 2];
                                rawImage.rawData[realPix + 1] = temp[bufferPix + 1];
                                rawImage.rawData[realPix + 2] = temp[bufferPix];
                            }

                        }
                    }
                }
            }
        }
    }
}