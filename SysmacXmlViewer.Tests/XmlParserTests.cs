using System.Xml.Linq;
using Xunit;
using SysmacXmlViewer.Services;
using System.IO;

namespace SysmacXmlViewer.Tests
{
    public class XmlParserTests
    {
        private const string SampleXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Root>
  <Header>
    <Project ID=""{123}"" Name=""TestProject"" Tracking=""1"">
      <DeviceType>NJ501</DeviceType>
      <DeviceModelFull>NJ501-1300</DeviceModelFull>
      <UnitVersion>1.40</UnitVersion>
    </Project>
    <Version>1.0</Version>
    <EnableOffset>True</EnableOffset>
  </Header>
  <Body>
    <RetainVariable>
      <Item Name=""Var1"" DataType=""BOOL"" Offset=""0"">
        <Data>True</Data>
      </Item>
      <Item Name=""Var2"" DataType=""INT"" Offset=""1"">
        <Data>100</Data>
      </Item>
    </RetainVariable>
  </Body>
</Root>";

        [Fact]
        public void ParseXmlFile_ValidXml_ReturnsCorrectData()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, SampleXml);
            var parser = new XmlParser();

            try
            {
                // Act
                var result = parser.ParseXmlFile(tempFile);

                // Assert
                Assert.NotNull(result);
                Assert.Equal("TestProject", result.ProjectInfo.Name);
                Assert.Equal(2, result.Variables.Count);
                Assert.Equal("Var1", result.Variables[0].Name);
                Assert.Equal("BOOL", result.Variables[0].DataType);
                Assert.Equal("Var2", result.Variables[1].Name);
                Assert.Equal("INT", result.Variables[1].DataType);
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }
    }
}
