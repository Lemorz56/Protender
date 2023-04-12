using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Google.Protobuf;
using Serilog;

namespace Protender;

public static class AssemblyUtils
{
    public static Dictionary<string, Type> LoadClasses(string assemblyName)
    {
        var assemblyWorking = FindAssemblyInReferences(assemblyName);

        //if (assemblyWorking == null) return classes;

        return assemblyWorking.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && !t.IsInterface && typeof(IMessage).IsAssignableFrom(t))
            .ToDictionary(type => type.Name);
    }

    private static Assembly FindAssemblyInReferences(string prefixName)
    {
        try
        {
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
            var loadedPaths = loadedAssemblies.Select(a => a.Location).ToArray();

            var referencedPaths = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll");
            var toLoad = referencedPaths.Where(r => !loadedPaths.Contains(r, StringComparer.InvariantCultureIgnoreCase))
                .Where(x => x.Contains(prefixName))
                .ToList();

            toLoad.ForEach(path =>
                loadedAssemblies.Add(AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(path))));

            var filtered = loadedAssemblies.FirstOrDefault(x => x.FullName != null && x.FullName.Contains(prefixName));
            if (filtered == null) throw new NullReferenceException(nameof(filtered));
            return filtered;
        }
        catch (Exception e)
        {
            Log.Error(e, "Something went wrong when trying to find assembly references of prefix {prefix}", prefixName);
            throw;
        }
    }
}