// <copyright file="FontUtils.cs" company="Vatsan Madhavan">
// Copyright (c) Vatsan Madhavan. All rights reserved.
// </copyright>

namespace FontInstaller
{
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Windows.Win32.Foundation;
    using Windows.Win32.Graphics.DirectWrite;
    using Windows.Win32.Graphics.Gdi;
    using static Windows.Win32.PInvoke;

    /// <summary>
    /// Contains font related functions.
    /// </summary>
    internal static class FontUtils
    {
        private static readonly HashSet<string> Fonts = new HashSet<string>();
        private static readonly IDWriteFactory DWriteFactory;
        private static readonly IDWriteGdiInterop DWriteGdiInterop;

        /// <summary>
        /// Initializes static members of the <see cref="FontUtils"/> class.
        /// </summary>
        static unsafe FontUtils()
        {
            var hwnd = new HWND(0);
            var hdc = GetDC(hwnd);
            try
            {
                var logFont = new LOGFONTW
                {
                    lfCharSet = (byte)DEFAULT_CHARSET,
                    lfFaceName = string.Empty,
                };

                EnumFontFamiliesEx(hdc, &logFont, EnumProc, new LPARAM(0), 0);
                FontFaces = Fonts.ToList().AsReadOnly();
            }
            finally
            {
                ReleaseDC(hwnd, hdc);
            }

            // Init DWriteFactory
            DWriteCreateFactory(
                DWRITE_FACTORY_TYPE.DWRITE_FACTORY_TYPE_SHARED,
                typeof(IDWriteFactory).GUID,
                out object factory)
                .ThrowOnFailure();

            DWriteFactory = factory as IDWriteFactory ?? throw new Exception();
            DWriteFactory.GetGdiInterop(out DWriteGdiInterop);
        }

        /// <summary>
        /// Gets a list of Font faces available on this system.
        /// </summary>
        internal static IReadOnlyList<string> FontFaces { get; }

        /// <summary>
        /// Gets a list of font-face names from a given font-file.
        /// </summary>
        /// <param name="fontFile">Path to font-file.</param>
        /// <returns>A list of font-face names.</returns>
        /// <exception cref="FileNotFoundException">Thrown when <paramref name="fontFile"/> doesn't exist.</exception>
        internal static unsafe IReadOnlyList<string> GetFontFaceNames(string fontFile)
        {
            if (!File.Exists(fontFile))
            {
                throw new FileNotFoundException("File doesn't exist", fontFile);
            }

            var faceNames = new List<string>();
            IDWriteFontFile? dWriteFontFile = null;
            fixed (char* filePath = fontFile)
            {
                DWriteFactory.CreateFontFileReference(
                    new PCWSTR(filePath),
                    fontFile: out dWriteFontFile);

                BOOL isSupportedFontFileType = false;
                DWRITE_FONT_FILE_TYPE fontFileType = default;
                DWRITE_FONT_FACE_TYPE fontFaceType = default;
                uint numberOfFaces = 0;
                dWriteFontFile.Analyze(
                    &isSupportedFontFileType,
                    &fontFileType,
                    &fontFaceType,
                    &numberOfFaces);

                if (isSupportedFontFileType)
                {
                    for (uint faceIndex = 0; faceIndex < numberOfFaces; faceIndex++)
                    {
                        DWriteFactory.CreateFontFace(
                            fontFaceType,
                            1,
                            new IDWriteFontFile[] { dWriteFontFile },
                            faceIndex,
                            DWRITE_FONT_SIMULATIONS.DWRITE_FONT_SIMULATIONS_NONE,
                            out var fontFace);

                        LOGFONTW logFont = default;
                        DWriteGdiInterop.ConvertFontFaceToLOGFONT(
                            fontFace,
                            &logFont);

                        faceNames.Add(logFont.lfFaceName.ToString());
                    }
                }
            }

            return faceNames.AsReadOnly();
        }

        private static unsafe int EnumProc(LOGFONTW* logFont, TEXTMETRICW* textMetric, uint dwFlags, LPARAM lParam)
        {
            Fonts.Add(logFont->lfFaceName.ToString());
            return 1;
        }
    }
}
