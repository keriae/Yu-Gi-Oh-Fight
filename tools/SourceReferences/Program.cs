using Mono.Cecil; using Mono.Cecil.Cil; using System.Text.Json;
var input=args[0];var net48=args[1];var output=args[2];Directory.CreateDirectory(output);
var changes=new List<object>();
foreach(var path in Directory.GetFiles(input,"*.dll")){File.Copy(path,Path.Combine(output,Path.GetFileName(path)),true);}
var resolver=new DefaultAssemblyResolver();resolver.AddSearchDirectory(input);resolver.AddSearchDirectory(net48);
var m=ModuleDefinition.ReadModule(Path.Combine(input,"mscorlib.dll"),new ReaderParameters{AssemblyResolver=resolver});
var full=ModuleDefinition.ReadModule(Path.Combine(net48,"mscorlib.dll"));
foreach(var t in m.Types){var rt=full.Types.FirstOrDefault(x=>x.FullName==t.FullName);if(rt==null)continue;
foreach(var f in rt.Fields.Where(f=>f.IsLiteral&&f.HasConstant))if(!t.Fields.Any(x=>x.Name==f.Name)){
var nf=new FieldDefinition(f.Name,f.Attributes,m.ImportReference(f.FieldType)){Constant=f.Constant};t.Fields.Add(nf);changes.Add(new {assembly="mscorlib",type=t.FullName,field=f.Name,value=f.Constant,source="Microsoft.NETFramework.ReferenceAssemblies.net48 1.0.3"});}}
foreach(var name in new[]{"System.Runtime.InteropServices.StructLayoutAttribute","System.Runtime.CompilerServices.SpecialNameAttribute"}){
if(m.Types.Any(t=>t.FullName==name))continue;var rt=full.Types.First(t=>t.FullName==name);var t=new TypeDefinition(rt.Namespace,rt.Name,rt.Attributes,m.ImportReference(m.Types.First(x=>x.FullName=="System.Attribute")));m.Types.Add(t);
foreach(var rm in rt.Methods.Where(x=>x.IsConstructor)){
var method=new MethodDefinition(rm.Name,rm.Attributes,m.TypeSystem.Void);foreach(var param in rm.Parameters)method.Parameters.Add(new ParameterDefinition(param.Name,param.Attributes,m.ImportReference(param.ParameterType)));t.Methods.Add(method);method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));}
foreach(var rf in rt.Fields.Where(x=>x.IsPublic)){var field=new FieldDefinition(rf.Name,rf.Attributes,m.ImportReference(rf.FieldType));if(rf.HasConstant)field.Constant=rf.Constant;t.Fields.Add(field);}
changes.Add(new {assembly="mscorlib",type=name,source="Microsoft.NETFramework.ReferenceAssemblies.net48 1.0.3",purpose="compiler-recognized pseudo custom attribute; no runtime instantiation"});}

// Unity stripping removes members referenced by existing AttributeUsage blobs; Roslyn needs the signature to decode those blobs.
var usage=m.Types.First(t=>t.FullName=="System.AttributeUsageAttribute");
foreach(var spec in new[]{("AllowMultiple",m.TypeSystem.Boolean),("Inherited",m.TypeSystem.Boolean),("ValidOn",m.ImportReference(m.Types.First(t=>t.FullName=="System.AttributeTargets")))}){
if(usage.Properties.Any(p=>p.Name==spec.Item1))continue;
var prop=new PropertyDefinition(spec.Item1,PropertyAttributes.None,spec.Item2);
var get=new MethodDefinition("get_"+spec.Item1,MethodAttributes.Public|MethodAttributes.HideBySig|MethodAttributes.SpecialName,spec.Item2);get.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));get.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));usage.Methods.Add(get);prop.GetMethod=get;
if(spec.Item1!="ValidOn"){var set=new MethodDefinition("set_"+spec.Item1,MethodAttributes.Public|MethodAttributes.HideBySig|MethodAttributes.SpecialName,m.TypeSystem.Void);set.Parameters.Add(new ParameterDefinition("value",ParameterAttributes.None,spec.Item2));set.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));usage.Methods.Add(set);prop.SetMethod=set;}
usage.Properties.Add(prop);changes.Add(new {assembly="mscorlib",type=usage.FullName,property=spec.Item1,source="Microsoft.NETFramework.ReferenceAssemblies.net48 1.0.3",purpose="decode preexisting AttributeUsage metadata during compilation"});}




