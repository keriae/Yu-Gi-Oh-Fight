using System.Security.Cryptography;
using System.Text.Json;
using Mono.Cecil;

if (args.Length != 5)
    throw new ArgumentException("Usage: SourceValidation <runtime Managed> <compiled Assembly-CSharp.dll> <serialized-bindings.json> <runtime-initialize.json> <report.json>");
var managed = Path.GetFullPath(args[0]);
var compiled = Path.GetFullPath(args[1]);
using var resolver = new DefaultAssemblyResolver();
resolver.AddSearchDirectory(Path.GetDirectoryName(compiled)!);
resolver.AddSearchDirectory(managed);
using var rebuilt = AssemblyDefinition.ReadAssembly(compiled, new ReaderParameters { AssemblyResolver = resolver, InMemory = true });
using var baselineResolver = new DefaultAssemblyResolver();
baselineResolver.AddSearchDirectory(managed);
using var baseline = AssemblyDefinition.ReadAssembly(Path.Combine(managed, "Assembly-CSharp.dll"),
    new ReaderParameters { AssemblyResolver = baselineResolver, InMemory = true });
using var bindingsDocument = JsonDocument.Parse(File.ReadAllBytes(args[2]));
if (bindingsDocument.RootElement.GetProperty("version").GetInt32() != 1)
    throw new InvalidDataException("Unsupported serialized bindings table");
var bindings = bindingsDocument.RootElement.GetProperty("bindings").EnumerateObject()
    .ToDictionary(item => item.Name, item => item.Value.GetString()!, StringComparer.Ordinal);
IEnumerable<TypeDefinition> Flatten(TypeDefinition type)
{
    yield return type;
    foreach (var nested in type.NestedTypes.SelectMany(Flatten)) yield return nested;
}
var current = rebuilt.MainModule.Types.SelectMany(Flatten).ToDictionary(type => type.FullName, StringComparer.Ordinal);
var errors = new SortedSet<string>(StringComparer.Ordinal);
var checkedTypes = new HashSet<string>(StringComparer.Ordinal);
var checkedMembers = new HashSet<string>(StringComparer.Ordinal);
var bindingTargets = new List<object>();
var runtimeInitializers = new List<object>();
var nativeImports = new List<object>();

string Identity(TypeReference type) => (type.Scope is AssemblyNameReference assembly ? assembly.Name
    : type.Module.Assembly.Name.Name) + ":" + type.FullName.Replace('/', '+');
