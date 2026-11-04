using System.IO.Compression;
using System.Xml.Linq;
using SysmacXmlViewer.Models;
using SysmacXmlViewer.Services;

namespace SysmacXmlViewer.Tests;

public sealed class SmcProjectMetadataTests : IDisposable
{
    private const string Device = "11111111-1111-4111-8111-111111111111";
    private const string Program = "22222222222242228222222222222222";
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.smc2");

    public SmcProjectMetadataTests()
    {
        using var zip = ZipFile.Open(_path, ZipArchiveMode.Create);
        Write(zip, "project/project.oem", $"""
            <Root><Entity type="Device" trackingId="{Device}"><ChildEntities>
              <Entity type="Program" name="DemoProgram" trackingId="{Program}"><ChildEntities>
                <Entity type="Variables" id="locals" />
              </ChildEntities></Entity>
              <Entity type="FunctionBlock" name="DemoBlock"><ChildEntities>
                <Entity type="Variables" id="block" />
              </ChildEntities></Entity>
              <Entity type="Variables" subtype="Global" id="globals" />
              <Entity type="DataType" id="types" />
              <Entity type="Device" trackingId="33333333-3333-4333-8333-333333333333"><ChildEntities>
                <Entity type="Variables" subtype="Global" id="other" />
              </ChildEntities></Entity>
            </ChildEntities></Entity></Root>
            """);
        Write(zip, "project/locals.xml", "[SLWD version=1.0]\n" +
            "++D=ARRAY[0..3] OF INT\tN=Values\tG=VAR\tCom=配列コメント\tEC=<ECs><EC EK=\"[1]\" C=\"要素 &amp; コメント\" /></ECs>\n" +
            "++D=INT\tN=Shared\tG=VAR\tCom=Local$tcomment$$n\n" +
            "++D=DemoBlock\tN=Instance\tG=VAR\tCom=FB instance\n" +
            "++D=DemoStruct\tN=Record\tG=VAR\tCom=Record comment\tEC=<ECs><EC EK=\"Field\" C=\"Instance member comment\" /></ECs>\n" +
            "++D=INT\tN=GlobalValue\tG=VAR_EXTERNAL\tCom=Do not use external declaration\n");
        Write(zip, "project/globals.xml", "[SLWD version=1.0]\n++D=INT\tN=Shared\tG=VAR_GLOBAL\tCom=Global comment\n");
        Write(zip, "project/block.xml", "[SLWD version=1.0]\n++D=INT\tN=Count\tG=VAR\tCom=Counter comment\n");
        Write(zip, "project/types.xml", "<data><DataType><DataType Name=\"DemoStruct\" BaseType=\"STRUCT\"><DataType Name=\"Field\" BaseType=\"INT\" Comment=\"Member comment\" /></DataType></DataType></data>");
    }

    private static void Write(ZipArchive zip, string name, string text)
    {
        using var writer = new StreamWriter(zip.CreateEntry(name).Open());
        writer.Write(text);
    }

    [Theory]
    [InlineData("Values[0]", "配列コメント")]
    [InlineData("Values[1]", "要素 & コメント")]
    [InlineData("Shared", "Local\tcomment$n")]
    [InlineData("Instance/Instance/Count", "FB instance / Counter comment")]
    [InlineData("Record.Field", "Instance member comment")]
    [InlineData("Unknown", "")]
    [InlineData("GlobalValue", "")]
    public void ResolvesLocalMetadataWithoutChangingBackup(string path, string comment)
    {
        var original = $"VAR://{Program}/{path}";
        var item = new VariableItem { Name = original, DataType = "INT", Value = "17", Offset = "4" };
        var metadata = SmcProjectMetadata.Load(_path, Device);
        metadata.Apply(new[] { item });
        Assert.Equal("DemoProgram/" + path, item.DisplayName);
        Assert.Equal("DemoProgram", item.GroupName);
        Assert.Equal("DemoProgram", item.PouName);
        Assert.Equal(comment, item.Comment);
        Assert.Equal(original, item.Name);
        Assert.Equal("17", item.Value);
        Assert.Equal("INT", item.DataType);
        Assert.Equal("4", item.Offset);
        var output = Path.GetTempFileName();
        try
        {
            new SysmacXmlViewer.Services.XmlWriter().SaveToXml(new XmlBackupData { Variables = new() { item } }, output);
            Assert.Equal(original, (string?)XDocument.Load(output).Descendants("Item").Single().Attribute("Name"));
        }
        finally { File.Delete(output); }
    }

