using System.Security.Cryptography;
using System.Text.Json;
using Mono.Cecil;
using Mono.Cecil.Cil;

if (args.Length != 4)
    throw new ArgumentException("Usage: SourceCompatibility <publisher Managed> <compiled Assembly-CSharp.dll> <original source root> <report.json>");
var managed = Path.GetFullPath(args[0]);
var compiled = Path.GetFullPath(args[1]);
var originalRoot = Path.GetFullPath(args[2]);
using var resolver = new DefaultAssemblyResolver();
resolver.AddSearchDirectory(managed);
var parameters = new ReaderParameters { AssemblyResolver = resolver, InMemory = true };
using var baseline = AssemblyDefinition.ReadAssembly(Path.Combine(managed, "Assembly-CSharp.dll"), parameters);
using var rebuilt = AssemblyDefinition.ReadAssembly(compiled, parameters);
IEnumerable<TypeDefinition> Flatten(TypeDefinition type) {
    yield return type;
    foreach (var child in type.NestedTypes)
        foreach (var item in Flatten(child)) yield return item;
}
var previous = baseline.MainModule.Types.SelectMany(Flatten).ToArray();
var current = rebuilt.MainModule.Types.SelectMany(Flatten).ToDictionary(t => t.FullName);
bool Generated(TypeDefinition t) => t.Name.StartsWith("<") || t.Name.Contains("AnonymousType")
    || (t.DeclaringType != null && Generated(t.DeclaringType));
bool UnityObject(TypeDefinition type) {
    var visited = new HashSet<string>();
    TypeReference? parent = type.BaseType;
    while (parent != null && visited.Add(parent.FullName)) {
        if (parent.FullName == "UnityEngine.Object") return true;
        try { parent = parent.Resolve()?.BaseType; }
        catch (AssemblyResolutionException) { return false; }
    }
    return false;
}
bool Serialized(FieldDefinition f) => !f.IsStatic && !f.IsInitOnly && !f.IsNotSerialized
    && (f.IsPublic || f.CustomAttributes.Any(a => a.AttributeType.FullName is "UnityEngine.SerializeField" or "UnityEngine.SerializeReference"));
