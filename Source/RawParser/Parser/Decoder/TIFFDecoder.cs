﻿using System;
using System.IO;

namespace RawNet
{
    internal class TiffDecoder : RawDecoder
    {
        protected IFD ifd;

        public TiffDecoder(Stream stream) : base(stream)
        {
            //parse the ifd
            if (stream.Length < 16)
                throw new RawDecoderException("Not a TIFF file (size too small)");
            Endianness endian = Endianness.little;
            byte[] data = new byte[5];
            stream.Position = 0;
            stream.Read(data, 0, 4);
            if (data[0] == 0x4D || data[1] == 0x4D)
            {
                //open binaryreader
                reader = new TIFFBinaryReaderRE(stream);
                endian = Endianness.big;

                if (data[3] != 42 && data[2] != 0x4f) // ORF sometimes has 0x4f, Lovely!
                    throw new RawDecoderException("Not a TIFF file (magic 42)");
            }
            else if (data[0] == 0x49 || data[1] == 0x49)
            {
                reader = new TIFFBinaryReader(stream);
                if (data[2] != 42 && data[2] != 0x52 && data[2] != 0x55) // ORF has 0x52, RW2 0x55 - Brillant!
                    throw new RawDecoderException("Not a TIFF file (magic 42)");
            }
            else
            {
                throw new RawDecoderException("Not a TIFF file (ID)");
            }

            uint nextIFD;
            reader.Position = 4;
            nextIFD = reader.ReadUInt32();
            ifd = new IFD(reader, nextIFD, endian, 0);
            nextIFD = ifd.NextOffset;

            while (nextIFD != 0)
            {
                ifd.subIFD.Add(new IFD(reader, nextIFD, endian, 0));
                if (ifd.subIFD.Count > 100)
                {
                    throw new RawDecoderException("TIFF file has too many SubIFDs, probably broken");
                }
                nextIFD = (ifd.subIFD[ifd.subIFD.Count - 1]).NextOffset;
            }
        }

        public override void DecodeRaw()
        {
            if (!ifd.tags.TryGetValue((TagType)0x0106, out var photoMetricTag)) throw new FormatException("File not correct");
            if (!ifd.tags.TryGetValue((TagType)0x0111, out var imageOffsetTag)) throw new FormatException("File not correct");
            if (!ifd.tags.TryGetValue((TagType)0x0100, out var imageWidthTag)) throw new FormatException("File not correct");
            if (!ifd.tags.TryGetValue((TagType)0x0101, out var imageHeightTag)) throw new FormatException("File not correct");
            if (!ifd.tags.TryGetValue((TagType)0x0103, out var imageCompressedTag)) throw new FormatException("File not correct");
            if (!ifd.tags.TryGetValue((TagType)0x0116, out var rowPerStripTag)) throw new FormatException("File not correct");
            if (!ifd.tags.TryGetValue((TagType)0x0117, out var stripSizeTag)) throw new FormatException("File not correct");

            if ((ushort)photoMetricTag.data[0] == 2)
            {
                if (!ifd.tags.TryGetValue((TagType)0x0102, out var bitPerSampleTag)) throw new FormatException("File not correct");
                if (!ifd.tags.TryGetValue((TagType)0x0115, out var samplesPerPixel)) throw new FormatException("File not correct");
                int height = imageHeightTag.GetInt(0);
                int width = imageWidthTag.GetInt(0);
                rawImage.isCFA = false;
                rawImage.raw.dim = new Point2D(width, height);
                rawImage.raw.uncroppedDim = rawImage.raw.dim;
                //suppose that image are always 8,8,8 or 16,16,16
                ushort colorDepth = (ushort)bitPerSampleTag.data[0];
                ushort[] image = new ushort[width * height * 3];
                long strips = height / Convert.ToInt64(rowPerStripTag.data[0]), lastStrip = height % Convert.ToInt64(rowPerStripTag.data[0]);
                long rowperstrip = Convert.ToInt64(rowPerStripTag.data[0]);
                uint compression = imageCompressedTag.GetUInt(0);
                if (compression == 1)
                {
                    //not compressed
                    for (int i = 0; i < strips + ((lastStrip == 0) ? 0 : 1); i++)
                    {
                        //for each complete strip
                        //move to the offset
                        reader.Position = Convert.ToInt64(imageOffsetTag.data[i]);
                        for (int y = 0; y < rowperstrip && !(i == strips && y <= lastStrip); y++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                //get the pixel
                                //red
                                image[(y + i * rowperstrip) * width * 3 + x * 3] = reader.ReadByte();
                                //green
                                image[(y + i * rowperstrip) * width * 3 + x * 3 + 1] = reader.ReadByte();
                                //blue 
                                image[(y + i * rowperstrip) * width * 3 + x * 3 + 2] = reader.ReadByte();
                                for (int z = 0; z < (Convert.ToInt32(samplesPerPixel.data[0]) - 3); z++)
                                {
                                    //pass the other pixel if more light
                                    reader.ReadByte();
                                }
                            }
                        }
                    }
                }
                else if (compression == 32773)
                {
                    //compressed
                    /*Loop until you get the number of unpacked bytes you are expecting:
                    Read the next source byte into n.
                    If n is between 0 and 127 inclusive, copy the next n+1 bytes literally.
                    Else if n is between - 127 and - 1 inclusive, copy the next byte -n + 1
                    times.
                    Else if n is - 128, noop.
                    Endloop
                    */
                    //not compressed
                    for (int i = 0; i < strips + ((lastStrip == 0) ? 0 : 1); i++)
                    {
                        //for each complete strip
                        //move to the offset
                        reader.Position = Convert.ToInt64(imageOffsetTag.data[i]);
                        for (int y = 0; y < rowperstrip && !(i == strips && y < lastStrip); y++)
                        {
                            //uncompress line by line of pixel
                            ushort[] temp = new ushort[3 * width];
                            short buffer = 0;
                            int count = 0;
                            for (int x = 0; x < width * 3;)
                            {
                                buffer = reader.ReadByte();
                                count = 0;
                                if (buffer >= 0)
                                {
                                    for (int k = 0; k < count; ++k, ++x)
                                    {
                                        temp[x] = reader.ReadByte();
                                    }
                                }
                                else
                                {
                                    count = -buffer;
                                    buffer = reader.ReadByte();
                                    for (int k = 0; k < count; ++k, ++x)
                                    {
                                        temp[x] = (ushort)buffer;
                                    }
                                }
                            }

                            for (int x = 0; x < width * 3; x++)
                            {

                                //red
                                image[(y + i * rowperstrip) * width * 3 + x * 3] = temp[x * 3];
                                //green
                                image[(y + i * rowperstrip) * width + x * 3 + 1] = temp[x * 3 + 1];
                                //blue 
                                image[(y + i * rowperstrip) * width + x * 3 + 2] = temp[x * 3 + 2];
                                for (int z = 0; z < ((int)samplesPerPixel.data[0] - 3); z++)
                                {
                                    //pass the other pixel if more light
                                    reader.ReadByte();
                                }
                            }
                        }
                    }
                }
                else throw new FormatException("Compression mode " + imageCompressedTag.DataAsString + " not supported yet");
                rawImage.cpp = 3;
                rawImage.ColorDepth = colorDepth;
                rawImage.bpp = colorDepth;
                rawImage.raw.data = image;
            }
            else throw new FormatException("Photometric interpretation " + photoMetricTag.DataAsString + " not supported yet");
        }

