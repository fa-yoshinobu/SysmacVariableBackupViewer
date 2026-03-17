using Xunit;
using SysmacXmlViewer.Services;

namespace SysmacXmlViewer.Tests
{
    public class DataTypeConverterTests
    {
        [Theory]
        [InlineData("BOOL", "True", "True")]
        [InlineData("BOOL", "False", "False")]
        [InlineData("BOOL", "1", "True")]
        [InlineData("BOOL", "0", "False")]
        [InlineData("UINT", "123", "123")]
        [InlineData("INT", "-123", "-123")]
        [InlineData("WORD", "65535", "65535")]
        public void ConvertValueToString_BasicTypes_ReturnsExpected(string dataType, string rawValue, string expected)
        {
            var result = DataTypeConverter.ConvertValueToString(dataType, rawValue);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ConvertValueToString_UnknownType_ReturnsOriginalValue()
        {
            var result = DataTypeConverter.ConvertValueToString("UNKNOWN", "SomeValue");
            Assert.Equal("SomeValue", result);
        }

        [Fact]
        public void ConvertValueToString_EmptyValue_ReturnsEmptyString()
        {
            var result = DataTypeConverter.ConvertValueToString("INT", "");
            Assert.Equal(string.Empty, result);
        }
    }
}
