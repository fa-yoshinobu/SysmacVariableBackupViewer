using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SysmacXmlViewer.Models;

namespace SysmacXmlViewer.Services
{
    public class XmlParser
    {
        public XmlBackupData ParseXmlFile(string filePath)
        {
            var data = new XmlBackupData();
            
            try
            {
                var doc = XDocument.Load(filePath);
                var content = doc.Root;
                
                if (content == null)
                {
                    throw new InvalidOperationException("XMLファイルのルート要素が見つかりません。");
                }

                // ヘッダー情報の解析
                var header = content.Element("Header");
                if (header != null)
                {
                    data.ProjectInfo = ParseHeader(header);
                }
                
                // 変数情報の解析
                var retainVariable = content.Element("Body")?.Element("RetainVariable");
                if (retainVariable != null)
                {
                    foreach (var item in retainVariable.Elements("Item"))
                    {
                        var variable = ParseVariableItem(item);
                        data.Variables.Add(variable);
                        
                        // データ型別にグループ化
                        if (!data.VariablesByType.ContainsKey(variable.DataType))
                        {
                            data.VariablesByType[variable.DataType] = new List<VariableItem>();
                        }
                        data.VariablesByType[variable.DataType].Add(variable);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new XmlParseException($"XMLファイルの解析に失敗しました: {ex.Message}", ex);
            }
            
            return data;
        }
        
        private ProjectInfo ParseHeader(XElement header)
        {
            var project = header.Element("Project");
            if (project == null)
            {
                throw new InvalidOperationException("Project要素が見つかりません。");
            }

                return new ProjectInfo
                {
                Id = project.Attribute("ID")?.Value ?? string.Empty,
                Name = project.Attribute("Name")?.Value ?? string.Empty,
                Tracking = project.Attribute("Tracking")?.Value ?? string.Empty,
                DeviceType = project.Element("DeviceType")?.Value ?? string.Empty,
                DeviceModelFull = project.Element("DeviceModelFull")?.Value ?? string.Empty,
                UnitVersion = project.Element("UnitVersion")?.Value ?? string.Empty,
                Version = header.Element("Version")?.Value ?? string.Empty,
                EnableOffset = bool.TryParse(header.Element("EnableOffset")?.Value, out var enable)
                    ? enable
                    : false
                };
        }
        
        private VariableItem ParseVariableItem(XElement item)
        {
            var name = item.Attribute("Name")?.Value ?? string.Empty;
            var dataType = item.Attribute("DataType")?.Value ?? string.Empty;
            var rawValue = item.Element("Data")?.Value ?? string.Empty;
            
            return new VariableItem
            {
                Name = name,
                DataType = dataType,
                Offset = item.Attribute("Offset")?.Value ?? string.Empty,
                Value = rawValue, // 元の値をそのまま設定
                DisplayName = NormalizeDisplayPath(name),
                GroupName = GetGroupNameFromDisplayPath(NormalizeDisplayPath(name))
            };
        }

        // 表示用パスの正規化: 先頭の "VAR:/" や "VAR://<seg>/" を除去し、重複スラッシュを整理
        private string NormalizeDisplayPath(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return string.Empty;

            string s = fullName;
            if (s.StartsWith("VAR://", StringComparison.OrdinalIgnoreCase))
            {
                // Keep GUID (program name) after VAR://
                s = s.Substring("VAR://".Length);
            }
            else if (s.StartsWith("VAR:/", StringComparison.OrdinalIgnoreCase))
            {
                s = s.Substring("VAR:/".Length);
            }

            var parts = s.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
            return string.Join("/", parts);
        }

        // 表示用パスからグループ名（最初のセグメント）を抽出
        private string GetGroupNameFromDisplayPath(string displayPath)
        {
            if (string.IsNullOrEmpty(displayPath)) return string.Empty;
            var idx = displayPath.IndexOf('/');
            return idx > 0 ? displayPath.Substring(0, idx) : displayPath;
        }
        
        private string ExtractDisplayName(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return string.Empty;
            
            // VAR://GUID/部分を除去
            var parts = fullName.Split('/');
            if (parts.Length > 1)
            {
                return string.Join("/", parts.Skip(1));
            }
            return fullName;
        }
        
        private string ExtractGroupName(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return string.Empty;
            
            var parts = fullName.Split('/');
            if (parts.Length > 2)
            {
                return parts[1]; // 最初の階層名をグループ名として使用
            }
            return "その他";
        }
    }

    public class XmlParseException : Exception
    {
        public XmlParseException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
    }
} 
