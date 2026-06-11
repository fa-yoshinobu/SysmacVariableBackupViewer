using SysmacXmlViewer.Models;
using SysmacXmlViewer.Services;

namespace SysmacXmlViewer.Tests;

public class CsvExporterTests
{
    [Fact]
    public void ExportToCsv_QuotesAndEscapesEveryDataField()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.csv");
        try
        {
            var variables = new[]
            {
                new VariableItem
                {
                    DisplayName = "Name \"A\"",
                    DataType = "STRING",
                    Value = "4869",
                    Offset = "0",
                    GroupName = "Group,1"
                }
            };

            new CsvExporter().ExportToCsv(variables, tempFile);

            var lines = File.ReadAllLines(tempFile);
            Assert.Equal("Variable Name,Data Type,Value,Converted Value,Offset,Group Name,Array Index", lines[0]);
            Assert.Equal("\"Name \"\"A\"\"\",\"STRING\",\"4869\",\"Hi\",\"0\",\"Group,1\",\"\"", lines[1]);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