var ns=ModuleDefinition.ReadModule(args[3]);
TypeReference CloneType(TypeReference type,TypeDefinition owner,Dictionary<GenericParameter,GenericParameter>? gps=null){
if(type is GenericParameter gp){if(gps!=null&&gps.TryGetValue(gp,out var mapped))return mapped;return owner.GenericParameters[gp.Position];}
if(type is ByReferenceType br)return new ByReferenceType(CloneType(br.ElementType,owner,gps));
if(type is ArrayType arr)return new ArrayType(CloneType(arr.ElementType,owner,gps),arr.Rank);
if(type is GenericInstanceType gi){var next=new GenericInstanceType(CloneType(gi.ElementType,owner,gps));foreach(var arg in gi.GenericArguments)next.GenericArguments.Add(CloneType(arg,owner,gps));return next;}
return m.Types.FirstOrDefault(t=>t.FullName==type.FullName)??m.ImportReference(type);}
MethodDefinition CloneMethod(MethodDefinition method,TypeDefinition owner){
var next=new MethodDefinition(method.Name,method.Attributes,m.TypeSystem.Void);var gps=new Dictionary<GenericParameter,GenericParameter>();
foreach(var gp in method.GenericParameters){var ng=new GenericParameter(gp.Name,next){Attributes=gp.Attributes};next.GenericParameters.Add(ng);gps[gp]=ng;}
next.ReturnType=CloneType(method.ReturnType,owner,gps);foreach(var param in method.Parameters)next.Parameters.Add(new ParameterDefinition(param.Name,param.Attributes,CloneType(param.ParameterType,owner,gps)));
foreach(var gp in method.GenericParameters)foreach(var constraint in gp.Constraints)gps[gp].Constraints.Add(new GenericParameterConstraint(CloneType(constraint.ConstraintType,owner,gps)));
next.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));owner.Methods.Add(next);return next;}
foreach(var name in new[]{"System.Index","System.Range"}){var rt=ns.Types.First(t=>t.FullName==name);var t=new TypeDefinition(rt.Namespace,rt.Name,rt.Attributes,m.Types.First(t=>t.FullName=="System.ValueType"));m.Types.Add(t);}
foreach(var name in new[]{"System.Index","System.Range"}){var rt=ns.Types.First(t=>t.FullName==name);var t=m.Types.First(t=>t.FullName==name);var map=rt.Methods.ToDictionary(x=>x,x=>CloneMethod(x,t));foreach(var rp in rt.Properties){var prop=new PropertyDefinition(rp.Name,rp.Attributes,CloneType(rp.PropertyType,t));if(rp.GetMethod!=null)prop.GetMethod=map[rp.GetMethod];if(rp.SetMethod!=null)prop.SetMethod=map[rp.SetMethod];t.Properties.Add(prop);}changes.Add(new{assembly="mscorlib",type=name,source="NETStandard.Library.Ref 2.1.0",purpose="C# compiler intrinsics for slice lowering; generated IL must not retain these references"});}
foreach(var name in new[]{"System.Runtime.CompilerServices.AsyncTaskMethodBuilder","System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1","System.Runtime.CompilerServices.AsyncVoidMethodBuilder"}){var t=m.Types.First(t=>t.FullName==name);if(t.Methods.Any(x=>x.Name=="AwaitOnCompleted"))continue;CloneMethod(full.Types.First(t=>t.FullName==name).Methods.First(x=>x.Name=="AwaitOnCompleted"),t);changes.Add(new{assembly="mscorlib",type=name,method="AwaitOnCompleted",source="Microsoft.NETFramework.ReferenceAssemblies.net48 1.0.3",purpose="compiler-required method; actual awaits use shipped AwaitUnsafeOnCompleted"});}
void FixScope(TypeReference type){if(type is TypeSpecification spec){FixScope(spec.ElementType);if(type is GenericInstanceType generic)foreach(var arg in generic.GenericArguments)FixScope(arg);return;}if(type.Scope is AssemblyNameReference a&&(a.Name=="mscorlib"||a.Name=="netstandard"))type.Scope=m;}
foreach(var t in m.Types.SelectMany(Walk)){if(t.BaseType!=null)FixScope(t.BaseType);foreach(var f in t.Fields)FixScope(f.FieldType);foreach(var p in t.Properties)FixScope(p.PropertyType);foreach(var method in t.Methods){FixScope(method.ReturnType);foreach(var param in method.Parameters)FixScope(param.ParameterType);}}
foreach(var a in m.AssemblyReferences.Where(a=>a.Name=="mscorlib"||a.Name=="netstandard").ToArray())m.AssemblyReferences.Remove(a);
m.Write(Path.Combine(output,"mscorlib.dll"));
var u=ModuleDefinition.ReadModule(Path.Combine(input,"UnityEngine.CoreModule.dll"),new ReaderParameters{AssemblyResolver=resolver});
var math=u.Types.First(x=>x.FullName=="UnityEngine.Mathf");
foreach(var val in new[]{("Deg2Rad",0.0174532924f),("Rad2Deg",57.29578f)})if(!math.Fields.Any(x=>x.Name==val.Item1)){math.Fields.Add(new FieldDefinition(val.Item1,FieldAttributes.Public|FieldAttributes.Static|FieldAttributes.Literal|FieldAttributes.HasDefault,u.TypeSystem.Single){Constant=val.Item2});changes.Add(new{assembly="UnityEngine.CoreModule",type=math.FullName,field=val.Item1,value=val.Item2,source="UnityCsReference Runtime/Export/Math/Mathf.cs"});}
if(!u.Types.Any(x=>x.FullName=="UnityEngine.SleepTimeout")){var sleep=new TypeDefinition("UnityEngine","SleepTimeout",TypeAttributes.Public|TypeAttributes.Class|TypeAttributes.Sealed|TypeAttributes.BeforeFieldInit,u.TypeSystem.Object);u.Types.Add(sleep);foreach(var val in new[]{("NeverSleep",-1),("SystemSetting",-2)}){sleep.Fields.Add(new FieldDefinition(val.Item1,FieldAttributes.Public|FieldAttributes.Static|FieldAttributes.Literal|FieldAttributes.HasDefault,u.TypeSystem.Int32){Constant=val.Item2});changes.Add(new{assembly="UnityEngine.CoreModule",type=sleep.FullName,field=val.Item1,value=val.Item2,source="UnityCsReference Runtime/Export/Device/Screen.cs"});}}
u.Write(Path.Combine(output,"UnityEngine.CoreModule.dll"));

