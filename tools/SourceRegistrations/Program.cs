using Mono.Cecil;using System.Text;using System.Security.Cryptography;using System.Text.Json;
if (args.Length != 2) throw new ArgumentException("Usage: SourceRegistrations <publisher Assembly-CSharp.dll> <output directory>");
if (Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(args[0]))).ToLowerInvariant() != "57d0517f1e4c7bd6211d35fdc1806c19ed5ed39b3d48c4a52662cf052b038db6") throw new InvalidDataException("Unsupported publisher assembly.");
using var m=ModuleDefinition.ReadModule(args[0]);var details=m.Types.Single(t=>t.Name=="<PrivateImplementationDetails>");
var files=details.Fields.Single(f=>f.Name=="3A2E61BFFCB901162B12054190A974F6DC899138D429EE80C8AFDA33AB285F42").InitialValue;
var types=details.Fields.Single(f=>f.Name=="CEE5C9DB7CEBECC2E6919965F7E99A598D75DFF5EA970B97FF8264E533555F7D").InitialValue;
var output=args[1];Directory.CreateDirectory(output);
string Array(byte[] bytes)=>"new byte[] {\n"+string.Join("\n",bytes.Chunk(48).Select(row=>"            "+string.Join(",",row)+","))+"\n        }";
var source="""
// Reconstructed from Unity-generated metadata in publisher MDPro3 1.4.7-2.
// Gameplay and asset scripts remain compiled from the matching upstream commit.
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
        TotalFiles = 2599,
        TotalTypes = 3597,
        IsEditorOnly = false
    };
}
""";
File.WriteAllText(Path.Combine(output,"MonoScriptRegistration.g.cs"),source.Replace("FILE_ARRAY",Array(files)).Replace("TYPES_ARRAY",Array(types)));
File.WriteAllText(Path.Combine(output,"registration-provenance.json"),JsonSerializer.Serialize(new{publisher_sha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(args[0]))).ToLowerInvariant(),file_paths_bytes=files.Length,file_paths_sha256=Convert.ToHexString(SHA256.HashData(files)).ToLowerInvariant(),types_bytes=types.Length,types_sha256=Convert.ToHexString(SHA256.HashData(types)).ToLowerInvariant()},new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine("generated registration arrays "+files.Length+" "+types.Length);
