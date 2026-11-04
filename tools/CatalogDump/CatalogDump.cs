using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;

public static class CatalogDump
{
    public static int Main(string[] args)
    {
        if (args.Length != 2) throw new ArgumentException("CatalogDump <catalog.bin> <output.json>");
        var load = typeof(ContentCatalogData).GetMethod("LoadFromFile", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        var catalog = (ContentCatalogData)load.Invoke(null, new object[] { args[0], 0 });
        var create = typeof(ContentCatalogData).GetMethod("CreateCustomLocator", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var locator = (IResourceLocator)create.Invoke(catalog, new object[] { "shipping-catalog", null, 8192 });
        var locations = new Dictionary<string, object>();
        var keys = new Dictionary<string, List<string>>();
        Action<IResourceLocation> visit = null;
        visit = delegate(IResourceLocation location)
        {
            var id = location.ProviderId + "|" + location.InternalId;
            if (locations.ContainsKey(id)) return;
            var deps = new List<string>();
            locations[id] = new { primary_key = location.PrimaryKey, internal_id = location.InternalId,
                provider = location.ProviderId, resource_type = location.ResourceType.FullName, dependencies = deps };
            if (location.Dependencies == null) return;
            foreach (var dependency in location.Dependencies)
            {
                deps.Add(dependency.ProviderId + "|" + dependency.InternalId);
                visit(dependency);
            }
        };
        foreach (var key in locator.Keys)
        {
            IList<IResourceLocation> found;
            if (!locator.Locate(key, null, out found)) continue;
            var ids = new List<string>();
            foreach (var location in found)
            {
                ids.Add(location.ProviderId + "|" + location.InternalId);
                visit(location);
            }
            keys[key.ToString()] = ids;
        }
        File.WriteAllText(args[1], JsonConvert.SerializeObject(new { keys = keys, locations = locations }, Formatting.Indented));
        Console.WriteLine("Catalog keys=" + keys.Count + " locations=" + locations.Count);
        return 0;
    }
}
