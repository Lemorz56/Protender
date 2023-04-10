using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Google.Protobuf;

namespace Protender;

public static class AssemblyUtils
{
    public static Dictionary<string, Type?> LoadClasses(string assemblyName)
    {
        var classes = new Dictionary<string, Type?>();
        var assemblyWorking = FindAssemblyInReferences(assemblyName);

        if (assemblyWorking == null) return classes;

        foreach (var type in assemblyWorking.GetTypes().Where(t => t.IsClass
                                                                   && !t.IsAbstract
                                                                   && !t.IsInterface
                                                                   && typeof(IMessage).IsAssignableFrom(t)
                 ))
            classes.Add(type.Name, type);

        return classes;
    }

    private static Assembly? FindAssemblyInReferences(string prefixName)
    {
        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
        var loadedPaths = loadedAssemblies.Select(a => a.Location).ToArray();

        var referencedPaths = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll");
        var toLoad = referencedPaths.Where(r => !loadedPaths.Contains(r, StringComparer.InvariantCultureIgnoreCase))
            .Where(x => x.Contains(prefixName))
            .ToList();

        toLoad.ForEach(path => loadedAssemblies.Add(AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(path))));

        return loadedAssemblies.FirstOrDefault(x => x.FullName != null && x.FullName.Contains(prefixName));
    }
}