using(var system=ModuleDefinition.ReadModule(Path.Combine(input,"System.dll"),new ReaderParameters{AssemblyResolver=resolver})){var ep=system.Types.First(t=>t.FullName=="System.Net.IPEndPoint");foreach(var val in new[]{("MinPort",0),("MaxPort",65535)})if(!ep.Fields.Any(f=>f.Name==val.Item1)){ep.Fields.Add(new FieldDefinition(val.Item1,FieldAttributes.Public|FieldAttributes.Static|FieldAttributes.Literal|FieldAttributes.HasDefault,system.TypeSystem.Int32){Constant=val.Item2});changes.Add(new{assembly="System",type=ep.FullName,field=val.Item1,value=val.Item2,source="Microsoft.NETFramework.ReferenceAssemblies.net48 1.0.3"});}system.Write(Path.Combine(output,"System.dll"));}
using(var collections=ModuleDefinition.ReadModule(Path.Combine(input,"Unity.Collections.dll"),new ReaderParameters{AssemblyResolver=resolver})){var a=collections.Types.First(t=>t.FullName=="Unity.Jobs.DOTSCompilerGeneratedAttribute");a.IsPublic=true;foreach(var ctor in a.Methods.Where(m=>m.IsConstructor))ctor.IsPublic=true;collections.Write(Path.Combine(output,"Unity.Collections.dll"));changes.Add(new{assembly="Unity.Collections",type=a.FullName,change="expose internal marker only in compile reference",purpose="recreate Unity-injected generated metadata using C#; runtime attribute retains original visibility"});}
File.WriteAllText(Path.Combine(output,"reference-prep.json"),JsonSerializer.Serialize(changes,new JsonSerializerOptions{WriteIndented=true,NumberHandling=System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals}));Console.WriteLine("reference supplements "+changes.Count);

IEnumerable<TypeDefinition> Walk(TypeDefinition t){yield return t;foreach(var n in t.NestedTypes)foreach(var c in Walk(n))yield return c;}
