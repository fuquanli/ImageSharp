// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

// ReSharper disable InconsistentNaming
using System.IO;
using System.Linq;

using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Metadata;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing.Processors.Quantization;
using SixLabors.ImageSharp.Tests.TestUtilities.ImageComparison;

using Xunit;

namespace SixLabors.ImageSharp.Tests.Formats.Png
{
    public class PngEncoderTests
    {
        public static readonly TheoryData<string, PngBitDepth> PngBitDepthFiles =
        new TheoryData<string, PngBitDepth>
        {
            { TestImages.Png.Rgb48Bpp, PngBitDepth.Bit16 },
            { TestImages.Png.Bpp1, PngBitDepth.Bit1 }
        };

        public static readonly TheoryData<string, PngBitDepth, PngColorType> PngTrnsFiles =
        new TheoryData<string, PngBitDepth, PngColorType>
        {
            { TestImages.Png.Gray1BitTrans, PngBitDepth.Bit1, PngColorType.Grayscale },
            { TestImages.Png.Gray2BitTrans, PngBitDepth.Bit2, PngColorType.Grayscale },
            { TestImages.Png.Gray4BitTrans, PngBitDepth.Bit4, PngColorType.Grayscale },
            { TestImages.Png.L8BitTrans, PngBitDepth.Bit8, PngColorType.Grayscale },
            { TestImages.Png.GrayTrns16BitInterlaced, PngBitDepth.Bit16, PngColorType.Grayscale },
            { TestImages.Png.Rgb24BppTrans, PngBitDepth.Bit8, PngColorType.Rgb },
            { TestImages.Png.Rgb48BppTrans, PngBitDepth.Bit16, PngColorType.Rgb }
        };

        /// <summary>
        /// All types except Palette
        /// </summary>
        public static readonly TheoryData<PngColorType> PngColorTypes = new TheoryData<PngColorType>
        {
            PngColorType.RgbWithAlpha,
            PngColorType.Rgb,
            PngColorType.Grayscale,
            PngColorType.GrayscaleWithAlpha,
        };

        public static readonly TheoryData<PngFilterMethod> PngFilterMethods = new TheoryData<PngFilterMethod>
        {
            PngFilterMethod.None,
            PngFilterMethod.Sub,
            PngFilterMethod.Up,
            PngFilterMethod.Average,
            PngFilterMethod.Paeth,
            PngFilterMethod.Adaptive
        };

        /// <summary>
        /// All types except Palette
        /// </summary>
        public static readonly TheoryData<int> CompressionLevels = new TheoryData<int>
        {
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9
        };

        public static readonly TheoryData<int> PaletteSizes = new TheoryData<int>
        {
            30, 55, 100, 201, 255
        };

        public static readonly TheoryData<int> PaletteLargeOnly = new TheoryData<int>
        {
            80, 100, 120, 230
        };

        public static readonly PngInterlaceMode[] InterlaceMode = new[]
        {
            PngInterlaceMode.None,
            PngInterlaceMode.Adam7
        };

        public static readonly TheoryData<string, int, int, PixelResolutionUnit> RatioFiles =
        new TheoryData<string, int, int, PixelResolutionUnit>
        {
            { TestImages.Png.Splash, 11810, 11810 , PixelResolutionUnit.PixelsPerMeter},
            { TestImages.Png.Ratio1x4, 1, 4 , PixelResolutionUnit.AspectRatio},
            { TestImages.Png.Ratio4x1, 4, 1, PixelResolutionUnit.AspectRatio }
        };

        [Theory]
        [WithFile(TestImages.Png.Palette8Bpp, nameof(PngColorTypes), PixelTypes.Rgba32)]
        [WithTestPatternImages(nameof(PngColorTypes), 48, 24, PixelTypes.Rgba32)]
        [WithTestPatternImages(nameof(PngColorTypes), 47, 8, PixelTypes.Rgba32)]
        [WithTestPatternImages(nameof(PngColorTypes), 49, 7, PixelTypes.Rgba32)]
        [WithSolidFilledImages(nameof(PngColorTypes), 1, 1, 255, 100, 50, 255, PixelTypes.Rgba32)]
        [WithTestPatternImages(nameof(PngColorTypes), 7, 5, PixelTypes.Rgba32)]
        public void WorksWithDifferentSizes<TPixel>(TestImageProvider<TPixel> provider, PngColorType pngColorType)
            where TPixel : struct, IPixel<TPixel>
        {
            TestPngEncoderCore(
                provider,
                pngColorType,
                PngFilterMethod.Adaptive,
                PngBitDepth.Bit8,
                PngInterlaceMode.None,
                appendPngColorType: true);
        }

