// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Autofac;
using Forged.MapServer.AI.CoreAI;
using Forged.MapServer.Chat.Channels;
using Forged.MapServer.DataStorage.Structs.A;
using Forged.MapServer.Entities.AreaTriggers;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Forged.MapServer.Groups;
using Forged.MapServer.Guilds;
using Forged.MapServer.Movement;
using Forged.MapServer.Scripting.Activators;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.IAreaTrigger;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Scripting.Interfaces.IPlayer;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Scripting.Registers;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
using Game.Common;
using Game.Common.Extendability;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Scripting;

// Manages registration, loading, and execution of Scripts.
public class ScriptManager
{
    private readonly GameObjectManager _gameObjectManager;

    // creature entry + chain ID
    private readonly MultiMap<Tuple<uint, ushort>, SplineChainLink> _mSplineChainsMap = new();

    private readonly Dictionary<Type, List<IScriptObject>> _scriptByType = new();
    private readonly Dictionary<Type, Dictionary<PlayerClass, List<IScriptObject>>> _scriptClassByType = new();

    // spline chains
    private readonly Dictionary<Type, ScriptRegistry> _scriptStorage = new();

    private readonly Dictionary<uint, WaypointPath> _waypointStore = new();
    private readonly WorldDatabase _worldDatabase;
    private readonly ClassFactory _classFactory;
    private readonly IConfiguration _configuration;
    private uint _scriptCount;

    public ScriptManager(GameObjectManager gameObjectManager, WorldDatabase worldDatabase, ClassFactory classFactory, IConfiguration configuration)
    {
        _gameObjectManager = gameObjectManager;
        _worldDatabase = worldDatabase;
        _classFactory = classFactory;
        _configuration = configuration;
    }

    public void Initialize()
    {
        var oldMSTime = Time.MSTime;

        LoadDatabase();

        Log.Logger.Information("Loading C# scripts");

        FillSpellSummary();

        //Load Scripts.dll
        LoadScripts();

        Log.Logger.Information($"Loaded {GetScriptCount()} C# scripts in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }

    //AreaTriggerScript
    public bool OnAreaTrigger(Player player, AreaTriggerRecord trigger, bool entered)
    {
        if (entered)
            foreach (var script in _gameObjectManager.GetAreaTriggerScriptIds(trigger.Id))
                return RunScriptRet<IAreaTriggerOnTrigger>(a => a.OnTrigger(player, trigger), script);
        else
            foreach (var script in _gameObjectManager.GetAreaTriggerScriptIds(trigger.Id))
                return RunScriptRet<IAreaTriggerOnExit>(p => p.OnExit(player, trigger), script);

        return false;
    }

    #region Main Script API

    public void AddScript<T>(T script) where T : IScriptObject
    {
        var interfaces = script.GetType().GetInterfaces();
        var hasClass = interfaces.Any(iface => iface.Name == nameof(IClassRescriction));

        if (!_scriptStorage.TryGetValue(script.GetType(), out var scriptReg))
        {
            scriptReg = new ScriptRegistry(this, _classFactory.Resolve<GameObjectManager>());
            _scriptStorage[script.GetType()] = scriptReg;
        }

        scriptReg.AddScript(script);

        foreach (var iface in interfaces)
            AddInterface(iface, script, hasClass);
    }

    public void ForEach<T>(Action<T> a) where T : IScriptObject
    {
        if (!_scriptByType.TryGetValue(typeof(T), out var ifaceImp))
            return;

        foreach (T s in ifaceImp)
            try
            {
                a.Invoke(s);
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex);
            }
    }

    public void ForEach<T>(PlayerClass playerClass, Action<T> a) where T : IScriptObject, IClassRescriction
    {
        if (!_scriptClassByType.TryGetValue(typeof(T), out var classKvp))
            return;

        if (classKvp.TryGetValue(playerClass, out var ifaceImp))
            foreach (T s in ifaceImp)
                try
                {
                    a.Invoke(s);
                }
                catch (Exception ex)
                {
                    Log.Logger.Error(ex);
                }

        if (classKvp.TryGetValue(PlayerClass.None, out var ifaceImpNone))
            foreach (T s in ifaceImpNone)
                try
                {
                    a.Invoke(s);
                }
                catch (Exception ex)
                {
                    Log.Logger.Error(ex);
                }
    }

    public T GetScript<T>(uint id) where T : IScriptObject
    {
        return _scriptStorage.TryGetValue(typeof(T), out var scriptReg) ? scriptReg.GetScriptById<T>(id) : default;
    }

    public ScriptRegistry GetScriptRegistry<T>()
    {
        return _scriptStorage.TryGetValue(typeof(T), out var scriptReg) ? scriptReg : null;
    }

    public void RunScript<T>(Action<T> a, uint id) where T : IScriptObject
    {
        var script = GetScript<T>(id);

        if (script == null)
            return;

        try
        {
            a.Invoke(script);
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex);
        }
    }

