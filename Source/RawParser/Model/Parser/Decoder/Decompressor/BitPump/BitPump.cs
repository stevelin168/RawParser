﻿using System;

namespace RawNet.Decoder.Decompressor
{
    internal abstract class BitPump
    {
        protected byte[] buffer;
        protected long size;            // This if the end of buffer.
        protected int off;                  // Offset in bytes
<<<<<<< HEAD:Source/RawParser/Model/Parser/Decoder/Decompressor/BitPump/BitPump.cs
                                            // protected int stuffed = 0;
=======
       // protected int stuffed = 0;
>>>>>>> b2ca1825590115767bd958f9ab327a4806fb4a92:Source/RawParser/Model/Parser/Decoder/Decompressor/BitPump/BitPump.cs
        protected int MIN_GET_BITS;   /* max value for long getBuffer */
        protected static int BITS_PER_LONG = (8 * sizeof(UInt32));
        protected static int BITS_PER_LONG_LONG = (8 * sizeof(UInt64));
        protected int left;
        protected ulong current;

        public abstract int Offset { get; set; }
        public abstract void Fill();

        public abstract uint PeekBit();
        public abstract void SkipBits(int nbits);
<<<<<<< HEAD:Source/RawParser/Model/Parser/Decoder/Decompressor/BitPump/BitPump.cs
        public abstract uint GetByte();
        public abstract uint GetBit();
        public abstract uint GetBits(int nbits);
        public abstract uint PeekBits(int nbits);
        public abstract uint PeekByte();
=======
        public abstract byte GetByte();
        public abstract uint GetBit();
        public abstract uint GetBits(int nbits);
        public abstract ushort GetLowBits(int nbits);
        public abstract uint PeekBits(int nbits);
        public abstract byte PeekByte();
>>>>>>> b2ca1825590115767bd958f9ab327a4806fb4a92:Source/RawParser/Model/Parser/Decoder/Decompressor/BitPump/BitPump.cs
    }
}