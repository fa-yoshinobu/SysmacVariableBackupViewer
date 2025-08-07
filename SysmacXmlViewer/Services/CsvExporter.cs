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
                writer.WriteLine($"\"{variable.DisplayName}\",\"{variable.DataTypeDisplayName}\",\"{variable.Value}\",\"{variable.ConvertedValueDisplay}\",\"{variable.Offset}\",\"{variable.GroupName}\",\"{variable.ArrayIndex}\"");
            }
        }
    }
} 