using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

if (args.Length != 3)
    throw new ArgumentException("Usage: SourceRegistrations <snapshot Assets directory> <output directory> <compiler defines>");
var assets = Path.GetFullPath(args[0]);
var output = Path.GetFullPath(args[1]);
var options = new CSharpParseOptions(LanguageVersion.CSharp9, preprocessorSymbols: args[2].Split(';', StringSplitOptions.RemoveEmptyEntries));
using var files = new MemoryStream();
using var types = new MemoryStream();
var inventory = new SortedDictionary<string, object>(StringComparer.Ordinal);
int fileCount = 0, typeCount = 0;

void WriteInt(Stream stream, int value)
{
    Span<byte> bytes = stackalloc byte[4];
    BinaryPrimitives.WriteInt32BigEndian(bytes, value);
    stream.Write(bytes);
}
void WriteString(Stream stream, string value)
{
    var bytes = Encoding.UTF8.GetBytes(value);
    WriteInt(stream, bytes.Length);
    stream.Write(bytes);
}
string DeclarationName(SyntaxNode node) => node switch
{
    TypeDeclarationSyntax type => type.Identifier.ValueText,
    DelegateDeclarationSyntax declaration => declaration.Identifier.ValueText,
    _ => throw new InvalidDataException("Unsupported type declaration")
};
string Namespace(SyntaxNode node) => string.Join(".", node.Ancestors().Reverse().Select(parent => parent switch
{
    BaseNamespaceDeclarationSyntax space => space.Name.ToString(),
    TypeDeclarationSyntax type => type.Identifier.ValueText,
    _ => null
}).Where(name => name != null));
int Arity(SyntaxNode node) => node switch
{
    TypeDeclarationSyntax type => type.TypeParameterList?.Parameters.Count ?? 0,
    DelegateDeclarationSyntax declaration => declaration.TypeParameterList?.Parameters.Count ?? 0,
    _ => 0
};

foreach (var path in Directory.EnumerateFiles(assets, "*.cs", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
{
    var text = File.ReadAllText(path);
    var tree = CSharpSyntaxTree.ParseText(text, options, path);
    var errors = tree.GetDiagnostics().Where(item => item.Severity == DiagnosticSeverity.Error).ToArray();
    if (errors.Length != 0) throw new InvalidDataException(string.Join("\n", errors.Select(error => error.ToString())));
    var declarations = tree.GetRoot().DescendantNodes()
        .Where(node => node is TypeDeclarationSyntax or DelegateDeclarationSyntax)
        .Select(node => new { space = Namespace(node), name = DeclarationName(node), arity = Arity(node) })
        .Distinct().ToArray();
    if (declarations.Length == 0) continue;
    var ambiguous = declarations.GroupBy(item => item.space + "|" + item.name)
        .Where(group => group.Count() > 1).Select(group => group.Key).ToHashSet(StringComparer.Ordinal);
    var relative = "Assets/" + Path.GetRelativePath(assets, path).Replace('\\', '/');
    WriteInt(files, declarations.Length);
    WriteString(files, "\\" + relative.Replace('/', '\\'));
    foreach (var declaration in declarations)
    {
        var name = declaration.space + "|" + declaration.name;
        // Unity stores generic arities with the same source class name. Its
        // ambiguity marker distinguishes overloaded declarations in that file.
        types.WriteByte(ambiguous.Contains(name) ? (byte)1 : (byte)0);
        WriteString(types, name);
    }
    inventory[relative] = new { sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant(), declarations };
    fileCount++;
    typeCount += declarations.Length;
}
string ByteArray(byte[] bytes) => "new byte[] {\n" + string.Join("\n", bytes.Chunk(48)
    .Select(row => "            " + string.Join(",", row) + ",")) + "\n        }";
var source = """
// Generated from the source snapshot by tools/SourceRegistrations.
[System.Runtime.CompilerServices.CompilerGenerated]
[System.ComponentModel.EditorBrowsable((System.ComponentModel.EditorBrowsableState)1)]
[System.CodeDom.Compiler.GeneratedCode("Unity.MonoScriptGenerator.MonoScriptInfoGenerator", "")]
internal class UnitySourceGeneratedAssemblyMonoScriptTypes_v1
{
    private struct MonoScriptData
    {
        public byte[] FilePathsData;
        public byte[] TypesData;
        public int TotalTypes;
        public int TotalFiles;
        public bool IsEditorOnly;
    }
    private static MonoScriptData Get() => new MonoScriptData
    {
        FilePathsData = FILE_ARRAY,
        TypesData = TYPES_ARRAY,
        TotalFiles = FILE_COUNT,
        TotalTypes = TYPE_COUNT,
        IsEditorOnly = false
    };
}
""";
Directory.CreateDirectory(output);
var generated = Path.Combine(output, "MonoScriptRegistration.g.cs");
File.WriteAllText(generated, source.Replace("FILE_ARRAY", ByteArray(files.ToArray()))
    .Replace("TYPES_ARRAY", ByteArray(types.ToArray())).Replace("FILE_COUNT", fileCount.ToString())
    .Replace("TYPE_COUNT", typeCount.ToString()) + "\n");
var record = new
{
    source_files = fileCount, source_types = typeCount,
    file_paths_bytes = files.Length, types_bytes = types.Length,
    generated_sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(generated))).ToLowerInvariant(),
    inventory
};
File.WriteAllText(Path.Combine(output, "registration.json"), JsonSerializer.Serialize(record, new JsonSerializerOptions { WriteIndented = true }) + "\n");
Console.WriteLine(JsonSerializer.Serialize(new { source_files = fileCount, source_types = typeCount }));