bool Retired(TypeReference type)
{
    var identity = Identity(type);
    return bindings.TryGetValue(identity, out var target) && target != identity
        || type.FullName.StartsWith("MDPro3.UI.", StringComparison.Ordinal)
        || type.FullName.StartsWith("MDPro3.Servant.", StringComparison.Ordinal);
}
void AuditType(TypeReference? type, string context)
{
    if (type == null || type is GenericParameter) return;
    if (Retired(type)) errors.Add(context + " references retired UI type " + type.FullName);
    if (type is GenericInstanceType generic)
        foreach (var argument in generic.GenericArguments) AuditType(argument, context);
    if (type is FunctionPointerType pointer)
    {
        AuditType(pointer.ReturnType, context);
        foreach (var parameter in pointer.Parameters) AuditType(parameter.ParameterType, context);
        return;
    }
    if (type is OptionalModifierType optional) AuditType(optional.ModifierType, context);
    if (type is RequiredModifierType required) AuditType(required.ModifierType, context);
    if (type is TypeSpecification specification)
    {
        AuditType(specification.ElementType, context);
        return;
    }
    if (!checkedTypes.Add(Identity(type))) return;
    try
    {
        if (type.Resolve() == null) errors.Add("Unresolved runtime type: " + type.FullName);
    }
    catch (Exception error) when (error is AssemblyResolutionException or ResolutionException)
    {
        errors.Add("Unresolved runtime type: " + type.FullName);
    }
}
void AuditMember(MemberReference member, string context)
{
    if (member is TypeReference type) { AuditType(type, context); return; }
    AuditType(member.DeclaringType, context);
    if (member is FieldReference field) AuditType(field.FieldType, context);
    if (member is MethodReference method)
    {
        AuditType(method.ReturnType, context);
        foreach (var parameter in method.Parameters) AuditType(parameter.ParameterType, context);
        if (method is GenericInstanceMethod generic)
            foreach (var argument in generic.GenericArguments) AuditType(argument, context);
        // Multidimensional array constructors/accessors are implemented by the CLR.
        if (method.DeclaringType is ArrayType && method.Name is ".ctor" or "Get" or "Set" or "Address") return;
    }
    if (!checkedMembers.Add(member.FullName)) return;
    try
    {
        var resolved = member switch
        {
            MethodReference methodReference => methodReference.Resolve() as MemberReference,
            FieldReference fieldReference => fieldReference.Resolve(),
            _ => member
        };
        if (resolved == null) errors.Add("Unresolved runtime member: " + member.FullName);
    }
    catch (Exception error) when (error is AssemblyResolutionException or ResolutionException)
    {
        errors.Add("Unresolved runtime member: " + member.FullName);
    }
}
void AuditArgument(CustomAttributeArgument argument, string context)
{
    AuditType(argument.Type, context);
    if (argument.Value is TypeReference type) AuditType(type, context);
    else if (argument.Value is CustomAttributeArgument nested) AuditArgument(nested, context);
    else if (argument.Value is CustomAttributeArgument[] values)
        foreach (var value in values) AuditArgument(value, context);
}
void AuditAttributes(ICustomAttributeProvider provider, string context)
{
    foreach (var attribute in provider.CustomAttributes)
    {
        AuditMember(attribute.Constructor, context);
        foreach (var argument in attribute.ConstructorArguments) AuditArgument(argument, context);
        foreach (var field in attribute.Fields) AuditArgument(field.Argument, context);
        foreach (var property in attribute.Properties) AuditArgument(property.Argument, context);
    }
}
void AuditGenerics(IGenericParameterProvider provider, string context)
{
    foreach (var parameter in provider.GenericParameters)
    {
        AuditAttributes(parameter, context);
        foreach (var constraint in parameter.Constraints)
        {
            AuditType(constraint.ConstraintType, context);
            AuditAttributes(constraint, context);
        }
    }
}
foreach (var type in current.Values)
{
    if (Retired(type)) errors.Add("Retired UI definition remains: " + type.FullName);
    AuditType(type.BaseType, type.FullName);
    AuditAttributes(type, type.FullName);
    AuditGenerics(type, type.FullName);
    foreach (var implemented in type.Interfaces)
    {
        AuditType(implemented.InterfaceType, type.FullName);
        AuditAttributes(implemented, type.FullName);
    }
    foreach (var field in type.Fields)
    {
        AuditType(field.FieldType, field.FullName);
        AuditAttributes(field, field.FullName);
    }
    foreach (var property in type.Properties)
    {
        AuditType(property.PropertyType, property.FullName);
        AuditAttributes(property, property.FullName);
        foreach (var parameter in property.Parameters) AuditType(parameter.ParameterType, property.FullName);
    }
    foreach (var @event in type.Events)
    {
        AuditType(@event.EventType, @event.FullName);
        AuditAttributes(@event, @event.FullName);
    }
    foreach (var method in type.Methods)
    {
        AuditMember(method, method.FullName);
        AuditAttributes(method, method.FullName);
        AuditAttributes(method.MethodReturnType, method.FullName);
        AuditGenerics(method, method.FullName);
        foreach (var parameter in method.Parameters) AuditAttributes(parameter, method.FullName);
        foreach (var @override in method.Overrides) AuditMember(@override, method.FullName);
        if (!method.HasBody) continue;
        foreach (var local in method.Body.Variables) AuditType(local.VariableType, method.FullName);
        foreach (var handler in method.Body.ExceptionHandlers) AuditType(handler.CatchType, method.FullName);
        foreach (var instruction in method.Body.Instructions)
        {
            if (instruction.Operand is MemberReference member) AuditMember(member, method.FullName);
            else if (instruction.Operand is CallSite site)
            {
                AuditType(site.ReturnType, method.FullName);
                foreach (var parameter in site.Parameters) AuditType(parameter.ParameterType, method.FullName);
            }
        }
    }
}
AuditAttributes(rebuilt, rebuilt.Name.Name);
AuditAttributes(rebuilt.MainModule, rebuilt.Name.Name);
foreach (var reference in rebuilt.MainModule.AssemblyReferences)
    if (reference.Name is "System.Private.CoreLib" or "System.Runtime")
        errors.Add("Unsupported host framework reference: " + reference.FullName);
