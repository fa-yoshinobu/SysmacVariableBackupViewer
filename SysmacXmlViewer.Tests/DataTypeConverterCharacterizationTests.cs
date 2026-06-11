using SysmacXmlViewer.Services;

namespace SysmacXmlViewer.Tests;

public class DataTypeConverterCharacterizationTests
{
    [Theory]
    [InlineData("REAL", "0000803F", "1.000000")]
    [InlineData("REAL", "1.5", "1.500000")]
    [InlineData("LREAL", "000000000000F03F", "1.000000")]
    [InlineData("LREAL", "1.5", "1.500000")]
    public void ConvertValueToString_FloatingPointValuesUseInvariantF6(string dataType, string rawValue, string expected)
    {
        Assert.Equal(expected, DataTypeConverter.ConvertValueToString(dataType, rawValue));
    }

    [Theory]
    [InlineData("REAL", "0000803F00")]
    [InlineData("REAL", "ABC")]
    [InlineData("LREAL", "000000000000F03F00")]
    [InlineData("LREAL", "ABC")]
    public void ConvertValueToString_FloatingPointHexRequiresExactLength(string dataType, string rawValue)
    {
        Assert.Equal(rawValue, DataTypeConverter.ConvertValueToString(dataType, rawValue));
    }

    [Theory]
    [InlineData("DATE_AND_TIME", "1234567890", "1970-01-01-00:00:01.23")]
    [InlineData("DATE", "86400000000000", "1970-01-02")]
    [InlineData("TIME_OF_DAY", "45296780000000", "12:34:56.78")]
    public void ConvertValueToString_DateAndTimeValuesUseFixedFormats(string dataType, string rawValue, string expected)
    {
        Assert.Equal(expected, DataTypeConverter.ConvertValueToString(dataType, rawValue));
    }

    [Fact]
    public void ConvertValueToString_TimeAllowsDurationsLongerThanOneDay()
    {
        Assert.Equal("1d1h1m1s500.000ms", DataTypeConverter.ConvertValueToString("TIME", "90061500000000"));
        Assert.True(DataTypeConverter.IsValidValue("TIME", "90061500000000"));
    }

    [Fact]
    public void ConvertValueToString_StringHexUsesUtf8ButStringArrayReturnsRawValue()
    {
        Assert.Equal("Hello", DataTypeConverter.ConvertValueToString("STRING", "48656C6C6F"));
        Assert.Equal("48656C6C6F", DataTypeConverter.ConvertValueToString("STRING[5]", "48656C6C6F"));
    }

    [Fact]
    public void ConvertStringToDataTypeValue_DateAndTimeRestoresNanosecondTicks()
    {
        Assert.Equal("1230000000", DataTypeConverter.ConvertStringToDataTypeValue("DATE_AND_TIME", "1970-01-01-00:00:01.23"));
        Assert.Equal("86400000000000", DataTypeConverter.ConvertStringToDataTypeValue("DATE", "1970-01-02"));
        Assert.Equal("45296780000000", DataTypeConverter.ConvertStringToDataTypeValue("TIME_OF_DAY", "12:34:56.78"));
    }

    [Fact]
    public void GetDataTypeDisplayNameNormalizesStringArray()
    {
        Assert.Equal("STRING", DataTypeConverter.GetDataTypeDisplayName("STRING[20]"));
    }
}
