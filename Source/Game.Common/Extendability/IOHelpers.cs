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
	private static readonly Dictionary<string, List<Assembly>> _loadedAssemblies = new();

	public static List<Assembly> GetAllAssembliesInDir(string path, bool loadGameAssembly = true)
	{
		var assemblies = _loadedAssemblies.LookupByKey(path);

		if (assemblies != null)
			return assemblies;
		else
			assemblies = new List<Assembly>();

		if (!Directory.Exists(path))
			Directory.CreateDirectory(path);

		var dir = new DirectoryInfo(path);

		var dlls = dir.GetFiles("*.dll", SearchOption.AllDirectories);

		foreach (var dll in dlls)
			assemblies.Add(Assembly.LoadFile(dll.FullName));

		if (loadGameAssembly)
			assemblies.Add(Assembly.GetExecutingAssembly());

		_loadedAssemblies[path] = assemblies;

		return assemblies;
	}

	public static IEnumerable<T> GetAllObjectsFromAssemblies<T>(string path)
	{
		var assemblies = GetAllAssembliesInDir(Path.Combine(AppContext.BaseDirectory, "Scripts"));

		if (File.Exists(AppContext.BaseDirectory + "Scripts.dll"))
		{
			var scrAss = Assembly.LoadFile(AppContext.BaseDirectory + "Scripts.dll");

			if (scrAss != null)
				assemblies.Add(scrAss);
		}

		foreach (var assembly in assemblies)
			foreach (var type in assembly.GetTypes())
				if (DoesTypeSupportInterface(type, typeof(T)))
					yield return (T)Activator.CreateInstance(type);
	}

	public static bool DoesTypeSupportInterface(Type type, Type inter)
	{
		if (type == inter) return false;

		if (inter.IsAssignableFrom(type))
			return true;

		if (type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == inter))
			return true;

		return type.GetInterfaces().Any(i => i == inter);
	}

	public static bool AreObjectsNotEqual(object obj1, object obj2)
	{
		return !AreObjectsEqual(obj1, obj2);
	}

    /// <summary>
    ///  Compares the values of 2 objects
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
}