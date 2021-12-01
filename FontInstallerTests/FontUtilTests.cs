using FontInstaller;
using System;
using System.IO;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;
using System.Linq;

namespace FontInstallerTests
{
    public class FontUtilTests
    {
        private ITestOutputHelper output;

        public FontUtilTests(ITestOutputHelper output)
        {
            this.output = output;
        }

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

        [Fact]
        public void IsInstalledTest()
        {
            var systemFontsFolder = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
            var fontFile = Directory.GetFiles(systemFontsFolder, "*.ttf")?.FirstOrDefault() ?? String.Empty;

            Assert.True(!string.IsNullOrEmpty(fontFile));

            this.output.WriteLine($"Using font file: {fontFile}");
            Assert.True(FontUtils.IsInstalled(fontFile));
        }
    }
}