        [Theory]
        [WithTestPatternImages(nameof(PngColorTypes), 24, 24, PixelTypes.Rgba32 | PixelTypes.Bgra32 | PixelTypes.Rgb24)]
        public void IsNotBoundToSinglePixelType<TPixel>(TestImageProvider<TPixel> provider, PngColorType pngColorType)
            where TPixel : struct, IPixel<TPixel>
        {
            foreach (PngInterlaceMode interlaceMode in InterlaceMode)
            {
                TestPngEncoderCore(
                provider,
                pngColorType,
                PngFilterMethod.Adaptive,
                PngBitDepth.Bit8,
                interlaceMode,
                appendPixelType: true,
                appendPngColorType: true);
            }
        }

        [Theory]
        [WithTestPatternImages(nameof(PngFilterMethods), 24, 24, PixelTypes.Rgba32)]
        public void WorksWithAllFilterMethods<TPixel>(TestImageProvider<TPixel> provider, PngFilterMethod pngFilterMethod)
            where TPixel : struct, IPixel<TPixel>
        {
            foreach (PngInterlaceMode interlaceMode in InterlaceMode)
            {
                TestPngEncoderCore(
                provider,
                PngColorType.RgbWithAlpha,
                pngFilterMethod,
                PngBitDepth.Bit8,
                interlaceMode,
                appendPngFilterMethod: true);
            }
        }

        [Theory]
        [WithTestPatternImages(nameof(CompressionLevels), 24, 24, PixelTypes.Rgba32)]
        public void WorksWithAllCompressionLevels<TPixel>(TestImageProvider<TPixel> provider, int compressionLevel)
            where TPixel : struct, IPixel<TPixel>
        {
            foreach (PngInterlaceMode interlaceMode in InterlaceMode)
            {
                TestPngEncoderCore(
                provider,
                PngColorType.RgbWithAlpha,
                PngFilterMethod.Adaptive,
                PngBitDepth.Bit8,
                interlaceMode,
                compressionLevel,
                appendCompressionLevel: true);
            }
        }

        [Theory]
        [WithTestPatternImages(24, 24, PixelTypes.Rgba32, PngColorType.Rgb, PngBitDepth.Bit8)]
        [WithTestPatternImages(24, 24, PixelTypes.Rgba64, PngColorType.Rgb, PngBitDepth.Bit16)]
        [WithTestPatternImages(24, 24, PixelTypes.Rgba32, PngColorType.RgbWithAlpha, PngBitDepth.Bit8)]
        [WithTestPatternImages(24, 24, PixelTypes.Rgba64, PngColorType.RgbWithAlpha, PngBitDepth.Bit16)]
        [WithTestPatternImages(24, 24, PixelTypes.Rgba32, PngColorType.Palette, PngBitDepth.Bit1)]
        [WithTestPatternImages(24, 24, PixelTypes.Rgba32, PngColorType.Palette, PngBitDepth.Bit2)]
        [WithTestPatternImages(24, 24, PixelTypes.Rgba32, PngColorType.Palette, PngBitDepth.Bit4)]
        [WithTestPatternImages(24, 24, PixelTypes.Rgba32, PngColorType.Palette, PngBitDepth.Bit8)]
        [WithTestPatternImages(24, 24, PixelTypes.Rgb24, PngColorType.Grayscale, PngBitDepth.Bit1)]
        [WithTestPatternImages(24, 24, PixelTypes.Rgb24, PngColorType.Grayscale, PngBitDepth.Bit2)]
        [WithTestPatternImages(24, 24, PixelTypes.Rgb24, PngColorType.Grayscale, PngBitDepth.Bit4)]
        [WithTestPatternImages(24, 24, PixelTypes.Rgb24, PngColorType.Grayscale, PngBitDepth.Bit8)]
        [WithTestPatternImages(24, 24, PixelTypes.Rgb48, PngColorType.Grayscale, PngBitDepth.Bit16)]
        [WithTestPatternImages(24, 24, PixelTypes.Rgba32, PngColorType.GrayscaleWithAlpha, PngBitDepth.Bit8)]
        [WithTestPatternImages(24, 24, PixelTypes.Rgba64, PngColorType.GrayscaleWithAlpha, PngBitDepth.Bit16)]
        public void WorksWithAllBitDepths<TPixel>(TestImageProvider<TPixel> provider, PngColorType pngColorType, PngBitDepth pngBitDepth)
            where TPixel : struct, IPixel<TPixel>
        {
            foreach (PngInterlaceMode interlaceMode in InterlaceMode)
            {
                TestPngEncoderCore(
                provider,
                pngColorType,
                PngFilterMethod.Adaptive,
                pngBitDepth,
                interlaceMode,
                appendPngColorType: true,
                appendPixelType: true,
                appendPngBitDepth: true);
            }
        }

