// <copyright file="FontUtils.cs" company="Vatsan Madhavan">
// Copyright (c) Vatsan Madhavan. All rights reserved.
// </copyright>

namespace FontInstaller
{
    using System.Buffers;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Windows.Win32.Foundation;
    using Windows.Win32.Graphics.DirectWrite;
    using Windows.Win32.Graphics.Gdi;
    using static Windows.Win32.PInvoke;

    /// <summary>
    /// Contains font related functions.
    /// </summary>
    public static class FontUtils
    {
        private static readonly IDWriteFactory DWriteFactory;
        private static readonly IDWriteGdiInterop DWriteGdiInterop;

        /// <summary>
        /// Initializes static members of the <see cref="FontUtils"/> class.
        /// </summary>
        static unsafe FontUtils()
        {
            // Init DWriteFactory
            DWriteCreateFactory(
                DWRITE_FACTORY_TYPE.DWRITE_FACTORY_TYPE_SHARED,
                typeof(IDWriteFactory).GUID,
                out object factory)
                .ThrowOnFailure();

            DWriteFactory = factory as IDWriteFactory ?? throw new Exception();
            DWriteFactory.GetGdiInterop(out DWriteGdiInterop);

            FontFaces = EnumerateSystemFontFamilies();
        }

        /// <summary>
        /// Gets a list of Font faces and corresponding file paths available on this system.
        /// </summary>
        public static IReadOnlyDictionary<string, IReadOnlyList<string>> FontFaces { get; }

        /// <summary>
        /// Gets a list of font-face names from a given font-file.
        /// </summary>
        /// <param name="fontFile">Path to font-file.</param>
        /// <returns>A list of font-face names.</returns>
        /// <exception cref="FileNotFoundException">Thrown when <paramref name="fontFile"/> doesn't exist.</exception>
        public static unsafe IReadOnlyList<string> GetFontFaceNames(string fontFile)
        {
            if (!File.Exists(fontFile))
            {
                throw new FileNotFoundException("File doesn't exist", fontFile);
            }

            var faceNames = new List<string>();
            fixed (char* filePath = fontFile)
            {
                DWriteFactory.CreateFontFileReference(
                    new PCWSTR(filePath),
                    fontFile: out IDWriteFontFile dWriteFontFile);

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

        /// <summary>
        /// Checks whether a font file is already installed.
        /// </summary>
        /// <param name="fontFile">Path to font file.</param>
        /// <returns>True if it's already installed; otherwise False.</returns>
        public static bool IsInstalled(string fontFile)
        {
            fontFile = Path.GetFullPath(fontFile).TrimEnd(Path.PathSeparator);
            var faceNames = GetFontFaceNames(fontFile);
            bool isInstalled = false;

            foreach (var faceName in faceNames)
            {
                if (!FontFaces.ContainsKey(faceName))
                {
                    isInstalled = false;
                    break;
                }

                var matches =
                    FontFaces[faceName]
                    .Where(path => string.Equals(path, fontFile, StringComparison.CurrentCultureIgnoreCase));
                isInstalled = matches?.Count() > 0;
                if (isInstalled)
                {
                    break;
                }
            }

            return isInstalled;
        }

        private static unsafe IReadOnlyDictionary<string, IReadOnlyList<string>> EnumerateSystemFontFamilies()
        {
            var fontFamilyInfo = new Dictionary<string, IReadOnlyList<string>>();

            DWriteFactory.GetSystemFontCollection(
                out IDWriteFontCollection fontCollection,
                true);
            var fontFamilyCount = fontCollection.GetFontFamilyCount();

            char* filePath = stackalloc char[(int)MAX_PATH];
            for (uint i = 0; i < fontFamilyCount; i++)
            {
                fontCollection.GetFontFamily(i, out IDWriteFontFamily fontFamily);
                fontFamily.GetFamilyNames(out IDWriteLocalizedStrings fontFamilyNames);
                var fontFamilyName = fontFamilyNames.ToUserDefaultLocaleString();

                var files = new List<string>();
                uint fontCount = fontFamily.GetFontCount();
                for (uint j = 0; j < fontCount; j++)
                {
                    fontFamily.GetFont(j, out var font);
                    font.CreateFontFace(out var fontFace);

                    uint numberOfFiles = 0;
                    fontFace.GetFiles(&numberOfFiles, null);

                    var fontFiles = new IDWriteFontFile[numberOfFiles];
                    fontFace.GetFiles(&numberOfFiles, fontFiles);

                    var fontFileLoaders = new List<IDWriteFontFileLoader>();
                    foreach (var fontFile in fontFiles)
                    {
                        fontFile.GetLoader(out var fontFileLoader);
                        if (fontFileLoader is IDWriteLocalFontFileLoader localFontFileLoader)
                        {
                            void* referenceKey;
                            uint keySize = 0;
                            fontFile.GetReferenceKey(&referenceKey, &keySize);

                            localFontFileLoader.GetFilePathFromKey(referenceKey, keySize, filePath, MAX_PATH);
                            files.Add(Path.GetFullPath(new string(filePath)).TrimEnd(Path.PathSeparator));
                        }
                    }
                }

                if (!fontFamilyInfo.ContainsKey(fontFamilyName) ||
                    fontFamilyInfo[fontFamilyName] == null ||
                    fontFamilyInfo[fontFamilyName].Count == 0)
                {
                    fontFamilyInfo[fontFamilyName] =
                        files
                        .Distinct()
                        .ToList()
                        .AsReadOnly();
                }
            }

            return fontFamilyInfo;
        }
    }
}
