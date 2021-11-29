// <copyright file="DWriteExtensions.cs" company="Vatsan Madhavan">
// Copyright (c) Vatsan Madhavan. All rights reserved.
// </copyright>

namespace FontInstaller
{
    using System.Buffers;
    using Windows.Win32.Foundation;
    using Windows.Win32.Graphics.DirectWrite;
    using static Windows.Win32.PInvoke;

    /// <summary>
    /// Extension methods related to DWrite interfaces.
    /// </summary>
    internal static class DWriteExtensions
    {
        private static readonly PWSTR EnUsLocaleName;
        private static readonly PWSTR UserDefaultLocaleName;

        static unsafe DWriteExtensions()
        {
            fixed (char* enUs = "en-us")
            {
                EnUsLocaleName = enUs;
            }

            char* localeName = stackalloc char[(int)LOCALE_NAME_MAX_LENGTH];
            if (GetUserDefaultLocaleName(localeName, (int)LOCALE_NAME_MAX_LENGTH) == 0)
            {
                UserDefaultLocaleName = localeName;
            }
            else
            {
                UserDefaultLocaleName = EnUsLocaleName;
            }
        }

        /// <summary>
        /// Gets the string representation of a DWrite string for the given locale.
        /// </summary>
        /// <param name="localizedStrings">DWrite Localized strings instance.</param>
        /// <param name="locale">locale of interest.</param>
        /// <returns>String if found, otherwise an empty string.</returns>
        internal static unsafe string ToString(
            this IDWriteLocalizedStrings localizedStrings,
            PWSTR locale)
        {
            uint localeIndex = 0;
            BOOL nameExists = false;
            localizedStrings.FindLocaleName(locale, &localeIndex, &nameExists);
            if (!nameExists)
            {
                localizedStrings.FindLocaleName(EnUsLocaleName, &localeIndex, &nameExists);
                if (!nameExists)
                {
                    // Fall back to index = 0
                    localeIndex = 0;
#if DEBUG
                    System.Diagnostics.Trace.WriteLine($"WARNING: Failed to find entry for locale {locale.ToString()}");
#endif
                }
            }

            uint stringLength = 0;
            localizedStrings.GetStringLength(localeIndex, &stringLength);

            if (stringLength == 0)
            {
                return string.Empty;
            }

            char[] buffer = ArrayPool<char>.Shared.Rent((int)stringLength + 1);
            try
            {
                fixed (char* fontFamilyName = buffer)
                {
                    localizedStrings.GetString(localeIndex, fontFamilyName, stringLength + 1);
                    return new string(fontFamilyName);
                }
            }
            finally
            {
                ArrayPool<char>.Shared.Return(buffer, clearArray: true);
            }
        }

        /// <summary>
        /// Gets the string representation of a DWrite string for the current default locale.
        /// </summary>
        /// <param name="localizedStrings">DWrite Localized strings instance.</param>
        /// <returns>String if found, otherwise an empty string.</returns>
        internal static unsafe string ToUserDefaultLocaleString(this IDWriteLocalizedStrings localizedStrings)
        {
            return localizedStrings.ToString(UserDefaultLocaleName);
        }
    }
}