        [Theory]
        [WithFile(TestImages.Png.Palette8Bpp, nameof(PaletteLargeOnly), PixelTypes.Rgba32)]
        public void PaletteColorType_WuQuantizer<TPixel>(TestImageProvider<TPixel> provider, int paletteSize)
            where TPixel : struct, IPixel<TPixel>
        {
            foreach (PngInterlaceMode interlaceMode in InterlaceMode)
            {
                TestPngEncoderCore(
                provider,
                PngColorType.Palette,
                PngFilterMethod.Adaptive,
                PngBitDepth.Bit8,
                interlaceMode,
                paletteSize: paletteSize,
                appendPaletteSize: true);
            }
        }

        [Theory]
        [WithBlankImages(1, 1, PixelTypes.Rgba32)]
        public void WritesFileMarker<TPixel>(TestImageProvider<TPixel> provider)
            where TPixel : struct, IPixel<TPixel>
        {
            using (Image<TPixel> image = provider.GetImage())
            using (var ms = new MemoryStream())
            {
                image.Save(ms, new PngEncoder());

                byte[] data = ms.ToArray().Take(8).ToArray();
                byte[] expected = {
                    0x89, // Set the high bit.
                    0x50, // P
                    0x4E, // N
                    0x47, // G
                    0x0D, // Line ending CRLF
                    0x0A, // Line ending CRLF
                    0x1A, // EOF
                    0x0A // LF
                };

                Assert.Equal(expected, data);
            }
        }

