// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.IO;
using SixLabors.ImageSharp.Formats.Experimental.Tiff.Compression.Decompressors;
using SixLabors.ImageSharp.Formats.Experimental.Tiff.Constants;
using SixLabors.ImageSharp.Formats.Experimental.Tiff.Utils;
using SixLabors.ImageSharp.IO;
using SixLabors.ImageSharp.Memory;
using Xunit;

namespace SixLabors.ImageSharp.Tests.Formats.Tiff.Compression
{
    [Trait("Format", "Tiff")]
    public class LzwTiffCompressionTests
    {
        [Theory]
        [InlineData(new byte[] { 1, 2, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 3, 4 }, new byte[] { 128, 0, 64, 66, 168, 36, 22, 12, 3, 2, 64, 64, 0, 0 })] // Repeated bytes

        public void Compress_Works(byte[] inputData, byte[] expectedCompressedData)
        {
            var compressedData = new byte[expectedCompressedData.Length];
            Stream streamData = CreateCompressedStream(inputData);
            streamData.Read(compressedData, 0, expectedCompressedData.Length);

            Assert.Equal(expectedCompressedData, compressedData);
        }

        [Theory]
        [InlineData(new byte[] { })]
        [InlineData(new byte[] { 42 })] // One byte
        [InlineData(new byte[] { 42, 16, 128, 53, 96, 218, 7, 64, 3, 4, 97 })] // Random bytes
        [InlineData(new byte[] { 1, 2, 42, 42, 42, 42, 42, 42, 42, 42, 42, 42, 3, 4 })] // Repeated bytes
        [InlineData(new byte[] { 1, 2, 42, 53, 42, 53, 42, 53, 42, 53, 42, 53, 3, 4 })] // Repeated sequence

        public void Compress_Decompress_Roundtrip_Works(byte[] data)
        {
            using BufferedReadStream stream = CreateCompressedStream(data);
            var buffer = new byte[data.Length];

            new LzwTiffCompression(Configuration.Default.MemoryAllocator, 10, 8, TiffPredictor.None).Decompress(stream, 0, (uint)stream.Length, buffer);

            Assert.Equal(data, buffer);
        }

        private static BufferedReadStream CreateCompressedStream(byte[] inputData)
        {
            Stream compressedStream = new MemoryStream();
            using System.Buffers.IMemoryOwner<byte> data = Configuration.Default.MemoryAllocator.Allocate<byte>(inputData.Length);
            inputData.AsSpan().CopyTo(data.GetSpan());

            using (var encoder = new TiffLzwEncoder(Configuration.Default.MemoryAllocator, data))
            {
                encoder.Encode(compressedStream);
            }

            compressedStream.Seek(0, SeekOrigin.Begin);

            return new BufferedReadStream(Configuration.Default, compressedStream);
        }
    }
}