        public override void DecodeMetadata()
        {
            if (rawImage.ColorDepth == 0)
            {
                rawImage.ColorDepth = ifd.GetEntryRecursive(TagType.BITSPERSAMPLE).GetUShort(0);
            }
            var isoTag = ifd.GetEntryRecursive(TagType.ISOSPEEDRATINGS);
            if (isoTag != null) rawImage.metadata.IsoSpeed = isoTag.GetInt(0);
            var exposure = ifd.GetEntryRecursive(TagType.EXPOSURETIME);
            var fn = ifd.GetEntryRecursive(TagType.FNUMBER);
            if (exposure != null) rawImage.metadata.Exposure = exposure.GetFloat(0);
            if (fn != null) rawImage.metadata.Aperture = fn.GetFloat(0);
            if (rawImage.whitePoint == 0)
            {
                Tag whitelevel = ifd.GetEntryRecursive(TagType.WHITELEVEL);
                if (whitelevel != null)
                {
                    rawImage.whitePoint = whitelevel.GetUInt(0);
                }
            }

            var time = ifd.GetEntryRecursive(TagType.DATETIMEORIGINAL);
            var timeModify = ifd.GetEntryRecursive(TagType.DATETIMEDIGITIZED);
            if (time != null) rawImage.metadata.TimeTake = time.DataAsString;
            if (timeModify != null) rawImage.metadata.TimeModify = timeModify.DataAsString;
            // Set the make and model
            var t = ifd.GetEntryRecursive(TagType.MAKE);
            var t2 = ifd.GetEntryRecursive(TagType.MODEL);
            if (t != null && t2 != null)
            {
                string make = t.DataAsString;
                string model = t2.DataAsString;
                make = make.Trim();
                model = model.Trim();
                rawImage.metadata.Make = make;
                rawImage.metadata.Model = model;
            }

            //rotation
            var rotateTag = ifd.GetEntryRecursive(TagType.ORIENTATION);
            if (rotateTag == null)
            {
                rotateTag = ifd.GetEntryRecursive((TagType)0xbc02);
                if (rotateTag != null)
                {
                    switch (rotateTag.GetUShort(0))
                    {
                        case 3:
                        case 2:
                            rawImage.rotation = 2;
                            break;
                        case 4:
                        case 6:
                            rawImage.rotation = 1;
                            break;
                        case 7:
                        case 5:
                            rawImage.rotation = 3;
                            break;
                    }
                }
            }
            else
            {
                switch (rotateTag.GetUShort(0))
                {
                    case 3:
                    case 2:
                        rawImage.rotation = 2;
                        break;
                    case 6:
                    case 5:
                        rawImage.rotation = 1;
                        break;
                    case 8:
                    case 7:
                        rawImage.rotation = 3;
                        break;
                }
            }
            rawImage.metadata.OriginalRotation = rawImage.rotation;
            rawImage.metadata.RawDim = new Point2D(rawImage.raw.uncroppedDim.width, rawImage.raw.uncroppedDim.height);
        }
    }
}