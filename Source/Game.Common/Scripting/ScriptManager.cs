// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Threading.Channels;
using Framework.Constants;
using Framework.Database;
using Game.Common.DataStorage.Structs.A;
using Game.Common.Extendability;
using Game.Common.Scripting.Activators;
using Game.Common.Scripting.Interfaces;
using Game.Common.Scripting.Registers;

namespace Game.Common.Scripting;

// Manages registration, loading, and execution of Scripts.
public class ScriptManager 
{
	private readonly List<IScriptObject> _blankList = new();
	
	private readonly Dictionary<Type, Dictionary<PlayerClass, List<IScriptObject>>> _scriptClassByType = new();
	private readonly Dictionary<Type, List<IScriptObject>> _scriptByType = new();
	private readonly Dictionary<Type, ScriptRegistry> _scriptStorage = new();
	private uint _scriptCount;

	private ScriptManager() { }

	//Initialization
	public void Initialize()
	{
		var oldMSTime = Time.MSTime;

		Log.outInfo(LogFilter.ServerLoading, "Loading C# scripts");

		//Load Scripts.dll
		LoadScripts();

		// MapScripts
        Log.outInfo(LogFilter.ServerLoading, $"Loaded {GetScriptCount()} C# scripts in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
	}

    public void ForEach<T>(Action<T> a) where T : IScriptObject
	{
		if (_scriptByType.TryGetValue(typeof(T), out var ifaceImp))
			foreach (T s in ifaceImp)
                try
                {
                    a.Invoke(s);
                }
                catch (Exception ex)
                {
					Log.outException(ex);
                }
    }

	public void ForEach<T>(PlayerClass playerClass, Action<T> a) where T : IScriptObject, IClassRescriction
	{
		if (_scriptClassByType.TryGetValue(typeof(T), out var classKvp))
		{
			if (classKvp.TryGetValue(playerClass, out var ifaceImp))
				foreach (T s in ifaceImp)
                    try
                    {
                        a.Invoke(s);
                    }
                    catch (Exception ex)
                    {
                        Log.outException(ex);
                    }

            if (classKvp.TryGetValue(PlayerClass.None, out var ifaceImpNone))
				foreach (T s in ifaceImpNone)
                    try
                    {
                        a.Invoke(s);
                    }
                    catch (Exception ex)
                    {
                        Log.outException(ex);
                    }
        }
	}

	public bool RunScriptRet<T>(Func<T, bool> func, uint id, bool ret = false) where T : IScriptObject
	{
		return RunScriptRet<T, bool>(func, id, ret);
	}

	public U RunScriptRet<T, U>(Func<T, U> func, uint id, U ret = default) where T : IScriptObject
	{
		var script = GetScript<T>(id);

		if (script == null)
			return ret;

        try
        {
            return func.Invoke(script);
        }
        catch (Exception e)
        {
            Log.outException(e);
        }

        return ret;
    }

	public void RunScript<T>(Action<T> a, uint id) where T : IScriptObject
	{
		var script = GetScript<T>(id);

		if (script != null)
            try
            {
                a.Invoke(script);
            }
            catch (Exception ex)
            {
                Log.outException(ex);
            }
    }

	public void AddScript<T>(T script) where T : IScriptObject
	{
		var interfaces = script.GetType().GetInterfaces();
		var hasClass = interfaces.Any(iface => iface.Name == nameof(IClassRescriction));

		if (!_scriptStorage.TryGetValue(script.GetType(), out var scriptReg))
		{
			scriptReg = new ScriptRegistry();
			_scriptStorage[script.GetType()] = scriptReg;
		}

		scriptReg.AddScript(script);

		foreach (var iface in interfaces)
			AddInterface(iface, script, hasClass);
	}

	private void AddInterface<T>(Type iface, T script, bool hasClass) where T : IScriptObject
	{
		if (iface.Name == nameof(IScriptObject))
			return;

		if (!_scriptStorage.TryGetValue(iface, out var scriptReg))
		{
			scriptReg = new ScriptRegistry();
			_scriptStorage[iface] = scriptReg;
		}

		scriptReg.AddScript(script);
		_scriptByType.AddIf(iface, script, (existing, newSc) => existing.GetName() != newSc.GetName());

		if (IOHelpers.DoesTypeSupportInterface(iface, typeof(IScriptObject)))
			_scriptStorage[iface] = scriptReg;

		if (hasClass)
		{
			if (!_scriptClassByType.TryGetValue(iface, out var classDict))
			{
				classDict = new Dictionary<PlayerClass, List<IScriptObject>>();
				_scriptClassByType[iface] = classDict;
			}

			classDict.AddIf(((IClassRescriction)script).PlayerClass, script, (existing, newSc) => existing.GetName() != newSc.GetName());
		}

		foreach (var f in iface.GetInterfaces())
			AddInterface(f, script, hasClass);
	}

	public ScriptRegistry GetScriptRegistry<T>()
	{
		if (_scriptStorage.TryGetValue(typeof(T), out var scriptReg))
			return scriptReg;

		return null;
	}

	public T GetScript<T>(uint id) where T : IScriptObject
	{
		if (_scriptStorage.TryGetValue(typeof(T), out var scriptReg))
			return scriptReg.GetScriptById<T>(id);

		return default;
	}


	public void LoadScripts()
	{
		var assemblies = IOHelpers.GetAllAssembliesInDir(Path.Combine(AppContext.BaseDirectory, "Scripts"));

		if (File.Exists(AppContext.BaseDirectory + "Scripts.dll"))
		{
			var scrAss = Assembly.LoadFile(AppContext.BaseDirectory + "Scripts.dll");

			if (scrAss != null)
				assemblies.Add(scrAss);
		}

		Dictionary<string, IScriptActivator> activators = new();
		Dictionary<Type, IScriptRegister> registers = new();

		foreach (var asm in assemblies)
			foreach (var type in asm.GetTypes())
			{
				RegisterActivators(activators, type);
				RegisterRegistors(registers, type);
			}

		foreach (var assembly in assemblies)
			foreach (var type in assembly.GetTypes())
			{
				var attributes = (ScriptAttribute[])type.GetCustomAttributes<ScriptAttribute>(true);

				if (!attributes.Empty())
				{
					var constructors = type.GetConstructors(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.CreateInstance);
					var numArgsMin = 99;

					foreach (var attribute in attributes)
					{
						var name = type.Name;
						Type paramType = null;
						var validArgs = true;
						var i = 0;

						foreach (var constructor in constructors)
						{
							var parameters = constructor.GetParameters();

							if (parameters.Length < numArgsMin)
							{
								numArgsMin = parameters.Length;

								if (numArgsMin == 1)
									paramType = parameters.FirstOrDefault().ParameterType;
							}

							if (parameters.Length != attribute.Args.Length)
								continue;

							foreach (var arg in parameters)
								if (arg.ParameterType != attribute.Args[i++].GetType())
								{
									validArgs = false;

									break;
								}

							if (validArgs)
								break;
						}

						if (!validArgs)
						{
							Log.outError(LogFilter.Scripts, "Script: {0} contains no Public Constructors with the right parameter types. Can't load script.", type.Name);

							continue;
						}

						if (!attribute.Name.IsEmpty())
							name = attribute.Name;

						IScriptObject activatedObj = null;

						var typeIfaces = type.GetInterfaces();
						var basetypes = new List<Type>();
						var baseType = type.BaseType;

						while (baseType != null)
						{
							basetypes.Add(baseType);
							baseType = baseType.BaseType;
						}


						foreach (var baseT in basetypes)
							if (!string.IsNullOrEmpty(baseT.Name) && activators.TryGetValue(baseT.Name, out var scriptActivator))
							{
								activatedObj = scriptActivator.Activate(type, name, attribute);

								if (activatedObj != null)
									break;
							}

						if (activatedObj == null)
							foreach (var intFace in typeIfaces)
								if (!string.IsNullOrEmpty(intFace.Name) && activators.TryGetValue(intFace.Name, out var scriptActivator))
								{
									activatedObj = scriptActivator.Activate(type, name, attribute);

									if (activatedObj != null)
										break;
								}

						if (activatedObj == null)
							if (attribute.Args.Empty())
							{
								if (numArgsMin == 0 || numArgsMin == 99)
									activatedObj = Activator.CreateInstance(type) as IScriptObject;
								else if (numArgsMin == 1 &&
										paramType != null &&
										paramType == typeof(string))
									activatedObj = Activator.CreateInstance(type, name) as IScriptObject;
							}
							else
							{
								if (numArgsMin == 1 &&
									paramType != null &&
									paramType != typeof(string))
									activatedObj = Activator.CreateInstance(type, attribute.Args) as IScriptObject;
								else
									activatedObj = Activator.CreateInstance(type,
																			new object[]
																			{
																				name
																			}.Combine(attribute.Args)) as IScriptObject;
							}

						if (activatedObj != null && IOHelpers.DoesTypeSupportInterface(activatedObj.GetType(), typeof(IScriptAutoAdd)))
							AddScript(activatedObj);

						if (registers.TryGetValue(attribute.GetType(), out var reg))
							reg.Register(attribute, activatedObj, name);
					}
				}
			}
	}

	private static void RegisterActivators(Dictionary<string, IScriptActivator> activators, Type type)
	{
		if (IOHelpers.DoesTypeSupportInterface(type, typeof(IScriptActivator)))
		{
			var asa = (IScriptActivator)Activator.CreateInstance(type);

			foreach (var t in asa.ScriptBaseTypes)
				activators[t] = asa;
		}
	}

	private static void RegisterRegistors(Dictionary<Type, IScriptRegister> registers, Type type)
	{
		if (IOHelpers.DoesTypeSupportInterface(type, typeof(IScriptRegister)))
		{
			var newReg = (IScriptRegister)Activator.CreateInstance(type);
			registers[newReg.AttributeType] = newReg;
		}
	}

    public void IncrementScriptCount()
	{
		++_scriptCount;
	}

	public uint GetScriptCount()
	{
		return _scriptCount;
	}

	//Reloading
	public void Reload()
	{
		Unload();
		LoadScripts();
	}

	//Unloading
	public void Unload()
	{
		foreach (var entry in _scriptStorage)
		{
			var scriptRegistry = entry.Value;
			scriptRegistry.Unload();
		}

		_scriptStorage.Clear();
		_scriptByType.Clear();
		_scriptClassByType.Clear();
	}
}
