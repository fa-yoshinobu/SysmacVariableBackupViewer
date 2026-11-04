using System.Globalization;
using SysmacXmlViewer.Services;

namespace SysmacXmlViewer.Tests;

public class ErrorMessagesTests
{
    [Fact]
    public void LocalizedSystemDetailsAreNotShown()
    {
        var errors = new Exception[]
        {
            new IOException("日本語の詳細"), new UnauthorizedAccessException("日本語の詳細"),
            new System.Xml.XmlException("日本語の詳細", null, 3, 7),
            new InvalidDataException("日本語の詳細"), new Exception("日本語の詳細")
        };
        foreach (var error in errors)
        {
            var message = ErrorMessages.Describe(error);
            Assert.NotEmpty(message);
            Assert.All(message, c => Assert.True(c < 128));
        }
        Assert.Contains("line 3, position 7", ErrorMessages.Describe(errors[2]));
    }

    [Theory]
    [InlineData("<Content/>", "The Header element is missing.")]
    [InlineData("<Other/>", "The Sysmac backup Content element is missing.")]
    [InlineData("<Content>", "The XML is invalid")]
    public void XmlFailuresRemainEnglishUnderJapaneseCulture(string xml, string expected)
    {
        var previous = CultureInfo.CurrentUICulture;
        var path = Path.GetTempFileName();
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("ja-JP");
            File.WriteAllText(path, xml);
            var error = Assert.Throws<XmlParseException>(() => new XmlParser().ParseXmlFile(path));
            Assert.Contains(expected, error.Message);
            Assert.Contains(expected, ErrorMessages.Describe(error));
            Assert.All(error.Message, c => Assert.True(c < 128));
        }
        finally { CultureInfo.CurrentUICulture = previous; File.Delete(path); }
    }
}
