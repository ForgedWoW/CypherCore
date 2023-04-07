// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Game.Common.Extendability;

public static class IOHelpers
{
    /// <summary>
    ///     Compares the values of 2 objects
    /// </summary>
    /// <returns> if types are equal and have the same property values </returns>
    public static bool AreObjectsEqual(object obj1, object obj2)
    {
        if (obj1 == null || obj2 == null)
            return obj1 == obj2;

        var type = obj1.GetType();

        if (type != obj2.GetType())
            return false;

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            var value1 = property.GetValue(obj1);
            var value2 = property.GetValue(obj2);

            if (!Equals(value1, value2))
                return false;
        }

        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

        foreach (var field in fields)
        {
            var value1 = field.GetValue(obj1);
            var value2 = field.GetValue(obj2);

            if (!Equals(value1, value2))
                return false;
        }

        return true;
    }

    public static bool AreObjectsNotEqual(object obj1, object obj2)
    {
        return !AreObjectsEqual(obj1, obj2);
    }

    public static bool DoesTypeSupportInterface(Type type, Type inter)
    {
        if (type == inter) return false;

        if (inter.IsAssignableFrom(type))
            return true;

        return type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == inter) || type.GetInterfaces().Any(i => i == inter);
    }

    public static List<Assembly> GetAllAssembliesInDir(string path, bool loadGameAssembly = true, bool loadScriptsDll = true)
    {
        path ??= ".\\Scripts";
        var assemblies = new List<Assembly>();

        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        var dir = new DirectoryInfo(path);

        var dlls = dir.GetFiles("*.dll", SearchOption.AllDirectories);

        assemblies.AddRange(dlls.Select(dll => Assembly.LoadFile(dll.FullName)));

        if (loadGameAssembly)
            assemblies.Add(Assembly.GetExecutingAssembly());

        if (loadScriptsDll && File.Exists(AppContext.BaseDirectory + "Scripts.dll"))
            assemblies.Add(Assembly.LoadFile(AppContext.BaseDirectory + "Scripts.dll"));

        return assemblies;
    }

    public static IEnumerable<T> GetAllObjectsFromAssemblies<T>(string path)
    {
        var assemblies = GetAllAssembliesInDir(path);

        foreach (var type in from assembly in assemblies from type in assembly.GetTypes() where DoesTypeSupportInterface(type, typeof(T)) select type)
            yield return (T)Activator.CreateInstance(type);
    }
}