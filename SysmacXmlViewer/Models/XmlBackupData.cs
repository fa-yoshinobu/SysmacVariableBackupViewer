using System.Collections.Generic;

namespace SysmacXmlViewer.Models
{
    public class XmlBackupData
    {
        public ProjectInfo ProjectInfo { get; set; } = new();
        public List<VariableItem> Variables { get; set; } = new();
        public Dictionary<string, List<VariableItem>> VariablesByType { get; set; } = new();

        public XmlBackupData()
        {
            Variables = new List<VariableItem>();
            VariablesByType = new Dictionary<string, List<VariableItem>>();
        }
    }
} 