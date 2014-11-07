/**
 * Author: Scr_Ra.
 * Date: November 3rd, 2014.
 * Version: 1.0
 * Description: A helper class based on the SevenZipHelper class from http://www.nullskull.com/a/768/7zip-lzma-inmemory-compression-with-c.aspx
 * 
 */

using System;
using SevenZip;

namespace SevenZipHelper
{
    /// <summary>
    /// Class to provide compressiond and decompression methods.
    /// </summary>
    public class SevenZipHelper : ICodeProgress
    {
        static int dictionary = 1 << 23;
        static bool eos = false;

        static CoderPropID[] propIDs = 
		{
            CoderPropID.DictionarySize,
            CoderPropID.PosStateBits,
            CoderPropID.LitContextBits,
            CoderPropID.LitPosBits,
            CoderPropID.Algorithm,
            CoderPropID.NumFastBytes,
            CoderPropID.MatchFinder,
            CoderPropID.EndMarker
        };

        // these are the default properties, keeping it simple for now:
        static object[] properties = 
        {
            (Int32)(dictionary),
            (Int32)(2),
            (Int32)(3),
            (Int32)(0),
            (Int32)(2),
            (Int32)(128),
            "bt4",
            eos
        };

        /// <summary>
        /// Defined the max size of a 
        /// </summary>
        public static uint MaxSize = uint.MaxValue;

        /// <summary>
        /// Default size of the header of every stream compressed. <b> Only useful when you are working with byte arrays.</b>
        /// </summary>
        public static int DefaultHeaderSize = 13;

        /// <summary>
        /// Buffer's size used on each operation.
        /// </summary>
        protected uint bufferSize;

        /// <summary>
        /// MemoryStream to be used as a buffer.
        /// </summary>
        protected System.IO.MemoryStream ioMemory;

        /// <summary>
        /// LZMA encoder.
        /// </summary>
        protected SevenZip.Compression.LZMA.Encoder compressor;

        /// <summary>
        /// LZMA decoder.
        /// </summary>
        protected SevenZip.Compression.LZMA.Decoder decompressor;

        /// <summary>
        /// Holds the size of the input paramater on compressing and decompressinge methods.
        /// </summary>
        protected Int64 inputBytesCount;

        /// <summary>
        /// Holds the size of the ouput parameter on compressing and decompressinge methods.
        /// </summary>
        protected Int64 outBytesCount;

        /// <summary>
        /// Creates a new object, ready to use.
        /// </summary>
        /// <param name="bufferSize"></param>
        public SevenZipHelper(uint bufferSize)
        {
            if (bufferSize <= 0)
                bufferSize = SevenZipHelper.MaxSize;
            this.bufferSize = bufferSize;
            this.ioMemory = new System.IO.MemoryStream((int)this.bufferSize);
            this.compressor = new SevenZip.Compression.LZMA.Encoder();
            this.decompressor = new SevenZip.Compression.LZMA.Decoder();
            this.compressor.SetCoderProperties(SevenZipHelper.propIDs, SevenZipHelper.properties);
        }

        /// <summary>
        /// Compresses a given Stream into a byte array.
        /// </summary>
        /// <param name="sourceStream"></param>
        /// <param name="compressedData"></param>
        /// <returns>Number of the compressed bytes.</returns>
        public void Compress( System.IO.Stream sourceStream, byte[] compressedData)
        {
            System.IO.MemoryStream outStream = new System.IO.MemoryStream(compressedData);
            this.compressor.WriteCoderProperties(outStream);
            long fileSize = sourceStream.Length;
            for (int i = 0; i < 8; i++)
                outStream.WriteByte((Byte)(fileSize >> (8 * i)));
            this.compressor.Code(sourceStream, outStream, -1, -1, this);
        }

        /// <summary>
        /// Compresses the source stream and writes it into another stream.<b> Do not use it with MemoryStreams.</b> If you want
        /// to write to a MemoryStream call Compress***************STILL have to write the documentation.
        /// </summary>
        /// <param name="sourceStream"></param>
        /// <param name="compressedStream"></param>
        public void Compress( System.IO.Stream sourceStream, System.IO.Stream compressedStream)
        {
            this.compressor.WriteCoderProperties(compressedStream);
            long fileSize = sourceStream.Length;
            for (int i = 0; i < 8; i++)
                compressedStream.WriteByte((Byte)(fileSize >> (8 * i)));
            this.compressor.Code(sourceStream, compressedStream, -1, -1, this);
        }

        public void Decompress( System.IO.Stream compressedStream, System.IO.Stream uncompressedStream )
        {
            compressedStream.Seek(0, System.IO.SeekOrigin.Begin);
            byte[] fileCompressionProperties = new byte[5];
            long outSize = 0;
            if (compressedStream.Read(fileCompressionProperties, 0, 5) != 5)
                throw (new Exception("input .lzma is too short"));
            for (int i = 0; i < 8; i++)
            {
                int v = compressedStream.ReadByte();
                if (v < 0)
                    throw (new Exception("Can't Read 1"));
                outSize |= ((long)(byte)v) << (8 * i);
            }
            decompressor.SetDecoderProperties(fileCompressionProperties);
            decompressor.Code(compressedStream, uncompressedStream, compressedStream.Length, outSize, this);
        }

        /// <summary>
        /// Decompress a byte array and writes the result into the decompressedData argument.
        /// </summary>
        /// <param name="compressedData"></param>
        /// <param name="compressedSize"></param>
        /// <param name="decompressedData"></param>
        public void Decompress(byte [] compressedData, int compressedSize, byte [] decompressedData)
        {
            System.IO.MemoryStream outStream = new System.IO.MemoryStream(decompressedData);
            this.ioMemory.Seek(0, System.IO.SeekOrigin.Begin);
            this.ioMemory.Write(compressedData, 0, compressedSize);

            this.ioMemory.Seek(0, System.IO.SeekOrigin.Begin);
            byte[] fileCompressionProperties = new byte[5];
            long outSize = 0;
            if (this.ioMemory.Read(fileCompressionProperties, 0, 5) != 5)
                throw (new Exception("input .lzma is too short"));
            for (int i = 0; i < 8; i++)
            {
                int v = this.ioMemory.ReadByte();
                if (v < 0)
                    throw (new Exception("Can't Read 1"));
                outSize |= ((long)(byte)v) << (8 * i);
            }
            if( decompressedData.Length < outSize)
            {
                throw (new InsufficientMemoryException("There is not enough space on the given array to hold the entire decompressed stream. Try to call decompress to a file stream."));
            }
            decompressor.SetDecoderProperties(fileCompressionProperties);
            decompressor.Code(this.ioMemory, outStream, compressedSize, outSize, this);
        }

        /// <summary>
        /// Releases all resources used by the reference.
        /// </summary>
        public virtual void Release()
        {
            this.ioMemory.Dispose();
            this.ioMemory = null;

            this.compressor = null;
            this.bufferSize = 0;
        }

        /// <summary>
        /// Sets the progress of the compress/decompress operation.
        /// </summary>
        /// <param name="inSize"></param>
        /// <param name="outSize"></param>
        public void SetProgress(Int64 inSize, Int64 outSize)
        {
            this.inputBytesCount = inSize;
            this.outBytesCount = outSize;
        }

        /// <summary>
        /// Gets the amount of bytes that the last operation produce.
        /// </summary>
        public Int64 OutputBytesCount
        {
            get
            {
                return this.outBytesCount;
            }
        }
    }
}
