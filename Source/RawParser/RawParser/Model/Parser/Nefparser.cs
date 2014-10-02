﻿using RawParser.Model.ImageDisplay;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RawParser.Model.Parser
{
    class Nefparser : TIFFParser
    {

        protected class NEFHeader : Header
        {
           
        }

        protected class NEFIFD : IFD
        {
            
        }

        protected class NikonMarkerNote : MarkerNote
        {
            
        }

        override public RawImage parse(string path)
        {
            NEFHeader header = new NEFHeader();
            NEFIFD ifd = new NEFIFD();

            NEFIFD subifd0 = new NEFIFD();
            NEFIFD subifd1 = new NEFIFD();

            NikonMarkerNote markernote = new NikonMarkerNote();

            BinaryReader fileStream = new BinaryReader(new FileStream(path, FileMode.Open, FileAccess.Read));
            try
            {
                //read the header
                header.byteOrder = fileStream.ReadUInt16();

                if (header.byteOrder == 0x4D4D)
                {
                    //File is in reverse bit order
                    fileStream = new BinaryReaderBE(new FileStream(path, FileMode.Open, FileAccess.Read), System.Text.Encoding.BigEndianUnicode);
                    fileStream.ReadUInt16();
                }
                
                header.TIFFMagic = fileStream.ReadUInt16();
                header.TIFFoffset = fileStream.ReadUInt32();
                                
                //read the IFD
                base.readIFD(fileStream, header.TIFFoffset,ifd, true);

                //read the second IFD
                base.readIFD(fileStream,(uint)ifd.findTag(330).data[0], subifd0, true);

                //read the third IFD    
                base.readIFD(fileStream, (uint)ifd.findTag(330).data[1], subifd1, true);

                base.readMarkerNote(fileStream, (uint)ifd.findTag(34665).data[0], markernote);
            }
            finally
            {
                fileStream.Close();
            }
            
            string tempstr = " ";
            for (int i = 0; i < ifd.tagNumber; i++)
            {
                tempstr += "[" + ifd.tags[i].tagId + ":";
                for (int j = 0; j < ifd.tags[i].dataCount; j++)
                {
                    tempstr += ifd.tags[i].data[j] + ":";
                }
                tempstr += "]";
            }
            Console.Write(
                header.byteOrder
                + " " + header.TIFFMagic
                + " " + header.TIFFoffset +
                tempstr);

            // Pixel [][] pixelBuffer = new Pixel ()[][];
            //RawImage rawImage = new RawImage(new Exif(), new Dimension(), pixelBuffer);
            //return rawImage;
            return null;
        }
    }
}