var missingTypes = new List<string>();
var baseChanges = new List<object>();
var missingFields = new List<object>();
var enumChanges = new List<object>();
var interopChanges = new List<object>();
int unityTypes = 0, serializedFields = 0, enumFields = 0, interopMethods = 0;
foreach (var old in previous.Where(t => !Generated(t) && t.Name != "<Module>")) {
    if (!current.TryGetValue(old.FullName, out var next)) {
        missingTypes.Add(old.FullName);
        continue;
    }
    if (old.BaseType?.FullName != next.BaseType?.FullName)
        baseChanges.Add(new { type = old.FullName, before = old.BaseType?.FullName, after = next.BaseType?.FullName });
    if (UnityObject(old)) {
        unityTypes++;
        foreach (var field in old.Fields.Where(Serialized)) {
            serializedFields++;
            var replacement = next.Fields.SingleOrDefault(f => f.Name == field.Name);
            if (replacement == null || !Serialized(replacement) || replacement.FieldType.FullName != field.FieldType.FullName)
                missingFields.Add(new { type = old.FullName, field = field.Name, expected = field.FieldType.FullName, actual = replacement?.FieldType.FullName });
        }
    }
    if (old.IsEnum)
        foreach (var field in old.Fields.Where(f => f.HasConstant)) {
            enumFields++;
            var replacement = next.Fields.SingleOrDefault(f => f.Name == field.Name);
            if (replacement == null || !Equals(field.Constant, replacement.Constant))
                enumChanges.Add(new { type = old.FullName, field = field.Name, before = field.Constant, after = replacement?.Constant });
        }
    foreach (var method in old.Methods.Where(m => m.HasPInvokeInfo)) {
        interopMethods++;
        var replacement = next.Methods.SingleOrDefault(m => m.FullName == method.FullName);
        if (replacement == null || !replacement.HasPInvokeInfo
            || replacement.PInvokeInfo.Module.Name != method.PInvokeInfo.Module.Name
            || replacement.PInvokeInfo.EntryPoint != method.PInvokeInfo.EntryPoint
            || replacement.PInvokeInfo.Attributes != method.PInvokeInfo.Attributes)
            interopChanges.Add(new { method = method.FullName, module = method.PInvokeInfo.Module.Name });
    }
}
var documents = new Dictionary<string, object>();
var sourceDocumentErrors = new List<string>();
var pdbPath = Path.Combine(managed, "Assembly-CSharp.pdb");
if (File.Exists(pdbPath)) {
    using var symbols = AssemblyDefinition.ReadAssembly(Path.Combine(managed, "Assembly-CSharp.dll"),
        new ReaderParameters { AssemblyResolver = resolver, ReadSymbols = true, SymbolReaderProvider = new PortablePdbReaderProvider() });
    foreach (var document in symbols.MainModule.Types.SelectMany(Flatten).SelectMany(t => t.Methods)
        .SelectMany(m => m.DebugInformation.SequencePoints).Select(p => p.Document).DistinctBy(d => d.Url)) {
        var url = document.Url.Replace('\\', '/');
        var offset = url.IndexOf("/Assets/", StringComparison.Ordinal);
        if (offset < 0 && url.StartsWith("Assets/")) offset = -1;
        if (offset < -1 || (offset == -1 && !url.StartsWith("Assets/"))) continue;
        var relative = url[(offset + 1)..];
        var path = Path.Combine(originalRoot, relative);
        if (!File.Exists(path)) { sourceDocumentErrors.Add("Missing original source: " + relative); continue; }
        var bytes = File.ReadAllBytes(path);
        byte[] digest = document.HashAlgorithm switch {
            DocumentHashAlgorithm.SHA1 => SHA1.HashData(bytes),
            DocumentHashAlgorithm.SHA256 => SHA256.HashData(bytes),
            _ => Array.Empty<byte>()
        };
        documents[relative] = new { matches = digest.SequenceEqual(document.Hash), algorithm = document.HashAlgorithm.ToString(),
            publisher = Convert.ToHexString(document.Hash).ToLowerInvariant(), actual = Convert.ToHexString(digest).ToLowerInvariant() };
        if (!digest.SequenceEqual(document.Hash)) sourceDocumentErrors.Add("Publisher PDB checksum mismatch: " + relative);
    }
}
if (documents.Count != 2279) sourceDocumentErrors.Add("Expected all 2279 pinned publisher source documents; found " + documents.Count);
var forbidden = rebuilt.MainModule.AssemblyReferences.Where(r => r.Name is "System.Private.CoreLib" or "System.Runtime").Select(r => r.FullName).ToArray();
var unresolvedTypes = new List<string>();
var unresolvedMembers = new List<string>();
foreach (var reference in rebuilt.MainModule.GetTypeReferences()) {
    if (reference.Scope.Name == "Assembly-CSharp") continue;
    try { if (reference.Resolve() == null) unresolvedTypes.Add(reference.FullName); }
    catch (Exception) { unresolvedTypes.Add(reference.FullName); }
}
foreach (var reference in rebuilt.MainModule.GetMemberReferences()) {
    if (reference.DeclaringType.Scope.Name == "Assembly-CSharp") continue;
    try {
        var found = reference switch { MethodReference method => method.Resolve() as MemberReference,
            FieldReference field => field.Resolve() as MemberReference, _ => reference };
        if (found == null) unresolvedMembers.Add(reference.FullName);
    } catch (Exception) { unresolvedMembers.Add(reference.FullName); }
}
var ok = rebuilt.Name.Name == "Assembly-CSharp" && missingTypes.Count == 0 && baseChanges.Count == 0
    && missingFields.Count == 0 && enumChanges.Count == 0 && interopChanges.Count == 0 && forbidden.Length == 0
    && unresolvedTypes.Count == 0 && unresolvedMembers.Count == 0 && sourceDocumentErrors.Count == 0;
var report = new {
    ok, assembly_name = rebuilt.Name.Name,
    compiled_sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(compiled))).ToLowerInvariant(),
    publisher_types = previous.Length, compiled_types = current.Count, unity_types_checked = unityTypes,
    serialized_fields_checked = serializedFields, enum_values_checked = enumFields, interop_methods_checked = interopMethods,
    missing_types = missingTypes, base_type_changes = baseChanges, incompatible_serialized_fields = missingFields,
    changed_enum_values = enumChanges, changed_native_imports = interopChanges, forbidden_host_references = forbidden,
    unresolved_runtime_types = unresolvedTypes, unresolved_runtime_members = unresolvedMembers,
    publisher_pdb_source_documents = documents,
    source_document_errors = sourceDocumentErrors,
    limits = "Checks assembly identity, retained types, Unity serialized field signatures, enums and native imports. Does not prove rendering or every card ruling. Compiler-generated state machines may differ."
};
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[3]))!);
File.WriteAllText(args[3], JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }) + "\n");
Console.WriteLine(JsonSerializer.Serialize(new { ok, unityTypes, serializedFields, missing = missingTypes.Count, fields = missingFields.Count, source_documents = documents.Count }));
Environment.ExitCode = ok ? 0 : 1;
