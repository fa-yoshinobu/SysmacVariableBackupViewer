using System.Globalization;
using SysmacXmlViewer.Models;
using SysmacXmlViewer.Services;

namespace SysmacXmlViewer.Tests;

public class DisplayRegressionTests
{
    [Theory]
    [InlineData("Text", "STRING[32]", "")]
    [InlineData("Text[2]", "STRING[32]", "2")]
    [InlineData("Grid[1,2]", "INT", "1,2")]
    [InlineData("Buffer[-1]", "INT", "-1")]
    [InlineData("Items[2].Values[-3,4]", "REAL", "2 / -3,4")]
    public void ArrayIndexComesFromVariableName(string name, string type, string expected)
    {
        var item = new VariableItem { Name = name, DataType = type };
        Assert.Equal(expected, item.ArrayIndex);
        Assert.Equal(expected.Length > 0, item.IsArray);
    }

    [Theory]
    [InlineData(0.0000001)]
    [InlineData(1.23456789012345)]
    [InlineData(double.Epsilon)]
    [InlineData(double.MaxValue)]
    public void LrealDisplayAndCsvRetainValue(double value)
    {
        var item = new VariableItem { DataType = "LREAL", Value = Convert.ToHexString(BitConverter.GetBytes(value)) };
        Assert.Equal(value, double.Parse(item.ConvertedValueDisplay, CultureInfo.InvariantCulture));
        var path = Path.GetTempFileName();
        try
        {
            new CsvExporter().ExportToCsv(new[] { item }, path);
            Assert.Contains("\"" + value.ToString("R", CultureInfo.InvariantCulture) + "\"", File.ReadAllText(path));
        }
        finally { File.Delete(path); }
    }

    [Theory]
    [InlineData(0.0000001f)]
    [InlineData(float.Epsilon)]
    [InlineData(float.MaxValue)]
    public void RealDisplayRetainsValue(float value)
    {
        var item = new VariableItem { DataType = "REAL", Value = Convert.ToHexString(BitConverter.GetBytes(value)) };
        Assert.Equal(value, float.Parse(item.ConvertedValueDisplay, CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("-9223372036854775808", "-106751d23h47m16s854.775808ms")]
    [InlineData("9223372036854775807", "106751d23h47m16s854.775807ms")]
    [InlineData("999999999", "999.999999ms")]
    [InlineData("-1", "-0.000001ms")]
    [InlineData("0", "0.000ms")]
    public void TimeDisplayHandlesBoundsWithoutOverflow(string raw, string expected)
    {
        Assert.Equal(expected, new VariableItem { DataType = "TIME", Value = raw }.ConvertedValueDisplay);
    }
}