        [Theory]
        [MemberData(nameof(RatioFiles))]
        public void Encode_PreserveRatio(string imagePath, int xResolution, int yResolution, PixelResolutionUnit resolutionUnit)
        {
            var options = new PngEncoder();

            var testFile = TestFile.Create(imagePath);
            using (Image<Rgba32> input = testFile.CreateRgba32Image())
            {
                using (var memStream = new MemoryStream())
                {
                    input.Save(memStream, options);

                    memStream.Position = 0;
                    using (var output = Image.Load<Rgba32>(memStream))
                    {
                        ImageMetadata meta = output.Metadata;
                        Assert.Equal(xResolution, meta.HorizontalResolution);
                        Assert.Equal(yResolution, meta.VerticalResolution);
                        Assert.Equal(resolutionUnit, meta.ResolutionUnits);
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(PngBitDepthFiles))]
        public void Encode_PreserveBits(string imagePath, PngBitDepth pngBitDepth)
        {
            var options = new PngEncoder();

            var testFile = TestFile.Create(imagePath);
            using (Image<Rgba32> input = testFile.CreateRgba32Image())
            {
                using (var memStream = new MemoryStream())
                {
                    input.Save(memStream, options);

                    memStream.Position = 0;
                    using (var output = Image.Load<Rgba32>(memStream))
                    {
                        PngMetadata meta = output.Metadata.GetPngMetadata();

                        Assert.Equal(pngBitDepth, meta.BitDepth);
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(PngTrnsFiles))]
        public void Encode_PreserveTrns(string imagePath, PngBitDepth pngBitDepth, PngColorType pngColorType)
        {
            var options = new PngEncoder();

            var testFile = TestFile.Create(imagePath);
            using (Image<Rgba32> input = testFile.CreateRgba32Image())
            {
                PngMetadata inMeta = input.Metadata.GetPngMetadata();
                Assert.True(inMeta.HasTransparency);

                using (var memStream = new MemoryStream())
                {
                    input.Save(memStream, options);
                    memStream.Position = 0;
                    using (var output = Image.Load<Rgba32>(memStream))
                    {
                        PngMetadata outMeta = output.Metadata.GetPngMetadata();
                        Assert.True(outMeta.HasTransparency);

                        switch (pngColorType)
                        {
                            case PngColorType.Grayscale:
                                if (pngBitDepth.Equals(PngBitDepth.Bit16))
                                {
                                    Assert.True(outMeta.TransparentL16.HasValue);
                                    Assert.Equal(inMeta.TransparentL16, outMeta.TransparentL16);
                                }
                                else
                                {
                                    Assert.True(outMeta.TransparentL8.HasValue);
                                    Assert.Equal(inMeta.TransparentL8, outMeta.TransparentL8);
                                }

                                break;
                            case PngColorType.Rgb:
                                if (pngBitDepth.Equals(PngBitDepth.Bit16))
                                {
                                    Assert.True(outMeta.TransparentRgb48.HasValue);
                                    Assert.Equal(inMeta.TransparentRgb48, outMeta.TransparentRgb48);
                                }
                                else
                                {
                                    Assert.True(outMeta.TransparentRgb24.HasValue);
                                    Assert.Equal(inMeta.TransparentRgb24, outMeta.TransparentRgb24);
                                }

                                break;
                        }
                    }
                }
            }
        }

        private static void TestPngEncoderCore<TPixel>(
            TestImageProvider<TPixel> provider,
            PngColorType pngColorType,
            PngFilterMethod pngFilterMethod,
            PngBitDepth bitDepth,
            PngInterlaceMode interlaceMode,
            int compressionLevel = 6,
            int paletteSize = 255,
            bool appendPngColorType = false,
            bool appendPngFilterMethod = false,
            bool appendPixelType = false,
            bool appendCompressionLevel = false,
            bool appendPaletteSize = false,
            bool appendPngBitDepth = false)
        where TPixel : struct, IPixel<TPixel>
        {
            using (Image<TPixel> image = provider.GetImage())
            {
                var encoder = new PngEncoder
                {
                    ColorType = pngColorType,
                    FilterMethod = pngFilterMethod,
                    CompressionLevel = compressionLevel,
                    BitDepth = bitDepth,
                    Quantizer = new WuQuantizer(paletteSize),
                    InterlaceMethod = interlaceMode
                };

                string pngColorTypeInfo = appendPngColorType ? pngColorType.ToString() : string.Empty;
                string pngFilterMethodInfo = appendPngFilterMethod ? pngFilterMethod.ToString() : string.Empty;
                string compressionLevelInfo = appendCompressionLevel ? $"_C{compressionLevel}" : string.Empty;
                string paletteSizeInfo = appendPaletteSize ? $"_PaletteSize-{paletteSize}" : string.Empty;
                string pngBitDepthInfo = appendPngBitDepth ? bitDepth.ToString() : string.Empty;
                string pngInterlaceModeInfo = interlaceMode != PngInterlaceMode.None ? $"_{interlaceMode}" : string.Empty;

                string debugInfo = $"{pngColorTypeInfo}{pngFilterMethodInfo}{compressionLevelInfo}{paletteSizeInfo}{pngBitDepthInfo}{pngInterlaceModeInfo}";

                string actualOutputFile = provider.Utility.SaveTestOutputFile(image, "png", encoder, debugInfo, appendPixelType);

                // Compare to the Magick reference decoder.
                IImageDecoder referenceDecoder = TestEnvironment.GetReferenceDecoder(actualOutputFile);
                // We compare using both our decoder and the reference decoder as pixel transformation
                // occurs within the encoder itself leaving the input image unaffected.
                // This means we are benefiting from testing our decoder also.
                using (var imageSharpImage = Image.Load<TPixel>(actualOutputFile, new PngDecoder()))
                using (var referenceImage = Image.Load<TPixel>(actualOutputFile, referenceDecoder))
                {
                    ImageComparer.Exact.VerifySimilarity(referenceImage, imageSharpImage);
                }
            }
        }
    }
}