    [Theory]
    [InlineData("Values[0]", "Parent")]
    [InlineData("Values[1]", "Array element")]
    [InlineData("Shared", "Variable")]
    [InlineData("Instance/Instance/Count", "Parent + Member")]
    [InlineData("Record.Field", "Member")]
    [InlineData("Unknown", "Unresolved")]
    public void IdentifiesCommentSource(string path, string source)
    {
        var item = new VariableItem { Name = $"VAR://{Program}/{path}" };
        SmcProjectMetadata.Load(_path, Device).Apply(new[] { item });
        Assert.Equal(source, item.CommentSource);
        Assert.Equal(path, item.VariableNameDisplay);
        Assert.Equal("DemoProgram", item.ScopeDisplay);
    }

    [Theory]
    [InlineData("VAR://Shared", "Shared")]
    [InlineData("VAR:/Shared", "Shared")]
    [InlineData("VAR://_DemoFlag", "_DemoFlag")]
    [InlineData("VAR://Record/Field", "Record/Field")]
    public void GlobalNamesAreNotMistakenForPouIds(string name, string display)
    {
        var item = new VariableItem { Name = name };
        Assert.False(item.IsLocalVariable);
        Assert.Equal("Global", item.ScopeKey);
        Assert.Equal("Global", item.ScopeDisplay);
        Assert.Equal(name, item.VariableNameDisplay);
        SmcProjectMetadata.Load(_path, Device).Apply(new[] { item });
        Assert.Equal(name, item.Name);
        Assert.Equal(display, item.VariableNameDisplay);
        Assert.Equal("Global", item.ScopeDisplay);
        if (display == "Shared") Assert.Equal("Global comment", item.Comment);
    }

    [Fact]
    public void RawLocalIdentifierIsVisibleUntilProjectIsApplied()
    {
        var item = new VariableItem { Name = $"VAR://{Program}/Values[1]" };
        Assert.Equal(item.Name, item.VariableNameDisplay);
        Assert.False(item.MetadataApplied);
        SmcProjectMetadata.Load(_path, Device).Apply(new[] { item });
        Assert.True(item.MetadataApplied);
        Assert.Equal("Values[1]", item.VariableNameDisplay);
        Assert.Equal("DemoProgram", item.ScopeDisplay);
    }

    [Fact]
    public void UnmatchedLocalIdDoesNotBecomeGlobal()
    {
        var item = new VariableItem { Name = "VAR://44444444444444448444444444444444/Example" };
        Assert.True(item.IsLocalVariable);
        Assert.NotEqual("Global", item.ScopeKey);
        Assert.Equal(item.Name, item.VariableNameDisplay);
        Assert.Empty(item.ScopeDisplay);
    }

    [Fact]
    public void KnownSymbolWithoutCommentDiffersFromUnresolved()
    {
        using (var zip = ZipFile.Open(_path, ZipArchiveMode.Update))
        {
            zip.GetEntry("project/globals.xml")!.Delete();
            Write(zip, "project/globals.xml", "[SLWD version=1.0]\n++D=INT\tN=Empty\tG=VAR_GLOBAL\n");
        }
        var known = new VariableItem { Name = "VAR:/Empty" };
        var unknown = new VariableItem { Name = "VAR:/Unknown" };
        SmcProjectMetadata.Load(_path, Device).Apply(new[] { known, unknown });
        Assert.Equal("No comment", known.CommentSource);
        Assert.Equal("Unresolved", unknown.CommentSource);
    }

    [Fact]
    public void GlobalsDoNotUseLocalCommentsAndUnknownProgramsRemainUnresolved()
    {
        var global = new VariableItem { Name = "VAR:/Shared" };
        var unknown = new VariableItem { Name = "VAR://44444444444444448444444444444444/Shared", Comment = "stale", PouName = "stale" };
        SmcProjectMetadata.Load(_path, Device).Apply(new[] { global, unknown });
        Assert.Equal("Global comment", global.Comment);
        Assert.Equal("Shared", global.DisplayName);
        Assert.Empty(global.PouName);
        Assert.Empty(unknown.Comment);
        Assert.Empty(unknown.PouName);
        Assert.Equal(unknown.Name[6..], unknown.DisplayName);
    }

    [Fact]
    public void RejectsDifferentDeviceAndMissingTracking()
    {
        Assert.Throws<InvalidDataException>(() => SmcProjectMetadata.Load(_path, "55555555-5555-4555-8555-555555555555"));
        Assert.Throws<InvalidDataException>(() => SmcProjectMetadata.Load(_path, ""));
    }

    [Fact]
    public void RejectsMalformedCommentXml()
    {
        using (var zip = ZipFile.Open(_path, ZipArchiveMode.Update))
        {
            zip.GetEntry("project/locals.xml")!.Delete();
            Write(zip, "project/locals.xml", "[SLWD version=1.0]\n++D=INT\tN=Value\tEC=<invalid>\n");
        }
        Assert.Throws<System.Xml.XmlException>(() => SmcProjectMetadata.Load(_path, Device));
    }

    public void Dispose() => File.Delete(_path);
}
