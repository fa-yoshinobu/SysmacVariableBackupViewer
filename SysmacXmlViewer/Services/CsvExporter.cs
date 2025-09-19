using System.Collections.Generic;
using System.IO;
using SysmacXmlViewer.Models;

namespace SysmacXmlViewer.Services
{
    public class CsvExporter
    {
        public void ExportToCsv(IEnumerable<VariableItem> variables, string filePath)
        {
            using var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8);
            
            // ヘッダー行
            writer.WriteLine("Variable Name,Data Type,Value,Converted Value,Offset,Group Name,Array Index");
            
            // データ行
            foreach (var variable in variables)
            {
                writer.WriteLine(
                    $"\"{EscapeCsv(variable.DisplayName)}\"," +
                    $"\"{EscapeCsv(variable.DataTypeDisplayName)}\"," +
                    $"\"{EscapeCsv(variable.Value)}\"," +
                    $"\"{EscapeCsv(variable.ConvertedValueDisplay)}\"," +
                    $"\"{EscapeCsv(variable.Offset)}\"," +
                    $"\"{EscapeCsv(variable.GroupName)}\"," +
                    $"\"{EscapeCsv(variable.ArrayIndex)}\"");
            }
        }

        private static string EscapeCsv(string value)
        {
            if (value is null) return string.Empty;
            // RFC4180 準拠: 二重引用符は2つにエスケープ
            return value.Replace("\"", "\"\"");
        }
    }
}