foreach (var reference in rebuilt.MainModule.GetTypeReferences()) AuditType(reference, reference.FullName);
foreach (var reference in rebuilt.MainModule.GetMemberReferences()) AuditMember(reference, reference.FullName);

foreach (var identity in bindings.Values.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
{
    var separator = identity.IndexOf(':');
    if (separator <= 0 || separator == identity.Length - 1) throw new InvalidDataException("Invalid script identity: " + identity);
    var assemblyName = identity[..separator];
    var typeName = identity[(separator + 1)..].Replace('+', '/');
    TypeDefinition? definition;
    if (assemblyName == rebuilt.Name.Name) current.TryGetValue(typeName, out definition);
    else
    {
        using var assembly = AssemblyDefinition.ReadAssembly(Path.Combine(managed, assemblyName + ".dll"),
            new ReaderParameters { AssemblyResolver = resolver });
        definition = assembly.MainModule.Types.SelectMany(Flatten).SingleOrDefault(type => type.FullName == typeName);
    }
    if (definition == null) errors.Add("Serialized binding target is missing: " + identity);
    bindingTargets.Add(new { identity, resolved = definition != null });
}

using var initialize = JsonDocument.Parse(File.ReadAllBytes(args[3]));
foreach (var entry in initialize.RootElement.GetProperty("root").EnumerateArray())
{
    if (entry.GetProperty("assemblyName").GetString() != rebuilt.Name.Name) continue;
    var space = entry.GetProperty("nameSpace").GetString()!;
    var name = entry.GetProperty("className").GetString()!;
    var typeName = (space.Length == 0 ? "" : space + ".") + name;
    var methodName = entry.GetProperty("methodName").GetString()!;
    var loadType = entry.GetProperty("loadTypes").GetInt32();
    var method = current.GetValueOrDefault(typeName)?.Methods.SingleOrDefault(candidate => candidate.Name == methodName
        && candidate.IsStatic && candidate.Parameters.Count == 0);
    var attribute = method?.CustomAttributes.SingleOrDefault(candidate => candidate.AttributeType.FullName == "UnityEngine.RuntimeInitializeOnLoadMethodAttribute");
    var declaredLoadType = attribute == null ? -1 : attribute.ConstructorArguments.Count == 0 ? 0
        : Convert.ToInt32(attribute.ConstructorArguments[0].Value);
    if (method == null || attribute == null || loadType != declaredLoadType)
        errors.Add("Runtime initializer does not match its static source method: " + typeName + "::" + methodName);
    runtimeInitializers.Add(new { type = typeName, method = methodName, load_type = loadType, resolved = method != null });
}

// The native libraries are fixed build inputs. Check the retained imports against
// their native contracts, while allowing managed types and unused wrappers to go.
string NativeType(TypeReference type) => type switch
{
    ByReferenceType byReference => "ref:" + NativeType(byReference.ElementType),
    PointerType pointer => "pointer:" + NativeType(pointer.ElementType),
    ArrayType array => "array:" + array.Rank + ":" + NativeType(array.ElementType),
    _ => NativeDefinition(type)
};
string NativeDefinition(TypeReference type)
{
    if (type.Namespace == "System") return type.FullName;
    var definition = type.Resolve();
    if (definition?.IsEnum == true)
        return "enum:" + NativeType(definition.Fields.Single(field => field.Name == "value__").FieldType);
    if (definition?.IsValueType == true)
        return "struct:" + definition.PackingSize + ":" + definition.ClassSize + ":" + (int)(definition.Attributes & TypeAttributes.LayoutMask)
            + ":" + string.Join(",", definition.Fields.Where(field => !field.IsStatic)
                .Select(field => field.Name + ":" + field.Offset + ":" + NativeType(field.FieldType)));
    if (definition?.BaseType?.FullName == "System.MulticastDelegate")
    {
        var invoke = definition.Methods.Single(method => method.Name == "Invoke");
        return "delegate:" + NativeType(invoke.ReturnType) + ":" + string.Join(",", invoke.Parameters.Select(parameter => NativeType(parameter.ParameterType)));
    }
    return type.Name;
}
string NativeMethod(MethodDefinition method) => method.Name + ":" + method.PInvokeInfo.Module.Name + ":"
    + method.PInvokeInfo.EntryPoint + ":" + (int)method.PInvokeInfo.Attributes + ":" + NativeType(method.ReturnType)
    + ":" + string.Join(",", method.Parameters.Select(parameter => NativeType(parameter.ParameterType)));
var originalImports = baseline.MainModule.Types.SelectMany(Flatten).SelectMany(type => type.Methods)
    .Where(method => method.HasPInvokeInfo).Select(NativeMethod).ToHashSet(StringComparer.Ordinal);
foreach (var method in current.Values.SelectMany(type => type.Methods).Where(method => method.HasPInvokeInfo))
{
    bool fixedNativeApi = method.PInvokeInfo.Module.Name is "ocgcore" or "ygoserver";
    bool matches = originalImports.Contains(NativeMethod(method));
    if (fixedNativeApi && !matches) errors.Add("Fixed native API import changed: " + method.FullName);
    if (!method.IsStatic || string.IsNullOrEmpty(method.PInvokeInfo.EntryPoint) || string.IsNullOrEmpty(method.PInvokeInfo.Module.Name))
        errors.Add("Invalid native import: " + method.FullName);
    nativeImports.Add(new { method = method.FullName, library = method.PInvokeInfo.Module.Name,
        entry_point = method.PInvokeInfo.EntryPoint, fixed_native_api = fixedNativeApi, contract_verified = matches });
}
if (rebuilt.Name.Name != "Assembly-CSharp") errors.Add("Unexpected assembly name: " + rebuilt.Name.Name);
var report = new
{
    ok = errors.Count == 0,
    assembly_name = rebuilt.Name.Name,
    compiled_sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(compiled))).ToLowerInvariant(),
    compiled_types = current.Count, runtime_types_checked = checkedTypes.Count, runtime_members_checked = checkedMembers.Count,
    serialized_binding_targets = bindingTargets, runtime_initializers = runtimeInitializers, native_imports = nativeImports,
    errors,
    limits = "Validates current runtime references, project component identities, startup callbacks and retained native imports. UI and duel behavior require runtime acceptance."
};
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[4]))!);
File.WriteAllText(args[4], JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }) + "\n");
Console.WriteLine(JsonSerializer.Serialize(new { report.ok, report.compiled_types, report.runtime_types_checked, report.runtime_members_checked,
    serialized_binding_targets = bindingTargets.Count, runtime_initializers = runtimeInitializers.Count, native_imports = nativeImports.Count, errors = errors.Count }));
foreach (var error in errors) Console.Error.WriteLine(error);
Environment.ExitCode = report.ok ? 0 : 1;
