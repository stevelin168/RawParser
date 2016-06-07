﻿using RawParser.Format.IFD;
using RawParser.Image;
using System.Collections.Generic;
using System.IO;

namespace RawParser.Parser
{
    abstract class AParser
    {
        public AParser()
        {

        }

        public uint height;
        public uint width;
        public ushort colorDepth;
        public byte[] cfa;
        public double[] camMul = new double[4];
        public double[] black = new double[4];
                

        //parse the image and return a rawimage 
        //takes time but if no constraint should be called instead of the other method
        abstract public RawImage parse(Stream s);

        //Callable if want to parse sequentally
        //Should absolutely be called in this order else there will be null pointer exception
        //this replace call back because something in .Net was causing more than 400 mb of ram
        abstract public void setStream(Stream s);
        abstract public byte[] parseThumbnail();
        abstract public byte[] parsePreview();
        abstract public Dictionary<ushort,Tag> parseExif();
        abstract public ushort[] parseRAWImage();
    }
}