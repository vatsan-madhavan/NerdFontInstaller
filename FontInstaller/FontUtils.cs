// <copyright file="FontUtils.cs" company="Vatsan Madhavan">
// Copyright (c) Vatsan Madhavan. All rights reserved.
// </copyright>

namespace FontInstaller
{
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Windows.Win32.Foundation;
    using Windows.Win32.Graphics.Gdi;
    using static Windows.Win32.PInvoke;

    /// <summary>
    /// Contains font related functions.
    /// </summary>
    internal static class FontUtils
    {
        private static readonly HashSet<string> Fonts = new HashSet<string>();

        /// <summary>
        /// Initializes static members of the <see cref="FontUtils"/> class.
        /// </summary>
        static unsafe FontUtils()
        {
            var hdc = GetDC(new HWND(0));
            var logFont = new LOGFONTW
            {
                lfCharSet = (byte)DEFAULT_CHARSET,
                lfFaceName = string.Empty,
            };

            EnumFontFamiliesEx(hdc, &logFont, EnumProc, new LPARAM(0), 0);
            FontFaces = Fonts.ToList().AsReadOnly();
        }

        /// <summary>
        /// Gets a list of Font faces available on this system.
        /// </summary>
        internal static IReadOnlyList<string> FontFaces { get; }

        private static unsafe int EnumProc(LOGFONTW* logFont, TEXTMETRICW* textMetric, uint dwFlags, LPARAM lParam)
        {
            Fonts.Add(logFont->lfFaceName.ToString());
            return 1;
        }
    }
}
