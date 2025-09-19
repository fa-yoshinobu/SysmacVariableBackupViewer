using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SysmacXmlViewer.Models;

namespace SysmacXmlViewer.Services
{
    public class XmlWriter
    {
        public void SaveToXml(XmlBackupData data, string filePath)
        {
            var doc = new XDocument(
                new XElement("Content",
                    CreateHeader(data.ProjectInfo),
                    CreateBody(data.Variables)
                )
            );
            
            doc.Save(filePath);
        }
        
        private XElement CreateHeader(ProjectInfo projectInfo)
        {
            return new XElement("Header",
                new XElement("Version", projectInfo.Version),
                new XElement("EnableOffset", projectInfo.EnableOffset),
                new XElement("Project",
                    new XAttribute("ID", projectInfo.Id),
                    new XAttribute("Name", projectInfo.Name),
                    new XAttribute("Tracking", projectInfo.Tracking),
                    new XElement("DeviceType", projectInfo.DeviceType),
                    new XElement("DeviceModelFull", projectInfo.DeviceModelFull),
                    new XElement("UnitVersion", projectInfo.UnitVersion)
                ),
                new XElement("CjMemorySettings",
                    new XElement("Area", new XAttribute("Type", "HR"), new XAttribute("Size", "0")),
                    new XElement("Area", new XAttribute("Type", "DM"), new XAttribute("Size", "0")),
                    new XElement("Area", new XAttribute("Type", "EM"), new XAttribute("Size", "0"))
                )
            );
        }
        
        private XElement CreateBody(List<VariableItem> variables)
        {
            return new XElement("Body",
                new XElement("RetainVariable",
                    variables.Select(v => new XElement("Item",
                    new XAttribute("Name", v.Name),
                    new XAttribute("DataType", v.DataType),
                    !string.IsNullOrEmpty(v.Offset) ? new XAttribute("Offset", v.Offset) : null,
                    // 解析済みの生値をそのまま保存（表示値→生値の再変換は行わない）
                    new XElement("Data", v.Value)
                    ))
                )
            );
        }
    }
} 