    public bool RunScriptRet<T>(Func<T, bool> func, uint id, bool ret = false) where T : IScriptObject
    {
        return RunScriptRet<T, bool>(func, id, ret);
    }

    public TU RunScriptRet<T, TU>(Func<T, TU> func, uint id, TU ret = default) where T : IScriptObject
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
            Log.Logger.Error(e);
        }

        return ret;
    }

    private void AddInterface<T>(Type iface, T script, bool hasClass) where T : IScriptObject
    {
        if (iface.Name == nameof(IScriptObject))
            return;

        if (!_scriptStorage.TryGetValue(iface, out var scriptReg))
        {
            scriptReg = new ScriptRegistry(this, _classFactory.Resolve<GameObjectManager>());
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

    #endregion Main Script API

    #region Loading and Unloading

    public void FillSpellSummary()
    {
        UnitAI.FillAISpellInfo(_classFactory.Resolve<SpellManager>());
    }

    public WaypointPath GetPath(uint creatureEntry)
    {
        return _waypointStore.LookupByKey(creatureEntry);
    }

    public uint GetScriptCount()
    {
        return _scriptCount;
    }

    public List<SplineChainLink> GetSplineChain(Creature who, ushort chainId)
    {
        return GetSplineChain(who.Entry, chainId);
    }

    public void IncrementScriptCount()
    {
        ++_scriptCount;
    }

    public void LoadDatabase()
    {
        LoadScriptWaypoints();
        LoadScriptSplineChains();
    }

    public void LoadScripts()
    {
        var assemblies = IOHelpers.GetAllAssembliesInDir(_configuration.GetDefaultValue("ScriptsDirectory", Path.Combine(AppContext.BaseDirectory, "Scripts")));

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
                            Log.Logger.Error("Script: {0} contains no Public Constructors with the right parameter types. Can't load script.", type.Name);

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
                                activatedObj = numArgsMin switch
                                {
                                    0 or 99                                                 => Activator.CreateInstance(type) as IScriptObject,
                                    1 when paramType != null && paramType == typeof(string) => Activator.CreateInstance(type, name) as IScriptObject,
                                    _                                                       => activatedObj
                                };
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

                        if (activatedObj != null)
                            activatedObj.ClassFactory = _classFactory;

                        if (activatedObj != null && IOHelpers.DoesTypeSupportInterface(activatedObj.GetType(), typeof(IScriptAutoAdd)))
                            AddScript(activatedObj);

                        if (registers.TryGetValue(attribute.GetType(), out var reg))
                            reg.Register(attribute, activatedObj, name);
                    }
                }
            }
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

    private void RegisterActivators(Dictionary<string, IScriptActivator> activators, Type type)
    {
        if (!IOHelpers.DoesTypeSupportInterface(type, typeof(IScriptActivator)))
            return;

        IScriptActivator asa = null;

        if (type.GetConstructors().Any(c => c.GetParameters().Any(p => p.ParameterType == typeof(ClassFactory))))
            asa = Activator.CreateInstance(type, _classFactory) as IScriptActivator;
        else
            asa = Activator.CreateInstance(type) as IScriptActivator;

        if (asa == null)
            return;

        foreach (var t in asa.ScriptBaseTypes)
            activators[t] = asa;
    }
    private void RegisterRegistors(Dictionary<Type, IScriptRegister> registers, Type type)
    {
        if (!IOHelpers.DoesTypeSupportInterface(type, typeof(IScriptRegister)))
            return;

        IScriptRegister newReg = null;

        if (type.GetConstructors().Any(c => c.GetParameters().Any(p => p.ParameterType == typeof(ClassFactory))))
            newReg = Activator.CreateInstance(type, _classFactory) as IScriptRegister;
        else
            newReg = Activator.CreateInstance(type) as IScriptRegister;

        if (newReg != null)
            registers[newReg.AttributeType] = newReg;
    }

    private List<SplineChainLink> GetSplineChain(uint entry, ushort chainId)
    {
        return _mSplineChainsMap.LookupByKey(Tuple.Create(entry, chainId));
    }

    private void LoadScriptSplineChains()
    {
        var oldMSTime = Time.MSTime;

        _mSplineChainsMap.Clear();

        //                                             0      1        2         3                 4            5
        var resultMeta = _worldDatabase.Query("SELECT entry, chainId, splineId, expectedDuration, msUntilNext, velocity FROM script_spline_chain_meta ORDER BY entry asc, chainId asc, splineId asc");
        //                                           0      1        2         3    4  5  6
        var resultWp = _worldDatabase.Query("SELECT entry, chainId, splineId, wpId, x, y, z FROM script_spline_chain_waypoints ORDER BY entry asc, chainId asc, splineId asc, wpId asc");

        if (resultMeta.IsEmpty() ||
            resultWp.IsEmpty())
            Log.Logger.Information("Loaded spline chain _data for 0 chains, consisting of 0 splines with 0 waypoints. DB tables `script_spline_chain_meta` and `script_spline_chain_waypoints` are empty.");
        else
        {
            uint chainCount = 0, splineCount = 0, wpCount = 0;

            do
            {
                var entry = resultMeta.Read<uint>(0);
                var chainId = resultMeta.Read<ushort>(1);
                var splineId = resultMeta.Read<byte>(2);

                var key = Tuple.Create(entry, chainId);

                if (!_mSplineChainsMap.ContainsKey(key))
                    _mSplineChainsMap[key] = new List<SplineChainLink>();

                var chain = _mSplineChainsMap[Tuple.Create(entry, chainId)];

                if (splineId != chain.Count)
                {
                    Log.Logger.Warning("Creature #{0}: Chain {1} has orphaned spline {2}, skipped.", entry, chainId, splineId);

                    continue;
                }

                var expectedDuration = resultMeta.Read<uint>(3);
                var msUntilNext = resultMeta.Read<uint>(4);
                var velocity = resultMeta.Read<float>(5);
                chain.Add(new SplineChainLink(expectedDuration, msUntilNext, velocity));

                if (splineId == 0)
                    ++chainCount;

                ++splineCount;
            } while (resultMeta.NextRow());

            do
            {
                var entry = resultWp.Read<uint>(0);
                var chainId = resultWp.Read<ushort>(1);
                var splineId = resultWp.Read<byte>(2);
                var wpId = resultWp.Read<byte>(3);
                var posX = resultWp.Read<float>(4);
                var posY = resultWp.Read<float>(5);
                var posZ = resultWp.Read<float>(6);

                if (!_mSplineChainsMap.TryGetValue(Tuple.Create(entry, chainId), out var chain))
                {
                    Log.Logger.Warning("Creature #{0} has waypoint _data for spline chain {1}. No such chain exists - entry skipped.", entry, chainId);

                    continue;
                }

                if (splineId >= chain.Count)
                {
                    Log.Logger.Warning("Creature #{0} has waypoint _data for spline ({1},{2}). The specified chain does not have a spline with this index - entry skipped.", entry, chainId, splineId);

                    continue;
                }

                var spline = chain[splineId];

                if (wpId != spline.Points.Count)
                {
                    Log.Logger.Warning("Creature #{0} has orphaned waypoint _data in spline ({1},{2}) at index {3}. Skipped.", entry, chainId, splineId, wpId);

                    continue;
                }

                spline.Points.Add(new Vector3(posX, posY, posZ));
                ++wpCount;
            } while (resultWp.NextRow());

            Log.Logger.Information("Loaded spline chain _data for {0} chains, consisting of {1} splines with {2} waypoints in {3} ms", chainCount, splineCount, wpCount, Time.GetMSTimeDiffToNow(oldMSTime));
        }
    }

    private void LoadScriptWaypoints()
    {
        var oldMSTime = Time.MSTime;

        // Drop Existing Waypoint list
        _waypointStore.Clear();

        ulong entryCount = 0;

        // Load Waypoints
        var result = _worldDatabase.Query("SELECT COUNT(entry) FROM script_waypoint GROUP BY entry");

        if (!result.IsEmpty())
            entryCount = result.Read<uint>(0);

        Log.Logger.Information($"Loading Script Waypoints for {entryCount} creature(s)...");

        //                                0       1         2           3           4           5
        result = _worldDatabase.Query("SELECT entry, pointid, location_x, location_y, location_z, waittime FROM script_waypoint ORDER BY pointid");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 Script Waypoints. DB table `script_waypoint` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var entry = result.Read<uint>(0);
            var id = result.Read<uint>(1);
            var x = result.Read<float>(2);
            var y = result.Read<float>(3);
            var z = result.Read<float>(4);
            var waitTime = result.Read<uint>(5);

            var info = _gameObjectManager.GetCreatureTemplate(entry);

            if (info == null)
            {
                Log.Logger.Error($"SystemMgr: DB table script_waypoint has waypoint for non-existant creature entry {entry}");

                continue;
            }

            if (info.ScriptID == 0)
                Log.Logger.Error($"SystemMgr: DB table script_waypoint has waypoint for creature entry {entry}, but creature does not have ScriptName defined and then useless.");

            if (!_waypointStore.ContainsKey(entry))
                _waypointStore[entry] = new WaypointPath();

            var path = _waypointStore[entry];
            path.ID = entry;
            path.Nodes.Add(new WaypointNode(id, x, y, z, null, waitTime));

            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} Script Waypoint nodes in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    #endregion Loading and Unloading

    #region Spells and Auras

    public Dictionary<IAuraScriptLoaderGetAuraScript, uint> CreateAuraScriptLoaders(uint spellId)
    {
        var scriptDic = new Dictionary<IAuraScriptLoaderGetAuraScript, uint>();
        var bounds = _gameObjectManager.GetSpellScriptsBounds(spellId);

        var reg = GetScriptRegistry<IAuraScriptLoaderGetAuraScript>();

        if (reg == null)
            return scriptDic;

        foreach (var id in bounds)
        {
            var tmpscript = reg.GetScriptById<IAuraScriptLoaderGetAuraScript>(id);

            if (tmpscript == null)
                continue;

            scriptDic[tmpscript] = id;
        }

        return scriptDic;
    }

    public List<AuraScript> CreateAuraScripts(uint spellId, Aura invoker)
    {
        var scriptList = new List<AuraScript>();
        var bounds = _gameObjectManager.GetSpellScriptsBounds(spellId);

        var reg = GetScriptRegistry<IAuraScriptLoaderGetAuraScript>();

        if (reg == null)
            return scriptList;

        foreach (var id in bounds)
        {
            var tmpscript = reg.GetScriptById<IAuraScriptLoaderGetAuraScript>(id);

            var script = tmpscript?.GetAuraScript();

            if (script == null)
                continue;

            script._Init(tmpscript.GetName(), spellId, _classFactory);

            if (!script._Load(invoker))
                continue;

            scriptList.Add(script);
        }

        return scriptList;
    }

    public Dictionary<ISpellScriptLoaderGetSpellScript, uint> CreateSpellScriptLoaders(uint spellId)
    {
        var scriptDic = new Dictionary<ISpellScriptLoaderGetSpellScript, uint>();
        var bounds = _gameObjectManager.GetSpellScriptsBounds(spellId);

        var reg = GetScriptRegistry<ISpellScriptLoaderGetSpellScript>();

        if (reg == null)
            return scriptDic;

        foreach (var id in bounds)
        {
            var tmpscript = reg.GetScriptById<ISpellScriptLoaderGetSpellScript>(id);

            if (tmpscript == null)
                continue;

            scriptDic[tmpscript] = id;
        }

        return scriptDic;
    }

    //SpellScriptLoader
    public List<SpellScript> CreateSpellScripts(uint spellId, Spell invoker)
    {
        var scriptList = new List<SpellScript>();
        var bounds = _gameObjectManager.GetSpellScriptsBounds(spellId);

        var reg = GetScriptRegistry<ISpellScriptLoaderGetSpellScript>();

        if (reg == null)
            return scriptList;

        foreach (var id in bounds)
        {
            var tmpscript = reg.GetScriptById<ISpellScriptLoaderGetSpellScript>(id);

            var script = tmpscript?.GetSpellScript();

            if (script == null)
                continue;

            script._Init(tmpscript.GetName(), spellId, _classFactory);

            if (!script._Load(invoker))
                continue;

            scriptList.Add(script);
        }

        return scriptList;
    }

    #endregion Spells and Auras

    #region AreaTriggers

    public Dictionary<IAreaTriggerScriptLoaderGetTriggerScriptScript, uint> CreateAreaTriggerScriptLoaders(uint areaTriggerId)
    {
        var scriptDic = new Dictionary<IAreaTriggerScriptLoaderGetTriggerScriptScript, uint>();
        var bounds = _gameObjectManager.GetAreaTriggerScriptIds(areaTriggerId);

        var reg = GetScriptRegistry<IAreaTriggerScriptLoaderGetTriggerScriptScript>();

        if (reg == null)
            return scriptDic;

        foreach (var id in bounds)
        {
            var tmpscript = reg.GetScriptById<IAreaTriggerScriptLoaderGetTriggerScriptScript>(id);

            if (tmpscript == null)
                continue;

            scriptDic[tmpscript] = id;
        }

        return scriptDic;
    }

    public List<AreaTriggerScript> CreateAreaTriggerScripts(uint areaTriggerId, AreaTrigger invoker)
    {
        var scriptList = new List<AreaTriggerScript>();
        var bounds = _gameObjectManager.GetAreaTriggerScriptIds(areaTriggerId);

        var reg = GetScriptRegistry<IAreaTriggerScriptLoaderGetTriggerScriptScript>();

        if (reg == null)
            return scriptList;

        foreach (var id in bounds)
        {
            var tmpscript = reg.GetScriptById<IAreaTriggerScriptLoaderGetTriggerScriptScript>(id);

            var script = tmpscript?.GetAreaTriggerScript();

            if (script == null)
                continue;

            script._Init(tmpscript.GetName(), areaTriggerId);

            if (!script._Load(invoker))
                continue;

            scriptList.Add(script);
        }

        return scriptList;
    }

    #endregion AreaTriggers

    #region Player Chat

    public void OnPlayerChat(Player player, ChatMsg type, Language lang, string msg)
    {
        ForEach<IPlayerOnChat>(p => p.OnChat(player, type, lang, msg));
    }

    public void OnPlayerChat(Player player, ChatMsg type, Language lang, string msg, Player receiver)
    {
        ForEach<IPlayerOnChatWhisper>(p => p.OnChat(player, type, lang, msg, receiver));
    }

    public void OnPlayerChat(Player player, ChatMsg type, Language lang, string msg, PlayerGroup group)
    {
        ForEach<IPlayerOnChatGroup>(p => p.OnChat(player, type, lang, msg, group));
    }

    public void OnPlayerChat(Player player, ChatMsg type, Language lang, string msg, Guild guild)
    {
        ForEach<IPlayerOnChatGuild>(p => p.OnChat(player, type, lang, msg, guild));
    }

    public void OnPlayerChat(Player player, ChatMsg type, Language lang, string msg, Channel channel)
    {
        ForEach<IPlayerOnChatChannel>(p => p.OnChat(player, type, lang, msg, channel));
    }

    #endregion Player Chat
}