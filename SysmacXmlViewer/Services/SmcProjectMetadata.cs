using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using SysmacXmlViewer.Models;

namespace SysmacXmlViewer.Services;

public sealed class SmcProjectMetadata
{
    private sealed record Symbol(string Type, string Comment, Dictionary<string, string> Elements);
    private sealed record Scope(string Name, Dictionary<string, Symbol> Symbols);
    private readonly Dictionary<string, Scope> _programs = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Dictionary<string, Symbol>> _types = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Symbol> _globals = new(StringComparer.OrdinalIgnoreCase);

    public static SmcProjectMetadata Load(string path, string deviceTracking)
    {
        if (!Guid.TryParse(deviceTracking, out var deviceId))
            throw new InvalidDataException("The backup has no valid device tracking ID.");
        using var archive = ZipFile.OpenRead(path);
        var matches = new List<(XElement Device, string Folder)>();
        foreach (var entry in archive.Entries.Where(e => e.FullName.EndsWith(".oem", StringComparison.OrdinalIgnoreCase)))
        {
            using var stream = entry.Open();
            var doc = ReadXml(stream);
            foreach (var device in doc.Descendants("Entity").Where(e => (string?)e.Attribute("type") == "Device"))
                if (Guid.TryParse((string?)device.Attribute("trackingId"), out var id) && id == deviceId)
                    matches.Add((device, entry.FullName[..(entry.FullName.LastIndexOf('/') + 1)]));
        }
        if (matches.Count != 1)
            throw new InvalidDataException("The project must contain exactly one device matching the backup tracking ID.");

        var result = new SmcProjectMetadata();
        var (selected, folder) = matches[0];
        var entities = selected.Descendants("Entity")
            .Where(e => e.Ancestors("Entity").FirstOrDefault(a => (string?)a.Attribute("type") == "Device") == selected)
            .ToList();
        foreach (var entity in entities)
        {
            var kind = (string?)entity.Attribute("type");
            if (kind is "Program" or "FunctionBlock" or "Function")
            {
                var symbols = new Dictionary<string, Symbol>(StringComparer.OrdinalIgnoreCase);
                foreach (var table in entity.Element("ChildEntities")?.Elements("Entity")
                    .Where(e => (string?)e.Attribute("type") == "Variables") ?? Enumerable.Empty<XElement>())
                    ReadSymbols(archive, folder, table, symbols);
                var name = (string?)entity.Attribute("name") ?? "";
                if (kind == "Program" && Guid.TryParse((string?)entity.Attribute("trackingId"), out var id))
                    result._programs.Add(id.ToString("N"), new Scope(name, symbols));
                else if (kind is "FunctionBlock" or "Function")
                    result._types.Add(name, symbols);
            }
            else if (kind == "Variables" && (string?)entity.Attribute("subtype") == "Global")
                ReadSymbols(archive, folder, entity, result._globals);
            else if (kind == "DataType")
            {
                var entry = archive.GetEntry(folder + (string?)entity.Attribute("id") + ".xml");
                if (entry == null) continue;
                using var stream = entry.Open();
                var doc = ReadXml(stream);
                foreach (var type in doc.Descendants("DataType").Where(e => e.Elements("DataType").Any() && (string?)e.Attribute("BaseType") == "STRUCT"))
                {
                    var members = new Dictionary<string, Symbol>(StringComparer.OrdinalIgnoreCase);
                    foreach (var member in type.Elements("DataType"))
                        members.Add((string?)member.Attribute("Name") ?? "", new Symbol(
                            (string?)member.Attribute("BaseType") ?? "", (string?)member.Attribute("Comment") ?? "", new()));
                    result._types.Add((string?)type.Attribute("Name") ?? "", members);
                }
            }
        }
        return result;
    }

    private static XDocument ReadXml(Stream stream)
    {
        using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
        return XDocument.Load(reader);
    }

