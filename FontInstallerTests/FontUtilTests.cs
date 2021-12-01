// <copyright file="FontUtilTests.cs" company="Vatsan Madhavan">
// Copyright (c) Vatsan Madhavan. All rights reserved.
// </copyright>

namespace FontInstallerTests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using FontInstaller;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// Contains tests for <see cref="FontUtils"/>.
    /// </summary>
    public class FontUtilTests
    {
        private ITestOutputHelper output;

        /// <summary>
        /// Initializes a new instance of the <see cref="FontUtilTests"/> class.
        /// </summary>
        /// <param name="output"><see cref="ITestOutputHelper"/> instance.</param>
        public FontUtilTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        /// <summary>
        /// Validates <see cref="FontUtils.GetFontFaceNames(string)"/>.
        /// </summary>
        [Fact]
        public void GetFontFaceNamesTest()
        {
            string baseLocation = Uri.UnescapeDataString(new Uri(Assembly.GetExecutingAssembly().Location).AbsolutePath);
            var fontsDir =
                Path.Combine(
                    Path.GetDirectoryName(baseLocation) ?? string.Empty,
                    "Fonts");

            Assert.True(Directory.Exists(fontsDir));
            this.output.WriteLine($"Fonts dir: {fontsDir}");

            var ttfs = Directory.EnumerateFiles(fontsDir, "*.ttf");
            foreach (var ttf in ttfs)
            {
                this.output.WriteLine($"Testing {ttf}");

                var faceNames = FontUtils.GetFontFaceNames(ttf);
                Assert.NotNull(faceNames);
                Assert.All(
                    faceNames,
                    (faceName) =>
                    {
                        this.output.WriteLine($"\tFaceName = {faceName}");
                        Assert.True(!string.IsNullOrWhiteSpace(faceName), $"A face name for {ttf} was null or empty");
                    });
            }
        }

        /// <summary>
        /// Tests for <see cref="FontUtils.IsInstalled(string)"/>.
        /// </summary>
        [Fact]
        public void IsInstalledTest()
        {
            var systemFontsFolder = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
            var fontFile = Directory.GetFiles(systemFontsFolder, "*.ttf")?.FirstOrDefault() ?? string.Empty;

            Assert.True(!string.IsNullOrEmpty(fontFile));

            this.output.WriteLine($"Using font file: {fontFile}");
            Assert.True(FontUtils.IsInstalled(fontFile));
        }

        /// <summary>
        /// Tests for <see cref="FontUtils.FontFaces"/>.
        /// </summary>
        [Fact]
        public void EnumerateFaceNamesTest()
        {
            Assert.NotNull(FontUtils.FontFaces);
            Assert.All(
                FontUtils.FontFaces,
                (f) =>
                {
                    this.output.WriteLine($"Face Name: '{f.Key}'");
                    Assert.NotNull(f.Value);
                    Assert.All(
                        f.Value,
                        (p) =>
                        {
                            Assert.True(!string.IsNullOrEmpty(p));
                            this.output.WriteLine($"\t{p}");
                            Assert.True(File.Exists(p));
                        });
                });
        }
    }
}