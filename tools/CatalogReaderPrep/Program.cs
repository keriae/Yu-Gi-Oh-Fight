using System.Security.Cryptography;
using Mono.Cecil;
using Mono.Cecil.Cil;

if (args.Length != 2)
    throw new ArgumentException("Usage: CatalogReaderPrep <publisher Addressables dll> <diagnostic output dll>");

{
    var diagnosticInput = Path.GetFullPath(args[0]);
    var diagnosticOutput = Path.GetFullPath(args[1]);
    if (diagnosticInput == diagnosticOutput
        || diagnosticOutput.Split(Path.DirectorySeparatorChar).Contains("vendor"))
        throw new ArgumentException("Catalog diagnostics must be written outside the publisher vendor tree.");
    var addressablesHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(diagnosticInput))).ToLowerInvariant();
    if (addressablesHash != "7b5bc0c29df0e84df9b39c86b08d3303773111c0bdbce02877047025dfa23470")
        throw new InvalidDataException("Unsupported publisher Addressables SHA-256.");
    // The raw catalog diagnostic retains IDs exactly as stored, avoiding the
    // player's runtime path resolver and scene singleton initialization.
    using var catalogResolver = new DefaultAssemblyResolver();
    catalogResolver.AddSearchDirectory(Path.GetDirectoryName(Path.GetFullPath(args[0]))!);
    using var catalogAssembly = AssemblyDefinition.ReadAssembly(args[0], new ReaderParameters { AssemblyResolver = catalogResolver });
    var addressablesType = catalogAssembly.MainModule.Types.Single(t => t.FullName == "UnityEngine.AddressableAssets.Addressables");
    var resolveMethod = addressablesType.Methods.Single(m => m.Name == "ResolveInternalId" && m.IsStatic
        && m.ReturnType.FullName == "System.String" && m.Parameters.Count == 1);
    resolveMethod.Body.Instructions.Clear();
    resolveMethod.Body.ExceptionHandlers.Clear();
    resolveMethod.Body.Variables.Clear();
    resolveMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
    resolveMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
    // Resolving a raw ID must not initialize ResourceManager/profiler scene
    // singletons. No Addressables runtime fields are consumed by this reader.
    var catalogStaticConstructor = addressablesType.Methods.Single(m => m.Name == ".cctor");
    catalogStaticConstructor.Body.Instructions.Clear();
    catalogStaticConstructor.Body.ExceptionHandlers.Clear();
    catalogStaticConstructor.Body.Variables.Clear();
    catalogStaticConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
    // The graph projection does not read bundle request options (hash/CRC/cache
    // policy). Their native Hash128 formatter is unnecessary for IDs and edges.
    var contentCatalog = catalogAssembly.MainModule.Types.Single(t => t.FullName == "UnityEngine.AddressableAssets.ResourceLocators.ContentCatalogData");
    var requestOptionsAdapter = contentCatalog.NestedTypes.Single(t => t.Name == "AssetBundleRequestOptionsSerializationAdapter");
    var requestOptionsRead = requestOptionsAdapter.Methods.Single(m => m.Name == "Deserialize");
    if (requestOptionsRead.ReturnType.FullName != "System.Object") throw new InvalidDataException("Unexpected catalog options adapter signature.");
    requestOptionsRead.Body.Instructions.Clear();
    requestOptionsRead.Body.ExceptionHandlers.Clear();
    requestOptionsRead.Body.Variables.Clear();
    requestOptionsRead.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
    requestOptionsRead.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
    Directory.CreateDirectory(Path.GetDirectoryName(diagnosticOutput)!);
    catalogAssembly.Write(diagnosticOutput);
    Console.WriteLine("Prepared diagnostic-only raw catalog reader: " + args[1]);
    return;
}