    private static void ReadSymbols(ZipArchive archive, string folder, XElement entity, Dictionary<string, Symbol> symbols)
    {
        var entry = archive.GetEntry(folder + (string?)entity.Attribute("id") + ".xml")
            ?? throw new InvalidDataException("A project variable table is missing.");
        using var reader = new StreamReader(entry.Open());
        if (reader.ReadLine()?.TrimStart('\uFEFF') != "[SLWD version=1.0]")
            throw new InvalidDataException("Unsupported project variable table format.");
        while (reader.ReadLine() is { } line)
        {
            if (!line.StartsWith("++D=", StringComparison.Ordinal)) continue;
            var fields = line.Split('\t').Select(f => f.Split('=', 2))
                .Where(f => f.Length == 2).ToDictionary(f => f[0], f => f[1]);
            if (!fields.TryGetValue("N", out var name) || fields.GetValueOrDefault("G") == "VAR_EXTERNAL") continue;
            var elements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (fields.TryGetValue("EC", out var xml))
            {
                using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(Decode(xml)));
                foreach (var item in ReadXml(stream).Descendants("EC"))
                    elements.Add((string?)item.Attribute("EK") ?? "", (string?)item.Attribute("C") ?? "");
            }
            symbols.Add(Decode(name), new Symbol(Decode(fields["++D"]), Decode(fields.GetValueOrDefault("Com", "")), elements));
        }
    }

    private static string Decode(string value) => Regex.Replace(value, @"\$(\$|t|r|n)",
        m => m.Groups[1].Value switch { "$" => "$", "t" => "\t", "r" => "\r", "n" => "\n", _ => m.Value });

    public void Apply(IEnumerable<VariableItem> variables)
    {
        foreach (var variable in variables)
        {
            var path = variable.Name;
            variable.PouName = "";
            variable.Comment = "";
            variable.CommentSource = "Unresolved";
            if (variable.IsLocalVariable)
            {
                path = path[6..];
                var slash = path.IndexOf('/');
                if (slash > 0 && Guid.TryParse(path[..slash], out var id) && _programs.TryGetValue(id.ToString("N"), out var scope))
                {
                    variable.PouName = scope.Name;
                    path = scope.Name + path[slash..];
                    (variable.Comment, variable.CommentSource) = ResolveComment(scope.Symbols, path[(scope.Name.Length + 1)..]);
                }
            }
            else if (path.StartsWith("VAR:/", StringComparison.OrdinalIgnoreCase))
            {
                path = variable.VariablePath;
                (variable.Comment, variable.CommentSource) = ResolveComment(_globals, path);
            }
            variable.DisplayName = string.Join("/", path.Split('/', StringSplitOptions.RemoveEmptyEntries));
            variable.GroupName = variable.DisplayName.Split('/')[0];
            variable.MetadataApplied = true;
        }
    }

    private (string Text, string Source) ResolveComment(Dictionary<string, Symbol> symbols, string path)
    {
        var parts = Regex.Split(path, @"[/.](?![^\[]*\])");
        var comments = new List<string>();
        var sources = new List<string>();
        var resolved = false;
        for (var i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            var bracket = part.IndexOf('[');
            var name = bracket < 0 ? part : part[..bracket];
            if (!symbols.TryGetValue(name, out var symbol)) break;
            var suffix = bracket < 0 ? "" : part[bracket..];
            var remaining = suffix + (i + 1 < parts.Length
                ? (suffix.Length > 0 ? "." : "") + string.Join(".", parts.Skip(i + 1)) : "");
            if (symbol.Elements.TryGetValue(remaining, out var exact) && !string.IsNullOrWhiteSpace(exact))
            {
                comments.Add(exact);
                sources.Add(remaining.Contains('[') ? "Array element" : "Member");
                resolved = true;
                break;
            }
            var comment = symbol.Elements.GetValueOrDefault(suffix, symbol.Comment);
            resolved = i == parts.Length - 1;
            if (!string.IsNullOrWhiteSpace(comment))
            {
                comments.Add(comment);
                sources.Add(!resolved || (suffix.Length > 0 && !symbol.Elements.ContainsKey(suffix))
                    ? "Parent" : suffix.Length > 0 ? "Array element" : i == 0 ? "Variable" : "Member");
            }
            var type = Regex.Replace(symbol.Type, @"^ARRAY\s*\[.*?\]\s+OF\s+", "", RegexOptions.IgnoreCase);
            if (!_types.TryGetValue(type, out var members)) break;
            // Backup FB paths can repeat the instance name before its members.
            if (i + 1 < parts.Length && parts[i + 1].Equals(part, StringComparison.OrdinalIgnoreCase)
                && !members.ContainsKey(name)) i++;
            symbols = members;
        }
        return (string.Join(" / ", comments.Distinct()), comments.Count == 0
            ? resolved ? "No comment" : "Unresolved"
            : string.Join(" + ", sources.Distinct()) + (resolved ? "" : " (unresolved member)"));
    }
}
