// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.AreaTriggers;
using Forged.MapServer.Events;
using Forged.MapServer.Globals;
using Forged.MapServer.Movement;
using Forged.MapServer.Spells;
using Forged.MapServer.Text;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.AI.SmartScripts;

public class SmartAIManager
{
    private readonly AreaTriggerDataStorage _areaTriggerDataStorage;
    private readonly CliDB _cliDB;
    private readonly IConfiguration _configuration;
    private readonly ConversationDataStorage _conversationDataStorage;
    private readonly CreatureTextManager _creatureTextManager;
    private readonly DB2Manager _db2Manager;
    private readonly GameEventManager _eventManager;
    private readonly MultiMap<int, SmartScriptHolder>[] _eventMap = new MultiMap<int, SmartScriptHolder>[(int)SmartScriptType.Max];
    private readonly GameObjectManager _gameObjectManager;
    private readonly SpellManager _spellManager;
    private readonly Dictionary<uint, WaypointPath> _waypointStore = new();
    private readonly WorldDatabase _worldDatabase;

    public SmartAIManager(WorldDatabase worldDatabase, CliDB cliDB, IConfiguration configuration, GameEventManager eventManager,
                          GameObjectManager gameObjectManager, AreaTriggerDataStorage areaTriggerDataStorage, SpellManager spellManager,
                          DB2Manager db2Manager, ConversationDataStorage conversationDataStorage, CreatureTextManager creatureTextManager)
    {
        _worldDatabase = worldDatabase;
        _cliDB = cliDB;
        _configuration = configuration;
        _eventManager = eventManager;
        _gameObjectManager = gameObjectManager;
        _areaTriggerDataStorage = areaTriggerDataStorage;
        _spellManager = spellManager;
        _db2Manager = db2Manager;
        _conversationDataStorage = conversationDataStorage;
        _creatureTextManager = creatureTextManager;

        for (byte i = 0; i < (int)SmartScriptType.Max; i++)
            _eventMap[i] = new MultiMap<int, SmartScriptHolder>();
    }

    public static SmartScriptHolder FindLinkedSourceEvent(List<SmartScriptHolder> list, uint eventId)
    {
        var sch = list.Find(p => p.Link == eventId);

        return sch;
    }

    public static uint GetEventMask(SmartEvents smartEvent) =>
        smartEvent switch
        {
            SmartEvents.UpdateIc => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.TimedActionlist,
            SmartEvents.UpdateOoc => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject + SmartScriptTypeMaskId.Instance + SmartScriptTypeMaskId.AreatrigggerEntity,
            SmartEvents.HealthPct => SmartScriptTypeMaskId.Creature,
            SmartEvents.ManaPct => SmartScriptTypeMaskId.Creature,
            SmartEvents.Aggro => SmartScriptTypeMaskId.Creature,
            SmartEvents.Kill => SmartScriptTypeMaskId.Creature,
            SmartEvents.Death => SmartScriptTypeMaskId.Creature,
            SmartEvents.Evade => SmartScriptTypeMaskId.Creature,
            SmartEvents.SpellHit => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
            SmartEvents.Range => SmartScriptTypeMaskId.Creature,
            SmartEvents.OocLos => SmartScriptTypeMaskId.Creature,
            SmartEvents.Respawn => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
            SmartEvents.TargetHealthPct => SmartScriptTypeMaskId.Creature,
            SmartEvents.VictimCasting => SmartScriptTypeMaskId.Creature,
            SmartEvents.FriendlyHealth => SmartScriptTypeMaskId.Creature,
            SmartEvents.FriendlyIsCc => SmartScriptTypeMaskId.Creature,
            SmartEvents.FriendlyMissingBuff => SmartScriptTypeMaskId.Creature,
            SmartEvents.SummonedUnit => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
            SmartEvents.TargetManaPct => SmartScriptTypeMaskId.Creature,
            SmartEvents.AcceptedQuest => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
            SmartEvents.RewardQuest => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
            SmartEvents.ReachedHome => SmartScriptTypeMaskId.Creature,
            SmartEvents.ReceiveEmote => SmartScriptTypeMaskId.Creature,
            SmartEvents.HasAura => SmartScriptTypeMaskId.Creature,
            SmartEvents.TargetBuffed => SmartScriptTypeMaskId.Creature,
            SmartEvents.Reset => SmartScriptTypeMaskId.Creature,
            SmartEvents.IcLos => SmartScriptTypeMaskId.Creature,
            SmartEvents.PassengerBoarded => SmartScriptTypeMaskId.Creature,
            SmartEvents.PassengerRemoved => SmartScriptTypeMaskId.Creature,
            SmartEvents.Charmed => SmartScriptTypeMaskId.Creature,
            SmartEvents.CharmedTarget => SmartScriptTypeMaskId.Creature,
            SmartEvents.SpellHitTarget => SmartScriptTypeMaskId.Creature,
            SmartEvents.Damaged => SmartScriptTypeMaskId.Creature,
            SmartEvents.DamagedTarget => SmartScriptTypeMaskId.Creature,
            SmartEvents.Movementinform => SmartScriptTypeMaskId.Creature,
            SmartEvents.SummonDespawned => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
            SmartEvents.CorpseRemoved => SmartScriptTypeMaskId.Creature,
            SmartEvents.AiInit => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
            SmartEvents.DataSet => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
            SmartEvents.WaypointStart => SmartScriptTypeMaskId.Creature,
            SmartEvents.WaypointReached => SmartScriptTypeMaskId.Creature,
            SmartEvents.TransportAddplayer => SmartScriptTypeMaskId.Transport,
            SmartEvents.TransportAddcreature => SmartScriptTypeMaskId.Transport,
            SmartEvents.TransportRemovePlayer => SmartScriptTypeMaskId.Transport,
            SmartEvents.TransportRelocate => SmartScriptTypeMaskId.Transport,
            SmartEvents.InstancePlayerEnter => SmartScriptTypeMaskId.Instance,
            SmartEvents.AreatriggerOntrigger => SmartScriptTypeMaskId.Areatrigger + SmartScriptTypeMaskId.AreatrigggerEntity,
            SmartEvents.QuestAccepted => SmartScriptTypeMaskId.Quest,
            SmartEvents.QuestObjCompletion => SmartScriptTypeMaskId.Quest,
            SmartEvents.QuestRewarded => SmartScriptTypeMaskId.Quest,
            SmartEvents.QuestCompletion => SmartScriptTypeMaskId.Quest,
            SmartEvents.QuestFail => SmartScriptTypeMaskId.Quest,
            SmartEvents.TextOver => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
            SmartEvents.ReceiveHeal => SmartScriptTypeMaskId.Creature,
            SmartEvents.JustSummoned => SmartScriptTypeMaskId.Creature,
            SmartEvents.WaypointPaused => SmartScriptTypeMaskId.Creature,
            SmartEvents.WaypointResumed => SmartScriptTypeMaskId.Creature,
            SmartEvents.WaypointStopped => SmartScriptTypeMaskId.Creature,
            SmartEvents.WaypointEnded => SmartScriptTypeMaskId.Creature,
            SmartEvents.TimedEventTriggered => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
            SmartEvents.Update => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject + SmartScriptTypeMaskId.AreatrigggerEntity,
            SmartEvents.Link => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject + SmartScriptTypeMaskId.Areatrigger + SmartScriptTypeMaskId.Event + SmartScriptTypeMaskId.Gossip + SmartScriptTypeMaskId.Quest + SmartScriptTypeMaskId.Spell + SmartScriptTypeMaskId.Transport + SmartScriptTypeMaskId.Instance + SmartScriptTypeMaskId.AreatrigggerEntity,
            SmartEvents.GossipSelect => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
            SmartEvents.JustCreated => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
            SmartEvents.GossipHello => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
            SmartEvents.FollowCompleted => SmartScriptTypeMaskId.Creature,
            SmartEvents.PhaseChange => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
            SmartEvents.IsBehindTarget => SmartScriptTypeMaskId.Creature,
            SmartEvents.GameEventStart => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
            SmartEvents.GameEventEnd => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
            SmartEvents.GoLootStateChanged => SmartScriptTypeMaskId.Gameobject,
            SmartEvents.GoEventInform => SmartScriptTypeMaskId.Gameobject,
            SmartEvents.ActionDone => SmartScriptTypeMaskId.Creature,
            SmartEvents.OnSpellclick => SmartScriptTypeMaskId.Creature,
            SmartEvents.FriendlyHealthPCT => SmartScriptTypeMaskId.Creature,
            SmartEvents.DistanceCreature => SmartScriptTypeMaskId.Creature,
            SmartEvents.DistanceGameobject => SmartScriptTypeMaskId.Creature,
            SmartEvents.CounterSet => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
            SmartEvents.SceneStart => SmartScriptTypeMaskId.Scene,
            SmartEvents.SceneTrigger => SmartScriptTypeMaskId.Scene,
            SmartEvents.SceneCancel => SmartScriptTypeMaskId.Scene,
            SmartEvents.SceneComplete => SmartScriptTypeMaskId.Scene,
            SmartEvents.SummonedUnitDies => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
            SmartEvents.OnSpellCast => SmartScriptTypeMaskId.Creature,
            SmartEvents.OnSpellFailed => SmartScriptTypeMaskId.Creature,
            SmartEvents.OnSpellStart => SmartScriptTypeMaskId.Creature,
            SmartEvents.OnDespawn => SmartScriptTypeMaskId.Creature,
            _ => 0,
        };

    public static uint GetTypeMask(SmartScriptType smartScriptType) =>
        smartScriptType switch
        {
            SmartScriptType.Creature => SmartScriptTypeMaskId.Creature,
            SmartScriptType.GameObject => SmartScriptTypeMaskId.Gameobject,
            SmartScriptType.AreaTrigger => SmartScriptTypeMaskId.Areatrigger,
            SmartScriptType.Event => SmartScriptTypeMaskId.Event,
            SmartScriptType.Gossip => SmartScriptTypeMaskId.Gossip,
            SmartScriptType.Quest => SmartScriptTypeMaskId.Quest,
            SmartScriptType.Spell => SmartScriptTypeMaskId.Spell,
            SmartScriptType.Transport => SmartScriptTypeMaskId.Transport,
            SmartScriptType.Instance => SmartScriptTypeMaskId.Instance,
            SmartScriptType.TimedActionlist => SmartScriptTypeMaskId.TimedActionlist,
            SmartScriptType.Scene => SmartScriptTypeMaskId.Scene,
            SmartScriptType.AreaTriggerEntity => SmartScriptTypeMaskId.AreatrigggerEntity,
            SmartScriptType.AreaTriggerEntityServerside => SmartScriptTypeMaskId.AreatrigggerEntity,
            _ => 0,
        };

    public static void TC_SAI_IS_BOOLEAN_VALID(SmartScriptHolder e, uint value, [CallerArgumentExpression("value")] string valueName = null)
    {
        if (value > 1)
            Log.Logger.Error($"SmartAIMgr: {e} uses param {valueName} of type Boolean with value {value}, valid values are 0 or 1, skipped.");
    }

    public SmartScriptHolder FindLinkedEvent(List<SmartScriptHolder> list, uint link)
    {
        var sch = list.Find(p => p.EventId == link && p.GetEventType() == SmartEvents.Link);

        return sch;
    }

    public WaypointPath GetPath(uint id)
    {
        return _waypointStore.LookupByKey(id);
    }

    public List<SmartScriptHolder> GetScript(int entry, SmartScriptType type)
    {
        List<SmartScriptHolder> temp = new();

        if (_eventMap[(uint)type].ContainsKey(entry))
        {
            foreach (var holder in _eventMap[(uint)type][entry])
                temp.Add(new SmartScriptHolder(holder));
        }
        else
        {
            if (entry > 0) //first search is for guid (negative), do not drop error if not found
                Log.Logger.Debug("SmartAIMgr.GetScript: Could not load Script for Entry {0} ScriptType {1}.", entry, type);
        }

        return temp;
    }

    public void LoadFromDB()
    {
        var oldMSTime = Time.MSTime;

        for (byte i = 0; i < (int)SmartScriptType.Max; i++)
            _eventMap[i].Clear(); //Drop Existing SmartAI List

        var stmt = _worldDatabase.GetPreparedStatement(WorldStatements.SEL_SMART_SCRIPTS);
        var result = _worldDatabase.Query(stmt);

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 SmartAI scripts. DB table `smartai_scripts` is empty.");

            return;
        }

        var count = 0;

        do
        {
            SmartScriptHolder temp = new()
            {
                EntryOrGuid = result.Read<int>(0)
            };

            if (temp.EntryOrGuid == 0)
            {
                if (_configuration.GetDefaultValue("load.autoclean", false))
                    _worldDatabase.Execute($"DELETE FROM smart_scripts WHERE entryorguid = {temp.EntryOrGuid}");
                else
                    Log.Logger.Error("SmartAIMgr.LoadFromDB: invalid entryorguid (0), skipped loading.");

                continue;
            }

            var sourceType = (SmartScriptType)result.Read<byte>(1);

            if (sourceType >= SmartScriptType.Max)
            {
                if (_configuration.GetDefaultValue("load.autoclean", false))
                    _worldDatabase.Execute($"DELETE FROM smart_scripts WHERE entryorguid = {temp.EntryOrGuid}");
                else
                    Log.Logger.Error("SmartAIMgr.LoadSmartAI: invalid source_type ({0}), skipped loading.", sourceType);

                continue;
            }

            if (temp.EntryOrGuid >= 0)
                switch (sourceType)
                {
                    case SmartScriptType.Creature:
                        if (_gameObjectManager.GetCreatureTemplate((uint)temp.EntryOrGuid) == null)
                        {
                            if (_configuration.GetDefaultValue("load.autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM smart_scripts WHERE entryorguid = {temp.EntryOrGuid}");
                            else
                                Log.Logger.Error("SmartAIMgr.LoadSmartAI: Creature entry ({0}) does not exist, skipped loading.", temp.EntryOrGuid);

                            continue;
                        }

                        break;

                    case SmartScriptType.GameObject:
                    {
                        if (_gameObjectManager.GetGameObjectTemplate((uint)temp.EntryOrGuid) == null)
                        {
                            if (_configuration.GetDefaultValue("load.autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM smart_scripts WHERE entryorguid = {temp.EntryOrGuid}");
                            else
                                Log.Logger.Error("SmartAIMgr.LoadSmartAI: GameObject entry ({0}) does not exist, skipped loading.", temp.EntryOrGuid);

                            continue;
                        }

                        break;
                    }
                    case SmartScriptType.AreaTrigger:
                    {
                        if (_cliDB.AreaTableStorage.LookupByKey((uint)temp.EntryOrGuid) == null)
                        {
                            if (_configuration.GetDefaultValue("load.autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM smart_scripts WHERE entryorguid = {temp.EntryOrGuid}");
                            else
                                Log.Logger.Error("SmartAIMgr.LoadSmartAI: AreaTrigger entry ({0}) does not exist, skipped loading.", temp.EntryOrGuid);

                            continue;
                        }

                        break;
                    }
                    case SmartScriptType.Scene:
                    {
                        if (_gameObjectManager.GetSceneTemplate((uint)temp.EntryOrGuid) == null)
                        {
                            if (_configuration.GetDefaultValue("load.autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM smart_scripts WHERE entryorguid = {temp.EntryOrGuid}");
                            else
                                Log.Logger.Error("SmartAIMgr.LoadFromDB: Scene id ({0}) does not exist, skipped loading.", temp.EntryOrGuid);

                            continue;
                        }

                        break;
                    }
                    case SmartScriptType.Quest:
                    {
                        if (_gameObjectManager.GetQuestTemplate((uint)temp.EntryOrGuid) == null)
                        {
                            if (_configuration.GetDefaultValue("load.autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM smart_scripts WHERE entryorguid = {temp.EntryOrGuid}");
                            else
                                Log.Logger.Error($"SmartAIMgr.LoadFromDB: QuestId id ({temp.EntryOrGuid}) does not exist, skipped loading.");

                            continue;
                        }

                        break;
                    }
                    case SmartScriptType.TimedActionlist:
                        break; //nothing to check, really
                    case SmartScriptType.AreaTriggerEntity:
                    {
                        if (_areaTriggerDataStorage.GetAreaTriggerTemplate(new AreaTriggerId((uint)temp.EntryOrGuid, false)) == null)
                        {
                            if (_configuration.GetDefaultValue("load.autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM smart_scripts WHERE entryorguid = {temp.EntryOrGuid}");
                            else
                                Log.Logger.Error($"SmartAIMgr.LoadFromDB: AreaTrigger entry ({temp.EntryOrGuid} IsServerSide false) does not exist, skipped loading.");

                            continue;
                        }

                        break;
                    }
                    case SmartScriptType.AreaTriggerEntityServerside:
                    {
                        if (_areaTriggerDataStorage.GetAreaTriggerTemplate(new AreaTriggerId((uint)temp.EntryOrGuid, true)) == null)
                        {
                            if (_configuration.GetDefaultValue("load.autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM smart_scripts WHERE entryorguid = {temp.EntryOrGuid}");
                            else
                                Log.Logger.Error($"SmartAIMgr.LoadFromDB: AreaTrigger entry ({temp.EntryOrGuid} IsServerSide true) does not exist, skipped loading.");

                            continue;
                        }

                        break;
                    }
                    default:
                        if (_configuration.GetDefaultValue("load.autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM smart_scripts WHERE entryorguid = {temp.EntryOrGuid}");
                        else
                            Log.Logger.Error("SmartAIMgr.LoadFromDB: not yet implemented source_type {0}", sourceType);

                        continue;
                }
            else
                switch (sourceType)
                {
                    case SmartScriptType.Creature:
                    {
                        var creature = _gameObjectManager.GetCreatureData((ulong)-temp.EntryOrGuid);

                        if (creature == null)
                        {
                            if (_configuration.GetDefaultValue("load.autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM smart_scripts WHERE entryorguid = {temp.EntryOrGuid}");
                            else
                                Log.Logger.Error($"SmartAIMgr.LoadFromDB: Creature guid ({-temp.EntryOrGuid}) does not exist, skipped loading.");

                            continue;
                        }

                        var creatureInfo = _gameObjectManager.GetCreatureTemplate(creature.Id);

                        if (creatureInfo == null)
                        {
                            if (_configuration.GetDefaultValue("load.autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM smart_scripts WHERE entryorguid = {temp.EntryOrGuid}");
                            else
                                Log.Logger.Error($"SmartAIMgr.LoadFromDB: Creature entry ({creature.Id}) guid ({-temp.EntryOrGuid}) does not exist, skipped loading.");

                            continue;
                        }

                        if (creatureInfo.AIName != "SmartAI")
                        {
                            if (_configuration.GetDefaultValue("load.autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM smart_scripts WHERE entryorguid = {temp.EntryOrGuid}");
                            else
                                Log.Logger.Error($"SmartAIMgr.LoadFromDB: Creature entry ({creature.Id}) guid ({-temp.EntryOrGuid}) is not using SmartAI, skipped loading.");

                            continue;
                        }

                        break;
                    }
                    case SmartScriptType.GameObject:
                    {
                        var gameObject = _gameObjectManager.GetGameObjectData((ulong)-temp.EntryOrGuid);

                        if (gameObject == null)
                        {
                            if (_configuration.GetDefaultValue("load.autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM smart_scripts WHERE entryorguid = {temp.EntryOrGuid}");
                            else
                                Log.Logger.Error($"SmartAIMgr.LoadFromDB: GameObject guid ({-temp.EntryOrGuid}) does not exist, skipped loading.");

                            continue;
                        }

                        var gameObjectInfo = _gameObjectManager.GetGameObjectTemplate(gameObject.Id);

                        if (gameObjectInfo == null)
                        {
                            if (_configuration.GetDefaultValue("load.autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM smart_scripts WHERE entryorguid = {temp.EntryOrGuid}");
                            else
                                Log.Logger.Error($"SmartAIMgr.LoadFromDB: GameObject entry ({gameObject.Id}) guid ({-temp.EntryOrGuid}) does not exist, skipped loading.");

                            continue;
                        }

                        if (gameObjectInfo.AIName != "SmartGameObjectAI")
                        {
                            if (_configuration.GetDefaultValue("load.autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM smart_scripts WHERE entryorguid = {temp.EntryOrGuid}");
                            else
                                Log.Logger.Error($"SmartAIMgr.LoadFromDB: GameObject entry ({gameObject.Id}) guid ({-temp.EntryOrGuid}) is not using SmartGameObjectAI, skipped loading.");

                            continue;
                        }

                        break;
                    }
                    default:
                        if (_configuration.GetDefaultValue("load.autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM smart_scripts WHERE entryorguid = {temp.EntryOrGuid}");
                        else
                            Log.Logger.Error($"SmartAIMgr.LoadFromDB: GUID-specific scripting not yet implemented for source_type {sourceType}");

                        continue;
                }

            temp.SourceType = sourceType;
            temp.EventId = result.Read<ushort>(2);
            temp.Link = result.Read<ushort>(3);
            temp.Event.type = (SmartEvents)result.Read<byte>(4);
            temp.Event.event_phase_mask = result.Read<ushort>(5);
            temp.Event.event_chance = result.Read<byte>(6);
            temp.Event.event_flags = (SmartEventFlags)result.Read<ushort>(7);

            temp.Event.raw.Param1 = result.Read<uint>(8);
            temp.Event.raw.Param2 = result.Read<uint>(9);
            temp.Event.raw.Param3 = result.Read<uint>(10);
            temp.Event.raw.Param4 = result.Read<uint>(11);
            temp.Event.raw.Param5 = result.Read<uint>(12);
            temp.Event.param_string = result.Read<string>(13);

            temp.Action.type = (SmartActions)result.Read<byte>(14);
            temp.Action.raw.Param1 = result.Read<uint>(15);
            temp.Action.raw.Param2 = result.Read<uint>(16);
            temp.Action.raw.Param3 = result.Read<uint>(17);
            temp.Action.raw.Param4 = result.Read<uint>(18);
            temp.Action.raw.Param5 = result.Read<uint>(19);
            temp.Action.raw.Param6 = result.Read<uint>(20);
            temp.Action.raw.Param7 = result.Read<uint>(21);

            temp.Target.type = (SmartTargets)result.Read<byte>(22);
            temp.Target.raw.Param1 = result.Read<uint>(23);
            temp.Target.raw.Param2 = result.Read<uint>(24);
            temp.Target.raw.Param3 = result.Read<uint>(25);
            temp.Target.raw.Param4 = result.Read<uint>(26);
            temp.Target.x = result.Read<float>(27);
            temp.Target.y = result.Read<float>(28);
            temp.Target.z = result.Read<float>(29);
            temp.Target.o = result.Read<float>(30);

            //check target
            if (!IsTargetValid(temp))
                continue;

            // check all event and action params
            if (!IsEventValid(temp))
                continue;

            // specific check for timed events
            switch (temp.Event.type)
            {
                case SmartEvents.Update:
                case SmartEvents.UpdateOoc:
                case SmartEvents.UpdateIc:
                case SmartEvents.HealthPct:
                case SmartEvents.ManaPct:
                case SmartEvents.Range:
                case SmartEvents.FriendlyHealthPCT:
                case SmartEvents.FriendlyMissingBuff:
                case SmartEvents.HasAura:
                case SmartEvents.TargetBuffed:
                    if (temp.Event.minMaxRepeat is { RepeatMin: 0, RepeatMax: 0 } && !temp.Event.event_flags.HasAnyFlag(SmartEventFlags.NotRepeatable) && temp.SourceType != SmartScriptType.TimedActionlist)
                    {
                        temp.Event.event_flags |= SmartEventFlags.NotRepeatable;
                        Log.Logger.Error($"SmartAIMgr.LoadFromDB: Entry {temp.EntryOrGuid} SourceType {temp.GetScriptType()}, Event {temp.EventId}, Missing Repeat Id.");
                    }

                    break;

                case SmartEvents.VictimCasting:
                case SmartEvents.IsBehindTarget:
                    if (temp.Event.minMaxRepeat is { Min: 0, Max: 0 } && !temp.Event.event_flags.HasAnyFlag(SmartEventFlags.NotRepeatable) && temp.SourceType != SmartScriptType.TimedActionlist)
                    {
                        temp.Event.event_flags |= SmartEventFlags.NotRepeatable;
                        Log.Logger.Error($"SmartAIMgr.LoadFromDB: Entry {temp.EntryOrGuid} SourceType {temp.GetScriptType()}, Event {temp.EventId}, Missing Repeat Id.");
                    }

                    break;

                case SmartEvents.FriendlyIsCc:
                    if (temp.Event.friendlyCC is { RepeatMin: 0, RepeatMax: 0 } && !temp.Event.event_flags.HasAnyFlag(SmartEventFlags.NotRepeatable) && temp.SourceType != SmartScriptType.TimedActionlist)
                    {
                        temp.Event.event_flags |= SmartEventFlags.NotRepeatable;
                        Log.Logger.Error($"SmartAIMgr.LoadFromDB: Entry {temp.EntryOrGuid} SourceType {temp.GetScriptType()}, Event {temp.EventId}, Missing Repeat Id.");
                    }

                    break;
            }

            // creature entry / guid not found in storage, create empty event list for it and increase counters
            if (!_eventMap[(int)sourceType].ContainsKey(temp.EntryOrGuid))
                ++count;

            // store the new event
            _eventMap[(int)sourceType].Add(temp.EntryOrGuid, temp);
        } while (result.NextRow());

        // Post Loading Validation
        for (byte i = 0; i < (int)SmartScriptType.Max; ++i)
        {
            if (_eventMap[i] == null)
                continue;

            foreach (var key in _eventMap[i].Keys)
            {
                var list = _eventMap[i].LookupByKey(key);

                foreach (var e in list)
                {
                    if (e.Link != 0)
                        if (FindLinkedEvent(list, e.Link) == null)
                            Log.Logger.Error("SmartAIMgr.LoadFromDB: Entry {0} SourceType {1}, Event {2}, Link Event {3} not found or invalid.",
                                             e.EntryOrGuid,
                                             e.GetScriptType(),
                                             e.EventId,
                                             e.Link);

                    if (e.GetEventType() == SmartEvents.Link)
                        if (FindLinkedSourceEvent(list, e.EventId) == null)
                            Log.Logger.Error("SmartAIMgr.LoadFromDB: Entry {0} SourceType {1}, Event {2}, Link Source Event not found or invalid. Event will never trigger.",
                                             e.EntryOrGuid,
                                             e.GetScriptType(),
                                             e.EventId);
                }
            }
        }

        Log.Logger.Information("Loaded {0} SmartAI scripts in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadWaypointFromDB()
    {
        var oldMSTime = Time.MSTime;

        _waypointStore.Clear();

        var stmt = _worldDatabase.GetPreparedStatement(WorldStatements.SEL_SMARTAI_WP);
        var result = _worldDatabase.Query(stmt);

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 SmartAI Waypoint Paths. DB table `waypoints` is empty.");

            return;
        }

        uint count = 0;
        uint total = 0;
        uint lastEntry = 0;
        uint lastId = 1;

        do
        {
            var entry = result.Read<uint>(0);
            var id = result.Read<uint>(1);
            var x = result.Read<float>(2);
            var y = result.Read<float>(3);
            var z = result.Read<float>(4);
            float? o = null;

            if (!result.IsNull(5))
                o = result.Read<float>(5);

            var delay = result.Read<uint>(6);

            if (lastEntry != entry)
            {
                lastId = 1;
                ++count;
            }

            if (lastId != id)
                Log.Logger.Error($"SmartWaypointMgr.LoadFromDB: Path entry {entry}, unexpected point id {id}, expected {lastId}.");

            ++lastId;

            if (!_waypointStore.ContainsKey(entry))
                _waypointStore[entry] = new WaypointPath();

            var path = _waypointStore[entry];
            path.ID = entry;
            path.Nodes.Add(new WaypointNode(id, x, y, z, o, delay));

            lastEntry = entry;
            ++total;
        } while (result.NextRow());

        Log.Logger.Information($"Loaded {count} SmartAI waypoint paths (total {total} waypoints) in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }

    private static bool CheckUnusedActionParams(SmartScriptHolder e)
    {
        var paramsStructSize = e.Action.type switch
        {
            SmartActions.None => 0,
            SmartActions.Talk => Marshal.SizeOf(typeof(SmartAction.Talk)),
            SmartActions.SetFaction => Marshal.SizeOf(typeof(SmartAction.Faction)),
            SmartActions.MorphToEntryOrModel => Marshal.SizeOf(typeof(SmartAction.MorphOrMount)),
            SmartActions.Sound => Marshal.SizeOf(typeof(SmartAction.Sound)),
            SmartActions.PlayEmote => Marshal.SizeOf(typeof(SmartAction.Emote)),
            SmartActions.FailQuest => Marshal.SizeOf(typeof(SmartAction.Quest)),
            SmartActions.OfferQuest => Marshal.SizeOf(typeof(SmartAction.QuestOffer)),
            SmartActions.SetReactState => Marshal.SizeOf(typeof(SmartAction.React)),
            SmartActions.ActivateGobject => 0,
            SmartActions.RandomEmote => Marshal.SizeOf(typeof(SmartAction.RandomEmote)),
            SmartActions.Cast => Marshal.SizeOf(typeof(SmartAction.Cast)),
            SmartActions.SummonCreature => Marshal.SizeOf(typeof(SmartAction.SummonCreature)),
            SmartActions.ThreatSinglePct => Marshal.SizeOf(typeof(SmartAction.ThreatPct)),
            SmartActions.ThreatAllPct => Marshal.SizeOf(typeof(SmartAction.ThreatPct)),
            SmartActions.CallAreaexploredoreventhappens => Marshal.SizeOf(typeof(SmartAction.Quest)),
            SmartActions.SetIngamePhaseGroup => Marshal.SizeOf(typeof(SmartAction.IngamePhaseGroup)),
            SmartActions.SetEmoteState => Marshal.SizeOf(typeof(SmartAction.Emote)),
            SmartActions.AutoAttack => Marshal.SizeOf(typeof(SmartAction.AutoAttack)),
            SmartActions.AllowCombatMovement => Marshal.SizeOf(typeof(SmartAction.CombatMove)),
            SmartActions.SetEventPhase => Marshal.SizeOf(typeof(SmartAction.SetEventPhase)),
            SmartActions.IncEventPhase => Marshal.SizeOf(typeof(SmartAction.IncEventPhase)),
            SmartActions.Evade => Marshal.SizeOf(typeof(SmartAction.Evade)),
            SmartActions.FleeForAssist => Marshal.SizeOf(typeof(SmartAction.FleeAssist)),
            SmartActions.CallGroupeventhappens => Marshal.SizeOf(typeof(SmartAction.Quest)),
            SmartActions.CombatStop => 0,
            SmartActions.RemoveAurasFromSpell => Marshal.SizeOf(typeof(SmartAction.RemoveAura)),
            SmartActions.Follow => Marshal.SizeOf(typeof(SmartAction.Follow)),
            SmartActions.RandomPhase => Marshal.SizeOf(typeof(SmartAction.RandomPhase)),
            SmartActions.RandomPhaseRange => Marshal.SizeOf(typeof(SmartAction.RandomPhaseRange)),
            SmartActions.ResetGobject => 0,
            SmartActions.CallKilledmonster => Marshal.SizeOf(typeof(SmartAction.KilledMonster)),
            SmartActions.SetInstData => Marshal.SizeOf(typeof(SmartAction.SetInstanceData)),
            SmartActions.SetInstData64 => Marshal.SizeOf(typeof(SmartAction.SetInstanceData64)),
            SmartActions.UpdateTemplate => Marshal.SizeOf(typeof(SmartAction.UpdateTemplate)),
            SmartActions.Die => 0,
            SmartActions.SetInCombatWithZone => 0,
            SmartActions.CallForHelp => Marshal.SizeOf(typeof(SmartAction.CallHelp)),
            SmartActions.SetSheath => Marshal.SizeOf(typeof(SmartAction.SetSheath)),
            SmartActions.ForceDespawn => Marshal.SizeOf(typeof(SmartAction.ForceDespawn)),
            SmartActions.SetInvincibilityHpLevel => Marshal.SizeOf(typeof(SmartAction.InvincHp)),
            SmartActions.MountToEntryOrModel => Marshal.SizeOf(typeof(SmartAction.MorphOrMount)),
            SmartActions.SetIngamePhaseId => Marshal.SizeOf(typeof(SmartAction.IngamePhaseId)),
            SmartActions.SetData => Marshal.SizeOf(typeof(SmartAction.SetData)),
            SmartActions.AttackStop => 0,
            SmartActions.SetVisibility => Marshal.SizeOf(typeof(SmartAction.Visibility)),
            SmartActions.SetActive => Marshal.SizeOf(typeof(SmartAction.Active)),
            SmartActions.AttackStart => 0,
            SmartActions.SummonGo => Marshal.SizeOf(typeof(SmartAction.SummonGO)),
            SmartActions.KillUnit => 0,
            SmartActions.ActivateTaxi => Marshal.SizeOf(typeof(SmartAction.Taxi)),
            SmartActions.WpStart => Marshal.SizeOf(typeof(SmartAction.WpStart)),
            SmartActions.WpPause => Marshal.SizeOf(typeof(SmartAction.WpPause)),
            SmartActions.WpStop => Marshal.SizeOf(typeof(SmartAction.WpStop)),
            SmartActions.AddItem => Marshal.SizeOf(typeof(SmartAction.Item)),
            SmartActions.RemoveItem => Marshal.SizeOf(typeof(SmartAction.Item)),
            SmartActions.SetRun => Marshal.SizeOf(typeof(SmartAction.SetRun)),
            SmartActions.SetDisableGravity => Marshal.SizeOf(typeof(SmartAction.SetDisableGravity)),
            SmartActions.Teleport => Marshal.SizeOf(typeof(SmartAction.Teleport)),
            SmartActions.SetCounter => Marshal.SizeOf(typeof(SmartAction.SetCounter)),
            SmartActions.StoreTargetList => Marshal.SizeOf(typeof(SmartAction.StoreTargets)),
            SmartActions.WpResume => 0,
            SmartActions.SetOrientation => 0,
            SmartActions.CreateTimedEvent => Marshal.SizeOf(typeof(SmartAction.TimeEvent)),
            SmartActions.Playmovie => Marshal.SizeOf(typeof(SmartAction.Movie)),
            SmartActions.MoveToPos => Marshal.SizeOf(typeof(SmartAction.MoveToPos)),
            SmartActions.EnableTempGobj => Marshal.SizeOf(typeof(SmartAction.EnableTempGO)),
            SmartActions.Equip => Marshal.SizeOf(typeof(SmartAction.Equip)),
            SmartActions.CloseGossip => 0,
            SmartActions.TriggerTimedEvent => Marshal.SizeOf(typeof(SmartAction.TimeEvent)),
            SmartActions.RemoveTimedEvent => Marshal.SizeOf(typeof(SmartAction.TimeEvent)),
            SmartActions.CallScriptReset => 0,
            SmartActions.SetRangedMovement => Marshal.SizeOf(typeof(SmartAction.SetRangedMovement)),
            SmartActions.CallTimedActionlist => Marshal.SizeOf(typeof(SmartAction.TimedActionList)),
            SmartActions.SetNpcFlag => Marshal.SizeOf(typeof(SmartAction.Flag)),
            SmartActions.AddNpcFlag => Marshal.SizeOf(typeof(SmartAction.Flag)),
            SmartActions.RemoveNpcFlag => Marshal.SizeOf(typeof(SmartAction.Flag)),
            SmartActions.SimpleTalk => Marshal.SizeOf(typeof(SmartAction.SimpleTalk)),
            SmartActions.SelfCast => Marshal.SizeOf(typeof(SmartAction.Cast)),
            SmartActions.CrossCast => Marshal.SizeOf(typeof(SmartAction.CrossCast)),
            SmartActions.CallRandomTimedActionlist => Marshal.SizeOf(typeof(SmartAction.RandTimedActionList)),
            SmartActions.CallRandomRangeTimedActionlist => Marshal.SizeOf(typeof(SmartAction.RandRangeTimedActionList)),
            SmartActions.RandomMove => Marshal.SizeOf(typeof(SmartAction.MoveRandom)),
            SmartActions.SetUnitFieldBytes1 => Marshal.SizeOf(typeof(SmartAction.SetunitByte)),
            SmartActions.RemoveUnitFieldBytes1 => Marshal.SizeOf(typeof(SmartAction.DelunitByte)),
            SmartActions.InterruptSpell => Marshal.SizeOf(typeof(SmartAction.InterruptSpellCasting)),
            SmartActions.AddDynamicFlag => Marshal.SizeOf(typeof(SmartAction.Flag)),
            SmartActions.RemoveDynamicFlag => Marshal.SizeOf(typeof(SmartAction.Flag)),
            SmartActions.JumpToPos => Marshal.SizeOf(typeof(SmartAction.Jump)),
            SmartActions.SendGossipMenu => Marshal.SizeOf(typeof(SmartAction.SendGossipMenu)),
            SmartActions.GoSetLootState => Marshal.SizeOf(typeof(SmartAction.SetGoLootState)),
            SmartActions.SendTargetToTarget => Marshal.SizeOf(typeof(SmartAction.SendTargetToTarget)),
            SmartActions.SetHomePos => 0,
            SmartActions.SetHealthRegen => Marshal.SizeOf(typeof(SmartAction.SetHealthRegen)),
            SmartActions.SetRoot => Marshal.SizeOf(typeof(SmartAction.SetRoot)),
            SmartActions.SummonCreatureGroup => Marshal.SizeOf(typeof(SmartAction.CreatureGroup)),
            SmartActions.SetPower => Marshal.SizeOf(typeof(SmartAction.Power)),
            SmartActions.AddPower => Marshal.SizeOf(typeof(SmartAction.Power)),
            SmartActions.RemovePower => Marshal.SizeOf(typeof(SmartAction.Power)),
            SmartActions.GameEventStop => Marshal.SizeOf(typeof(SmartAction.GameEventStop)),
            SmartActions.GameEventStart => Marshal.SizeOf(typeof(SmartAction.GameEventStart)),
            SmartActions.StartClosestWaypoint => Marshal.SizeOf(typeof(SmartAction.ClosestWaypointFromList)),
            SmartActions.MoveOffset => Marshal.SizeOf(typeof(SmartAction.MoveOffset)),
            SmartActions.RandomSound => Marshal.SizeOf(typeof(SmartAction.RandomSound)),
            SmartActions.SetCorpseDelay => Marshal.SizeOf(typeof(SmartAction.CorpseDelay)),
            SmartActions.DisableEvade => Marshal.SizeOf(typeof(SmartAction.DisableEvade)),
            SmartActions.GoSetGoState => Marshal.SizeOf(typeof(SmartAction.GoState)),
            SmartActions.AddThreat => Marshal.SizeOf(typeof(SmartAction.Threat)),
            SmartActions.LoadEquipment => Marshal.SizeOf(typeof(SmartAction.LoadEquipment)),
            SmartActions.TriggerRandomTimedEvent => Marshal.SizeOf(typeof(SmartAction.RandomTimedEvent)),
            SmartActions.PauseMovement => Marshal.SizeOf(typeof(SmartAction.PauseMovement)),
            SmartActions.PlayAnimkit => Marshal.SizeOf(typeof(SmartAction.AnimKit)),
            SmartActions.ScenePlay => Marshal.SizeOf(typeof(SmartAction.Scene)),
            SmartActions.SceneCancel => Marshal.SizeOf(typeof(SmartAction.Scene)),
            SmartActions.SpawnSpawngroup => Marshal.SizeOf(typeof(SmartAction.GroupSpawn)),
            SmartActions.DespawnSpawngroup => Marshal.SizeOf(typeof(SmartAction.GroupSpawn)),
            SmartActions.RespawnBySpawnId => Marshal.SizeOf(typeof(SmartAction.RespawnData)),
            SmartActions.InvokerCast => Marshal.SizeOf(typeof(SmartAction.Cast)),
            SmartActions.PlayCinematic => Marshal.SizeOf(typeof(SmartAction.Cinematic)),
            SmartActions.SetMovementSpeed => Marshal.SizeOf(typeof(SmartAction.MovementSpeed)),
            SmartActions.PlaySpellVisualKit => Marshal.SizeOf(typeof(SmartAction.SpellVisualKit)),
            SmartActions.OverrideLight => Marshal.SizeOf(typeof(SmartAction.OverrideLight)),
            SmartActions.OverrideWeather => Marshal.SizeOf(typeof(SmartAction.OverrideWeather)),
            SmartActions.SetAIAnimKit => 0,
            SmartActions.SetHover => Marshal.SizeOf(typeof(SmartAction.SetHover)),
            SmartActions.SetHealthPct => Marshal.SizeOf(typeof(SmartAction.SetHealthPct)),
            SmartActions.CreateConversation => Marshal.SizeOf(typeof(SmartAction.Conversation)),
            SmartActions.SetImmunePC => Marshal.SizeOf(typeof(SmartAction.SetImmunePc)),
            SmartActions.SetImmuneNPC => Marshal.SizeOf(typeof(SmartAction.SetImmuneNPC)),
            SmartActions.SetUninteractible => Marshal.SizeOf(typeof(SmartAction.SetUninteractible)),
            SmartActions.ActivateGameobject => Marshal.SizeOf(typeof(SmartAction.ActivateGameObject)),
            SmartActions.AddToStoredTargetList => Marshal.SizeOf(typeof(SmartAction.AddToStoredTargets)),
            SmartActions.BecomePersonalCloneForPlayer => Marshal.SizeOf(typeof(SmartAction.BecomePersonalClone)),
            SmartActions.TriggerGameEvent => Marshal.SizeOf(typeof(SmartAction.TriggerGameEvent)),
            SmartActions.DoAction => Marshal.SizeOf(typeof(SmartAction.DoAction)),
            _ => Marshal.SizeOf(typeof(SmartAction.Raw)),
        };

        var rawCount = Marshal.SizeOf(typeof(SmartAction.Raw)) / sizeof(uint);
        var paramsCount = paramsStructSize / sizeof(uint);

        for (var index = paramsCount; index < rawCount; index++)
        {
            uint value = index switch
            {
                0 => e.Action.raw.Param1,
                1 => e.Action.raw.Param2,
                2 => e.Action.raw.Param3,
                3 => e.Action.raw.Param4,
                4 => e.Action.raw.Param5,
                5 => e.Action.raw.Param6,
                _ => 0
            };

            if (value != 0)
                Log.Logger.Warning($"SmartAIMgr: {e} has unused action_param{index + 1} with value {value}, it should be 0.");
        }

        return true;
    }

    private static bool CheckUnusedEventParams(SmartScriptHolder e)
    {
        var paramsStructSize = e.Event.type switch
        {
            SmartEvents.UpdateIc => Marshal.SizeOf(typeof(SmartEvent.MinMaxRepeat)),
            SmartEvents.UpdateOoc => Marshal.SizeOf(typeof(SmartEvent.MinMaxRepeat)),
            SmartEvents.HealthPct => Marshal.SizeOf(typeof(SmartEvent.MinMaxRepeat)),
            SmartEvents.ManaPct => Marshal.SizeOf(typeof(SmartEvent.MinMaxRepeat)),
            SmartEvents.Aggro => 0,
            SmartEvents.Kill => Marshal.SizeOf(typeof(SmartEvent.Kill)),
            SmartEvents.Death => 0,
            SmartEvents.Evade => 0,
            SmartEvents.SpellHit => Marshal.SizeOf(typeof(SmartEvent.SpellHit)),
            SmartEvents.Range => Marshal.SizeOf(typeof(SmartEvent.MinMaxRepeat)),
            SmartEvents.OocLos => Marshal.SizeOf(typeof(SmartEvent.Los)),
            SmartEvents.Respawn => Marshal.SizeOf(typeof(SmartEvent.Respawn)),
            SmartEvents.VictimCasting => Marshal.SizeOf(typeof(SmartEvent.TargetCasting)),
            SmartEvents.FriendlyIsCc => Marshal.SizeOf(typeof(SmartEvent.FriendlyCc)),
            SmartEvents.FriendlyMissingBuff => Marshal.SizeOf(typeof(SmartEvent.MissingBuff)),
            SmartEvents.SummonedUnit => Marshal.SizeOf(typeof(SmartEvent.Summoned)),
            SmartEvents.AcceptedQuest => Marshal.SizeOf(typeof(SmartEvent.Quest)),
            SmartEvents.RewardQuest => Marshal.SizeOf(typeof(SmartEvent.Quest)),
            SmartEvents.ReachedHome => 0,
            SmartEvents.ReceiveEmote => Marshal.SizeOf(typeof(SmartEvent.Emote)),
            SmartEvents.HasAura => Marshal.SizeOf(typeof(SmartEvent.Aura)),
            SmartEvents.TargetBuffed => Marshal.SizeOf(typeof(SmartEvent.Aura)),
            SmartEvents.Reset => 0,
            SmartEvents.IcLos => Marshal.SizeOf(typeof(SmartEvent.Los)),
            SmartEvents.PassengerBoarded => Marshal.SizeOf(typeof(SmartEvent.MinMax)),
            SmartEvents.PassengerRemoved => Marshal.SizeOf(typeof(SmartEvent.MinMax)),
            SmartEvents.Charmed => Marshal.SizeOf(typeof(SmartEvent.Charm)),
            SmartEvents.SpellHitTarget => Marshal.SizeOf(typeof(SmartEvent.SpellHit)),
            SmartEvents.Damaged => Marshal.SizeOf(typeof(SmartEvent.MinMaxRepeat)),
            SmartEvents.DamagedTarget => Marshal.SizeOf(typeof(SmartEvent.MinMaxRepeat)),
            SmartEvents.Movementinform => Marshal.SizeOf(typeof(SmartEvent.MovementInform)),
            SmartEvents.SummonDespawned => Marshal.SizeOf(typeof(SmartEvent.Summoned)),
            SmartEvents.CorpseRemoved => 0,
            SmartEvents.AiInit => 0,
            SmartEvents.DataSet => Marshal.SizeOf(typeof(SmartEvent.DataSet)),
            SmartEvents.WaypointReached => Marshal.SizeOf(typeof(SmartEvent.Waypoint)),
            SmartEvents.TransportAddplayer => 0,
            SmartEvents.TransportAddcreature => Marshal.SizeOf(typeof(SmartEvent.TransportAddCreature)),
            SmartEvents.TransportRemovePlayer => 0,
            SmartEvents.TransportRelocate => Marshal.SizeOf(typeof(SmartEvent.TransportRelocate)),
            SmartEvents.InstancePlayerEnter => Marshal.SizeOf(typeof(SmartEvent.InstancePlayerEnter)),
            SmartEvents.AreatriggerOntrigger => Marshal.SizeOf(typeof(SmartEvent.Areatrigger)),
            SmartEvents.QuestAccepted => 0,
            SmartEvents.QuestObjCompletion => 0,
            SmartEvents.QuestCompletion => 0,
            SmartEvents.QuestRewarded => 0,
            SmartEvents.QuestFail => 0,
            SmartEvents.TextOver => Marshal.SizeOf(typeof(SmartEvent.TextOver)),
            SmartEvents.ReceiveHeal => Marshal.SizeOf(typeof(SmartEvent.MinMaxRepeat)),
            SmartEvents.JustSummoned => 0,
            SmartEvents.WaypointPaused => Marshal.SizeOf(typeof(SmartEvent.Waypoint)),
            SmartEvents.WaypointResumed => Marshal.SizeOf(typeof(SmartEvent.Waypoint)),
            SmartEvents.WaypointStopped => Marshal.SizeOf(typeof(SmartEvent.Waypoint)),
            SmartEvents.WaypointEnded => Marshal.SizeOf(typeof(SmartEvent.Waypoint)),
            SmartEvents.TimedEventTriggered => Marshal.SizeOf(typeof(SmartEvent.TimedEvent)),
            SmartEvents.Update => Marshal.SizeOf(typeof(SmartEvent.MinMaxRepeat)),
            SmartEvents.Link => 0,
            SmartEvents.GossipSelect => Marshal.SizeOf(typeof(SmartEvent.Gossip)),
            SmartEvents.JustCreated => 0,
            SmartEvents.GossipHello => Marshal.SizeOf(typeof(SmartEvent.GossipHello)),
            SmartEvents.FollowCompleted => 0,
            SmartEvents.GameEventStart => Marshal.SizeOf(typeof(SmartEvent.GameEvent)),
            SmartEvents.GameEventEnd => Marshal.SizeOf(typeof(SmartEvent.GameEvent)),
            SmartEvents.GoLootStateChanged => Marshal.SizeOf(typeof(SmartEvent.GoLootStateChanged)),
            SmartEvents.GoEventInform => Marshal.SizeOf(typeof(SmartEvent.EventInform)),
            SmartEvents.ActionDone => Marshal.SizeOf(typeof(SmartEvent.DoAction)),
            SmartEvents.OnSpellclick => 0,
            SmartEvents.FriendlyHealthPCT => Marshal.SizeOf(typeof(SmartEvent.FriendlyHealthPct)),
            SmartEvents.DistanceCreature => Marshal.SizeOf(typeof(SmartEvent.Distance)),
            SmartEvents.DistanceGameobject => Marshal.SizeOf(typeof(SmartEvent.Distance)),
            SmartEvents.CounterSet => Marshal.SizeOf(typeof(SmartEvent.Counter)),
            SmartEvents.SceneStart => 0,
            SmartEvents.SceneTrigger => 0,
            SmartEvents.SceneCancel => 0,
            SmartEvents.SceneComplete => 0,
            SmartEvents.SummonedUnitDies => Marshal.SizeOf(typeof(SmartEvent.Summoned)),
            SmartEvents.OnSpellCast => Marshal.SizeOf(typeof(SmartEvent.SpellCast)),
            SmartEvents.OnSpellFailed => Marshal.SizeOf(typeof(SmartEvent.SpellCast)),
            SmartEvents.OnSpellStart => Marshal.SizeOf(typeof(SmartEvent.SpellCast)),
            SmartEvents.OnDespawn => 0,
            _ => Marshal.SizeOf(typeof(SmartEvent.Raw)),
        };

        var rawCount = Marshal.SizeOf(typeof(SmartEvent.Raw)) / sizeof(uint);
        var paramsCount = paramsStructSize / sizeof(uint);

        for (var index = paramsCount; index < rawCount; index++)
        {
            uint value = index switch
            {
                0 => e.Event.raw.Param1,
                1 => e.Event.raw.Param2,
                2 => e.Event.raw.Param3,
                3 => e.Event.raw.Param4,
                4 => e.Event.raw.Param5,
                _ => 0
            };

            if (value != 0)
                Log.Logger.Warning($"SmartAIMgr: {e} has unused event_param{index + 1} with value {value}, it should be 0.");
        }

        return true;
    }

    private static bool CheckUnusedTargetParams(SmartScriptHolder e)
    {
        var paramsStructSize = e.Target.type switch
        {
            SmartTargets.None => 0,
            SmartTargets.Self => 0,
            SmartTargets.Victim => 0,
            SmartTargets.HostileSecondAggro => Marshal.SizeOf(typeof(SmartTarget.HostilRandom)),
            SmartTargets.HostileLastAggro => Marshal.SizeOf(typeof(SmartTarget.HostilRandom)),
            SmartTargets.HostileRandom => Marshal.SizeOf(typeof(SmartTarget.HostilRandom)),
            SmartTargets.HostileRandomNotTop => Marshal.SizeOf(typeof(SmartTarget.HostilRandom)),
            SmartTargets.ActionInvoker => 0,
            SmartTargets.Position => 0, //Uses X,Y,Z,O
            SmartTargets.CreatureRange => Marshal.SizeOf(typeof(SmartTarget.UnitRange)),
            SmartTargets.CreatureGuid => Marshal.SizeOf(typeof(SmartTarget.UnitGUID)),
            SmartTargets.CreatureDistance => Marshal.SizeOf(typeof(SmartTarget.UnitDistance)),
            SmartTargets.Stored => Marshal.SizeOf(typeof(SmartTarget.Stored)),
            SmartTargets.GameobjectRange => Marshal.SizeOf(typeof(SmartTarget.GoRange)),
            SmartTargets.GameobjectGuid => Marshal.SizeOf(typeof(SmartTarget.GoGUID)),
            SmartTargets.GameobjectDistance => Marshal.SizeOf(typeof(SmartTarget.GoDistance)),
            SmartTargets.InvokerParty => 0,
            SmartTargets.PlayerRange => Marshal.SizeOf(typeof(SmartTarget.PlayerRange)),
            SmartTargets.PlayerDistance => Marshal.SizeOf(typeof(SmartTarget.PlayerDistance)),
            SmartTargets.ClosestCreature => Marshal.SizeOf(typeof(SmartTarget.UnitClosest)),
            SmartTargets.ClosestGameobject => Marshal.SizeOf(typeof(SmartTarget.GoClosest)),
            SmartTargets.ClosestPlayer => Marshal.SizeOf(typeof(SmartTarget.PlayerDistance)),
            SmartTargets.ActionInvokerVehicle => 0,
            SmartTargets.OwnerOrSummoner => Marshal.SizeOf(typeof(SmartTarget.Owner)),
            SmartTargets.ThreatList => Marshal.SizeOf(typeof(SmartTarget.ThreatList)),
            SmartTargets.ClosestEnemy => Marshal.SizeOf(typeof(SmartTarget.ClosestAttackable)),
            SmartTargets.ClosestFriendly => Marshal.SizeOf(typeof(SmartTarget.ClosestFriendly)),
            SmartTargets.LootRecipients => 0,
            SmartTargets.Farthest => Marshal.SizeOf(typeof(SmartTarget.Farthest)),
            SmartTargets.VehiclePassenger => Marshal.SizeOf(typeof(SmartTarget.Vehicle)),
            SmartTargets.ClosestUnspawnedGameobject => Marshal.SizeOf(typeof(SmartTarget.GoClosest)),
            _ => Marshal.SizeOf(typeof(SmartTarget.Raw)),
        };

        var rawCount = Marshal.SizeOf(typeof(SmartTarget.Raw)) / sizeof(uint);
        var paramsCount = paramsStructSize / sizeof(uint);

        for (var index = paramsCount; index < rawCount; index++)
        {
            uint value = index switch
            {
                0 => e.Target.raw.Param1,
                1 => e.Target.raw.Param2,
                2 => e.Target.raw.Param3,
                3 => e.Target.raw.Param4,
                _ => 0
            };

            if (value != 0)
                Log.Logger.Warning($"SmartAIMgr: {e} has unused target_param{index + 1} with value {value}, it must be 0, skipped.");
        }

        return true;
    }

    private static bool EventHasInvoker(SmartEvents smartEvent)
    {
        return smartEvent switch
        {
            // white list of events that actually have an invoker passed to them
            SmartEvents.Aggro                => true,
            SmartEvents.Death                => true,
            SmartEvents.Kill                 => true,
            SmartEvents.SummonedUnit         => true,
            SmartEvents.SummonedUnitDies     => true,
            SmartEvents.SpellHit             => true,
            SmartEvents.SpellHitTarget       => true,
            SmartEvents.Damaged              => true,
            SmartEvents.ReceiveHeal          => true,
            SmartEvents.ReceiveEmote         => true,
            SmartEvents.JustSummoned         => true,
            SmartEvents.DamagedTarget        => true,
            SmartEvents.SummonDespawned      => true,
            SmartEvents.PassengerBoarded     => true,
            SmartEvents.PassengerRemoved     => true,
            SmartEvents.GossipHello          => true,
            SmartEvents.GossipSelect         => true,
            SmartEvents.AcceptedQuest        => true,
            SmartEvents.RewardQuest          => true,
            SmartEvents.FollowCompleted      => true,
            SmartEvents.OnSpellclick         => true,
            SmartEvents.GoLootStateChanged   => true,
            SmartEvents.AreatriggerOntrigger => true,
            SmartEvents.IcLos                => true,
            SmartEvents.OocLos               => true,
            SmartEvents.DistanceCreature     => true,
            SmartEvents.FriendlyHealthPCT    => true,
            SmartEvents.FriendlyIsCc         => true,
            SmartEvents.FriendlyMissingBuff  => true,
            SmartEvents.ActionDone           => true,
            SmartEvents.Range                => true,
            SmartEvents.VictimCasting        => true,
            SmartEvents.TargetBuffed         => true,
            SmartEvents.InstancePlayerEnter  => true,
            SmartEvents.TransportAddcreature => true,
            SmartEvents.DataSet              => true,
            SmartEvents.QuestAccepted        => true,
            SmartEvents.QuestObjCompletion   => true,
            SmartEvents.QuestCompletion      => true,
            SmartEvents.QuestFail            => true,
            SmartEvents.QuestRewarded        => true,
            SmartEvents.SceneStart           => true,
            SmartEvents.SceneTrigger         => true,
            SmartEvents.SceneCancel          => true,
            SmartEvents.SceneComplete        => true,
            _                                => false
        };
    }

    private static bool IsMinMaxValid(SmartScriptHolder e, uint min, uint max)
    {
        if (max < min)
        {
            Log.Logger.Error($"SmartAIMgr: {e} uses min/max params wrong ({min}/{max}), skipped.");

            return false;
        }

        return true;
    }

    private static bool NotNULL(SmartScriptHolder e, uint data)
    {
        if (data == 0)
        {
            Log.Logger.Error($"SmartAIMgr: {e} Parameter can not be NULL, skipped.");

            return false;
        }

        return true;
    }

    private bool IsAnimKitValid(SmartScriptHolder e, uint entry)
    {
        if (!_cliDB.AnimKitStorage.ContainsKey(entry))
        {
            Log.Logger.Error($"SmartAIMgr: {e} uses non-existent AnimKit entry {entry}, skipped.");

            return false;
        }

        return true;
    }

    private bool IsAreaTriggerValid(SmartScriptHolder e, uint entry)
    {
        if (!_cliDB.AreaTriggerStorage.ContainsKey(entry))
        {
            Log.Logger.Error($"SmartAIMgr: {e} uses non-existent AreaTrigger entry {entry}, skipped.");

            return false;
        }

        return true;
    }

    private bool IsCreatureValid(SmartScriptHolder e, uint entry)
    {
        if (_gameObjectManager.GetCreatureTemplate(entry) == null)
        {
            Log.Logger.Error($"SmartAIMgr: {e} uses non-existent Creature entry {entry}, skipped.");

            return false;
        }

        return true;
    }

    private bool IsEmoteValid(SmartScriptHolder e, uint entry)
    {
        if (!_cliDB.EmotesStorage.ContainsKey(entry))
        {
            Log.Logger.Error($"SmartAIMgr: {e} uses non-existent Emote entry {entry}, skipped.");

            return false;
        }

        return true;
    }

    private bool IsEventValid(SmartScriptHolder e)
    {
        if (e.Event.type >= SmartEvents.End)
        {
            Log.Logger.Error("SmartAIMgr: EntryOrGuid {0} using event({1}) has invalid event type ({2}), skipped.", e.EntryOrGuid, e.EventId, e.GetEventType());

            return false;
        }

        // in SMART_SCRIPT_TYPE_TIMED_ACTIONLIST all event types are overriden by core
        if (e.GetScriptType() != SmartScriptType.TimedActionlist && !Convert.ToBoolean(GetEventMask(e.Event.type) & GetTypeMask(e.GetScriptType())))
        {
            Log.Logger.Error("SmartAIMgr: EntryOrGuid {0}, event type {1} can not be used for Script type {2}", e.EntryOrGuid, e.GetEventType(), e.GetScriptType());

            return false;
        }

        if (e.Action.type is <= 0 or >= SmartActions.End)
        {
            Log.Logger.Error("SmartAIMgr: EntryOrGuid {0} using event({1}) has invalid action type ({2}), skipped.", e.EntryOrGuid, e.EventId, e.GetActionType());

            return false;
        }

        if (e.Event.event_phase_mask > (uint)SmartEventPhaseBits.All)
        {
            Log.Logger.Error("SmartAIMgr: EntryOrGuid {0} using event({1}) has invalid phase mask ({2}), skipped.", e.EntryOrGuid, e.EventId, e.Event.event_phase_mask);

            return false;
        }

        if (e.Event.event_flags > SmartEventFlags.All)
        {
            Log.Logger.Error("SmartAIMgr: EntryOrGuid {0} using event({1}) has invalid event flags ({2}), skipped.", e.EntryOrGuid, e.EventId, e.Event.event_flags);

            return false;
        }

        if (e.Link != 0 && e.Link == e.EventId)
        {
            Log.Logger.Error("SmartAIMgr: EntryOrGuid {0} SourceType {1}, Event {2}, Event is linking self (infinite loop), skipped.", e.EntryOrGuid, e.GetScriptType(), e.EventId);

            return false;
        }

        if (e.GetScriptType() == SmartScriptType.TimedActionlist)
        {
            e.Event.type = SmartEvents.UpdateOoc; //force default OOC, can change when calling the script!

            if (!IsMinMaxValid(e, e.Event.minMaxRepeat.Min, e.Event.minMaxRepeat.Max))
                return false;

            if (!IsMinMaxValid(e, e.Event.minMaxRepeat.RepeatMin, e.Event.minMaxRepeat.RepeatMax))
                return false;
        }
        else
        {
            switch (e.Event.type)
            {
                case SmartEvents.Update:
                case SmartEvents.UpdateIc:
                case SmartEvents.UpdateOoc:
                case SmartEvents.HealthPct:
                case SmartEvents.ManaPct:
                case SmartEvents.Range:
                case SmartEvents.Damaged:
                case SmartEvents.DamagedTarget:
                case SmartEvents.ReceiveHeal:
                    if (!IsMinMaxValid(e, e.Event.minMaxRepeat.Min, e.Event.minMaxRepeat.Max))
                        return false;

                    if (!IsMinMaxValid(e, e.Event.minMaxRepeat.RepeatMin, e.Event.minMaxRepeat.RepeatMax))
                        return false;

                    break;

                case SmartEvents.SpellHit:
                case SmartEvents.SpellHitTarget:
                    if (e.Event.spellHit.Spell != 0)
                    {
                        var spellInfo = _spellManager.GetSpellInfo(e.Event.spellHit.Spell);

                        if (spellInfo == null)
                        {
                            Log.Logger.Error($"SmartAIMgr: {e} uses non-existent Spell entry {e.Event.spellHit.Spell}, skipped.");

                            return false;
                        }

                        if (e.Event.spellHit.School != 0 && ((SpellSchoolMask)e.Event.spellHit.School & spellInfo.SchoolMask) != spellInfo.SchoolMask)
                        {
                            Log.Logger.Error($"SmartAIMgr: {e} uses Spell entry {e.Event.spellHit.Spell} with invalid school mask, skipped.");

                            return false;
                        }
                    }

                    if (!IsMinMaxValid(e, e.Event.spellHit.CooldownMin, e.Event.spellHit.CooldownMax))
                        return false;

                    break;

                case SmartEvents.OnSpellCast:
                case SmartEvents.OnSpellFailed:
                case SmartEvents.OnSpellStart:
                {
                    if (!IsSpellValid(e, e.Event.spellCast.Spell))
                        return false;

                    if (!IsMinMaxValid(e, e.Event.spellCast.CooldownMin, e.Event.spellCast.CooldownMax))
                        return false;

                    break;
                }
                case SmartEvents.OocLos:
                case SmartEvents.IcLos:
                    if (!IsMinMaxValid(e, e.Event.los.CooldownMin, e.Event.los.CooldownMax))
                        return false;

                    if (e.Event.los.HostilityMode >= (uint)LOSHostilityMode.End)
                    {
                        Log.Logger.Error($"SmartAIMgr: {e} uses hostilityMode with invalid value {e.Event.los.HostilityMode} (max allowed value {LOSHostilityMode.End - 1}), skipped.");

                        return false;
                    }

                    TC_SAI_IS_BOOLEAN_VALID(e, e.Event.los.PlayerOnly);

                    break;

                case SmartEvents.Respawn:
                    if (e.Event.respawn.Type == (uint)SmartRespawnCondition.Map && _cliDB.MapStorage.LookupByKey(e.Event.respawn.Map) == null)
                    {
                        Log.Logger.Error($"SmartAIMgr: {e} uses non-existent Map entry {e.Event.respawn.Map}, skipped.");

                        return false;
                    }

                    if (e.Event.respawn.Type == (uint)SmartRespawnCondition.Area && !_cliDB.AreaTableStorage.ContainsKey(e.Event.respawn.Area))
                    {
                        Log.Logger.Error($"SmartAIMgr: {e} uses non-existent Area entry {e.Event.respawn.Area}, skipped.");

                        return false;
                    }

                    break;

                case SmartEvents.FriendlyIsCc:
                    if (!IsMinMaxValid(e, e.Event.friendlyCC.RepeatMin, e.Event.friendlyCC.RepeatMax))
                        return false;

                    break;

                case SmartEvents.FriendlyMissingBuff:
                {
                    if (!IsSpellValid(e, e.Event.missingBuff.Spell))
                        return false;

                    if (!NotNULL(e, e.Event.missingBuff.Radius))
                        return false;

                    if (!IsMinMaxValid(e, e.Event.missingBuff.RepeatMin, e.Event.missingBuff.RepeatMax))
                        return false;

                    break;
                }
                case SmartEvents.Kill:
                    if (!IsMinMaxValid(e, e.Event.kill.CooldownMin, e.Event.kill.CooldownMax))
                        return false;

                    if (e.Event.kill.Creature != 0 && !IsCreatureValid(e, e.Event.kill.Creature))
                        return false;

                    TC_SAI_IS_BOOLEAN_VALID(e, e.Event.kill.PlayerOnly);

                    break;

                case SmartEvents.VictimCasting:
                    if (e.Event.targetCasting.SpellId > 0 && !_spellManager.HasSpellInfo(e.Event.targetCasting.SpellId))
                    {
                        Log.Logger.Error($"SmartAIMgr: Entry {e.EntryOrGuid} SourceType {e.GetScriptType()} Event {e.EventId} Action {e.GetActionType()} uses non-existent Spell entry {e.Event.spellHit.Spell}, skipped.");

                        return false;
                    }

                    if (!IsMinMaxValid(e, e.Event.minMax.RepeatMin, e.Event.minMax.RepeatMax))
                        return false;

                    break;

                case SmartEvents.PassengerBoarded:
                case SmartEvents.PassengerRemoved:
                    if (!IsMinMaxValid(e, e.Event.minMax.RepeatMin, e.Event.minMax.RepeatMax))
                        return false;

                    break;

                case SmartEvents.SummonDespawned:
                case SmartEvents.SummonedUnit:
                case SmartEvents.SummonedUnitDies:
                    if (e.Event.summoned.Creature != 0 && !IsCreatureValid(e, e.Event.summoned.Creature))
                        return false;

                    if (!IsMinMaxValid(e, e.Event.summoned.CooldownMin, e.Event.summoned.CooldownMax))
                        return false;

                    break;

                case SmartEvents.AcceptedQuest:
                case SmartEvents.RewardQuest:
                    if (e.Event.quest.QuestId != 0 && !IsQuestValid(e, e.Event.quest.QuestId))
                        return false;

                    if (!IsMinMaxValid(e, e.Event.quest.CooldownMin, e.Event.quest.CooldownMax))
                        return false;

                    break;

                case SmartEvents.ReceiveEmote:
                {
                    if (e.Event.emote.EmoteId != 0 && !IsTextEmoteValid(e, e.Event.emote.EmoteId))
                        return false;

                    if (!IsMinMaxValid(e, e.Event.emote.CooldownMin, e.Event.emote.CooldownMax))
                        return false;

                    break;
                }
                case SmartEvents.HasAura:
                case SmartEvents.TargetBuffed:
                {
                    if (!IsSpellValid(e, e.Event.aura.Spell))
                        return false;

                    if (!IsMinMaxValid(e, e.Event.aura.RepeatMin, e.Event.aura.RepeatMax))
                        return false;

                    break;
                }
                case SmartEvents.TransportAddcreature:
                {
                    if (e.Event.transportAddCreature.Creature != 0 && !IsCreatureValid(e, e.Event.transportAddCreature.Creature))
                        return false;

                    break;
                }
                case SmartEvents.Movementinform:
                {
                    if (MotionMaster.IsInvalidMovementGeneratorType((MovementGeneratorType)e.Event.movementInform.Type))
                    {
                        Log.Logger.Error($"SmartAIMgr: {e} uses invalid Motion type {e.Event.movementInform.Type}, skipped.");

                        return false;
                    }

                    break;
                }
                case SmartEvents.DataSet:
                {
                    if (!IsMinMaxValid(e, e.Event.dataSet.CooldownMin, e.Event.dataSet.CooldownMax))
                        return false;

                    break;
                }
                case SmartEvents.AreatriggerOntrigger:
                {
                    if (e.Event.areatrigger.ID != 0 && (e.GetScriptType() == SmartScriptType.AreaTriggerEntity || e.GetScriptType() == SmartScriptType.AreaTriggerEntityServerside))
                    {
                        Log.Logger.Error($"SmartAIMgr: Entry {e.EntryOrGuid} SourceType {e.GetScriptType()} Event {e.EventId} Action {e.GetActionType()} areatrigger param not supported for SMART_SCRIPT_TYPE_AREATRIGGER_ENTITY and SMART_SCRIPT_TYPE_AREATRIGGER_ENTITY_SERVERSIDE, skipped.");

                        return false;
                    }

                    if (e.Event.areatrigger.ID != 0 && !IsAreaTriggerValid(e, e.Event.areatrigger.ID))
                        return false;

                    break;
                }
                case SmartEvents.TextOver:
                {
                    if (!IsTextValid(e, e.Event.textOver.TextGroupID))
                        return false;

                    break;
                }
                case SmartEvents.GameEventStart:
                case SmartEvents.GameEventEnd:
                {
                    var events = _eventManager.GetEventMap();

                    if (e.Event.gameEvent.GameEventId >= events.Length || !events[e.Event.gameEvent.GameEventId].IsValid())
                        return false;

                    break;
                }
                case SmartEvents.FriendlyHealthPCT:
                    if (!IsMinMaxValid(e, e.Event.friendlyHealthPct.RepeatMin, e.Event.friendlyHealthPct.RepeatMax))
                        return false;

                    if (e.Event.friendlyHealthPct.MaxHpPct > 100 || e.Event.friendlyHealthPct.MinHpPct > 100)
                    {
                        Log.Logger.Error($"SmartAIMgr: {e} has pct value above 100, skipped.");

                        return false;
                    }

                    switch (e.GetTargetType())
                    {
                        case SmartTargets.CreatureRange:
                        case SmartTargets.CreatureGuid:
                        case SmartTargets.CreatureDistance:
                        case SmartTargets.ClosestCreature:
                        case SmartTargets.ClosestPlayer:
                        case SmartTargets.PlayerRange:
                        case SmartTargets.PlayerDistance:
                            break;

                        case SmartTargets.ActionInvoker:
                            if (!NotNULL(e, e.Event.friendlyHealthPct.Radius))
                                return false;

                            break;

                        default:
                            Log.Logger.Error($"SmartAIMgr: {e} uses invalid target_type {e.GetTargetType()}, skipped.");

                            return false;
                    }

                    break;

                case SmartEvents.DistanceCreature:
                    if (e.Event.distance is { GUID: 0, Entry: 0 })
                    {
                        Log.Logger.Error("SmartAIMgr: Event SMART_EVENT_DISTANCE_CREATURE did not provide creature guid or entry, skipped.");

                        return false;
                    }

                    if (e.Event.distance.GUID != 0 && e.Event.distance.Entry != 0)
                    {
                        Log.Logger.Error("SmartAIMgr: Event SMART_EVENT_DISTANCE_CREATURE provided both an entry and guid, skipped.");

                        return false;
                    }

                    if (e.Event.distance.GUID != 0 && _gameObjectManager.GetCreatureData(e.Event.distance.GUID) == null)
                    {
                        Log.Logger.Error("SmartAIMgr: Event SMART_EVENT_DISTANCE_CREATURE using invalid creature guid {0}, skipped.", e.Event.distance.GUID);

                        return false;
                    }

                    if (e.Event.distance.Entry != 0 && _gameObjectManager.GetCreatureTemplate(e.Event.distance.Entry) == null)
                    {
                        Log.Logger.Error("SmartAIMgr: Event SMART_EVENT_DISTANCE_CREATURE using invalid creature entry {0}, skipped.", e.Event.distance.Entry);

                        return false;
                    }

                    break;

                case SmartEvents.DistanceGameobject:
                    if (e.Event.distance is { GUID: 0, Entry: 0 })
                    {
                        Log.Logger.Error("SmartAIMgr: Event SMART_EVENT_DISTANCE_GAMEOBJECT did not provide gameobject guid or entry, skipped.");

                        return false;
                    }

                    if (e.Event.distance.GUID != 0 && e.Event.distance.Entry != 0)
                    {
                        Log.Logger.Error("SmartAIMgr: Event SMART_EVENT_DISTANCE_GAMEOBJECT provided both an entry and guid, skipped.");

                        return false;
                    }

                    if (e.Event.distance.GUID != 0 && _gameObjectManager.GetGameObjectData(e.Event.distance.GUID) == null)
                    {
                        Log.Logger.Error("SmartAIMgr: Event SMART_EVENT_DISTANCE_GAMEOBJECT using invalid gameobject guid {0}, skipped.", e.Event.distance.GUID);

                        return false;
                    }

                    if (e.Event.distance.Entry != 0 && _gameObjectManager.GetGameObjectTemplate(e.Event.distance.Entry) == null)
                    {
                        Log.Logger.Error("SmartAIMgr: Event SMART_EVENT_DISTANCE_GAMEOBJECT using invalid gameobject entry {0}, skipped.", e.Event.distance.Entry);

                        return false;
                    }

                    break;

                case SmartEvents.CounterSet:
                    if (!IsMinMaxValid(e, e.Event.counter.CooldownMin, e.Event.counter.CooldownMax))
                        return false;

                    if (e.Event.counter.ID == 0)
                    {
                        Log.Logger.Error("SmartAIMgr: Event SMART_EVENT_COUNTER_SET using invalid counter id {0}, skipped.", e.Event.counter.ID);

                        return false;
                    }

                    if (e.Event.counter.Value == 0)
                    {
                        Log.Logger.Error("SmartAIMgr: Event SMART_EVENT_COUNTER_SET using invalid value {0}, skipped.", e.Event.counter.Value);

                        return false;
                    }

                    break;

                case SmartEvents.Reset:
                    if (e.Action.type == SmartActions.CallScriptReset)
                    {
                        // There might be SMART_TARGET_* cases where this should be allowed, they will be handled if needed
                        Log.Logger.Error($"SmartAIMgr: {e} uses event SMART_EVENT_RESET and action SMART_ACTION_CALL_SCRIPT_RESET, skipped.");

                        return false;
                    }

                    break;

                case SmartEvents.Charmed:
                    TC_SAI_IS_BOOLEAN_VALID(e, e.Event.charm.OnRemove);

                    break;

                case SmartEvents.QuestObjCompletion:
                    if (_gameObjectManager.GetQuestObjective(e.Event.questObjective.ID) == null)
                    {
                        Log.Logger.Error($"SmartAIMgr: Event SMART_EVENT_QUEST_OBJ_COMPLETION using invalid objective id {e.Event.questObjective.ID}, skipped.");

                        return false;
                    }

                    break;

                case SmartEvents.QuestAccepted:
                case SmartEvents.QuestCompletion:
                case SmartEvents.QuestFail:
                case SmartEvents.QuestRewarded:
                    break;

                case SmartEvents.Link:
                case SmartEvents.GoLootStateChanged:
                case SmartEvents.GoEventInform:
                case SmartEvents.TimedEventTriggered:
                case SmartEvents.InstancePlayerEnter:
                case SmartEvents.TransportRelocate:
                case SmartEvents.CorpseRemoved:
                case SmartEvents.AiInit:
                case SmartEvents.ActionDone:
                case SmartEvents.TransportAddplayer:
                case SmartEvents.TransportRemovePlayer:
                case SmartEvents.Aggro:
                case SmartEvents.Death:
                case SmartEvents.Evade:
                case SmartEvents.ReachedHome:
                case SmartEvents.JustSummoned:
                case SmartEvents.WaypointReached:
                case SmartEvents.WaypointPaused:
                case SmartEvents.WaypointResumed:
                case SmartEvents.WaypointStopped:
                case SmartEvents.WaypointEnded:
                case SmartEvents.GossipSelect:
                case SmartEvents.GossipHello:
                case SmartEvents.JustCreated:
                case SmartEvents.FollowCompleted:
                case SmartEvents.OnSpellclick:
                case SmartEvents.OnDespawn:
                case SmartEvents.SceneStart:
                case SmartEvents.SceneCancel:
                case SmartEvents.SceneComplete:
                case SmartEvents.SceneTrigger:
                    break;

                //Unused
                case SmartEvents.TargetHealthPct:
                case SmartEvents.FriendlyHealth:
                case SmartEvents.TargetManaPct:
                case SmartEvents.CharmedTarget:
                case SmartEvents.WaypointStart:
                case SmartEvents.PhaseChange:
                case SmartEvents.IsBehindTarget:
                    Log.Logger.Error($"SmartAIMgr: Unused event_type {e} skipped.");

                    return false;

                default:
                    Log.Logger.Error("SmartAIMgr: Not handled event_type({0}), Entry {1} SourceType {2} Event {3} Action {4}, skipped.", e.GetEventType(), e.EntryOrGuid, e.GetScriptType(), e.EventId, e.GetActionType());

                    return false;
            }
        }

        if (!CheckUnusedEventParams(e))
            return false;

        switch (e.GetActionType())
        {
            case SmartActions.Talk:
            {
                TC_SAI_IS_BOOLEAN_VALID(e, e.Action.talk.UseTalkTarget);

                if (!IsTextValid(e, e.Action.talk.TextGroupId))
                    return false;

                break;
            }
            case SmartActions.SimpleTalk:
            {
                if (!IsTextValid(e, e.Action.simpleTalk.TextGroupId))
                    return false;

                break;
            }
            case SmartActions.SetFaction:
                if (e.Action.faction.FactionId != 0 && _cliDB.FactionTemplateStorage.LookupByKey(e.Action.faction.FactionId) == null)
                {
                    Log.Logger.Error($"SmartAIMgr: {e} uses non-existent Faction {e.Action.faction.FactionId}, skipped.");

                    return false;
                }

                break;

            case SmartActions.MorphToEntryOrModel:
            case SmartActions.MountToEntryOrModel:
                if (e.Action.morphOrMount.Creature != 0 || e.Action.morphOrMount.Model != 0)
                {
                    if (e.Action.morphOrMount.Creature > 0 && _gameObjectManager.GetCreatureTemplate(e.Action.morphOrMount.Creature) == null)
                    {
                        Log.Logger.Error($"SmartAIMgr: {e} uses non-existent Creature entry {e.Action.morphOrMount.Creature}, skipped.");

                        return false;
                    }

                    if (e.Action.morphOrMount.Model != 0)
                    {
                        if (e.Action.morphOrMount.Creature != 0)
                        {
                            Log.Logger.Error($"SmartAIMgr: {e} has ModelID set with also set CreatureId, skipped.");

                            return false;
                        }
                        else if (!_cliDB.CreatureDisplayInfoStorage.ContainsKey(e.Action.morphOrMount.Model))
                        {
                            Log.Logger.Error($"SmartAIMgr: {e} uses non-existent Model id {e.Action.morphOrMount.Model}, skipped.");

                            return false;
                        }
                    }
                }

                break;

            case SmartActions.Sound:
                if (!IsSoundValid(e, e.Action.sound.SoundId))
                    return false;

                TC_SAI_IS_BOOLEAN_VALID(e, e.Action.sound.OnlySelf);

                break;

            case SmartActions.SetEmoteState:
            case SmartActions.PlayEmote:
                if (!IsEmoteValid(e, e.Action.emote.EmoteId))
                    return false;

                break;

            case SmartActions.PlayAnimkit:
                if (e.Action.animKit.Kit != 0 && !IsAnimKitValid(e, e.Action.animKit.Kit))
                    return false;

                if (e.Action.animKit.Type > 3)
                {
                    Log.Logger.Error($"SmartAIMgr: {e} uses invalid AnimKit type {e.Action.animKit.Type}, skipped.");

                    return false;
                }

                break;

            case SmartActions.PlaySpellVisualKit:
                if (e.Action.spellVisualKit.SpellVisualKitId != 0 && !IsSpellVisualKitValid(e, e.Action.spellVisualKit.SpellVisualKitId))
                    return false;

                break;

            case SmartActions.OfferQuest:
                if (!IsQuestValid(e, e.Action.questOffer.QuestId))
                    return false;

                TC_SAI_IS_BOOLEAN_VALID(e, e.Action.questOffer.DirectAdd);

                break;

            case SmartActions.FailQuest:
                if (!IsQuestValid(e, e.Action.quest.QuestId))
                    return false;

                break;

            case SmartActions.ActivateTaxi:
            {
                if (!_cliDB.TaxiPathStorage.ContainsKey(e.Action.taxi.ID))
                {
                    Log.Logger.Error($"SmartAIMgr: {e} uses invalid Taxi path ID {e.Action.taxi.ID}, skipped.");

                    return false;
                }

                break;
            }
            case SmartActions.RandomEmote:
                if (e.Action.randomEmote.Emote1 != 0 && !IsEmoteValid(e, e.Action.randomEmote.Emote1))
                    return false;

                if (e.Action.randomEmote.Emote2 != 0 && !IsEmoteValid(e, e.Action.randomEmote.Emote2))
                    return false;

                if (e.Action.randomEmote.Emote3 != 0 && !IsEmoteValid(e, e.Action.randomEmote.Emote3))
                    return false;

                if (e.Action.randomEmote.Emote4 != 0 && !IsEmoteValid(e, e.Action.randomEmote.Emote4))
                    return false;

                if (e.Action.randomEmote.Emote5 != 0 && !IsEmoteValid(e, e.Action.randomEmote.Emote5))
                    return false;

                if (e.Action.randomEmote.Emote6 != 0 && !IsEmoteValid(e, e.Action.randomEmote.Emote6))
                    return false;

                break;

            case SmartActions.RandomSound:
                if (e.Action.randomSound.Sound1 != 0 && !IsSoundValid(e, e.Action.randomSound.Sound1))
                    return false;

                if (e.Action.randomSound.Sound2 != 0 && !IsSoundValid(e, e.Action.randomSound.Sound2))
                    return false;

                if (e.Action.randomSound.Sound3 != 0 && !IsSoundValid(e, e.Action.randomSound.Sound3))
                    return false;

                if (e.Action.randomSound.Sound4 != 0 && !IsSoundValid(e, e.Action.randomSound.Sound4))
                    return false;

                TC_SAI_IS_BOOLEAN_VALID(e, e.Action.randomSound.OnlySelf);

                break;

            case SmartActions.Cast:
            {
                if (!IsSpellValid(e, e.Action.cast.Spell))
                    return false;

                var spellInfo = _spellManager.GetSpellInfo(e.Action.cast.Spell);

                foreach (var spellEffectInfo in spellInfo.Effects)
                    if (spellEffectInfo.IsEffect(SpellEffectName.KillCredit) || spellEffectInfo.IsEffect(SpellEffectName.KillCredit2))
                        if (spellEffectInfo.TargetA.Target == Targets.UnitCaster)
                            Log.Logger.Error($"SmartAIMgr: {e} Effect: SPELL_EFFECT_KILL_CREDIT: (SpellId: {e.Action.cast.Spell} targetA: {spellEffectInfo.TargetA.Target} - targetB: {spellEffectInfo.TargetB.Target}) has invalid target for this Action");

                break;
            }
            case SmartActions.CrossCast:
            {
                if (!IsSpellValid(e, e.Action.crossCast.Spell))
                    return false;

                var targetType = (SmartTargets)e.Action.crossCast.TargetType;

                if (targetType is SmartTargets.CreatureGuid or SmartTargets.GameobjectGuid)
                {
                    if (e.Action.crossCast.TargetParam2 != 0)
                    {
                        if (targetType == SmartTargets.CreatureGuid && !IsCreatureValid(e, e.Action.crossCast.TargetParam2))
                            return false;
                        else if (targetType == SmartTargets.GameobjectGuid && !IsGameObjectValid(e, e.Action.crossCast.TargetParam2))
                            return false;
                    }

                    ulong guid = e.Action.crossCast.TargetParam1;
                    var spawnType = targetType == SmartTargets.CreatureGuid ? SpawnObjectType.Creature : SpawnObjectType.GameObject;
                    var data = _gameObjectManager.GetSpawnData(spawnType, guid);

                    if (data == null)
                    {
                        Log.Logger.Error($"SmartAIMgr: {e} specifies invalid CasterTargetType guid ({spawnType},{guid})");

                        return false;
                    }
                    else if (e.Action.crossCast.TargetParam2 != 0 && e.Action.crossCast.TargetParam2 != data.Id)
                    {
                        Log.Logger.Error($"SmartAIMgr: {e} specifies invalid entry {e.Action.crossCast.TargetParam2} (expected {data.Id}) for CasterTargetType guid ({spawnType},{guid})");

                        return false;
                    }
                }

                break;
            }
            case SmartActions.InvokerCast:
                if (e.GetScriptType() != SmartScriptType.TimedActionlist && e.GetEventType() != SmartEvents.Link && !EventHasInvoker(e.Event.type))
                {
                    Log.Logger.Error($"SmartAIMgr: {e} has invoker cast action, but event does not provide any invoker!");

                    return false;
                }

                if (!IsSpellValid(e, e.Action.cast.Spell))
                    return false;

                break;

            case SmartActions.SelfCast:
                if (!IsSpellValid(e, e.Action.cast.Spell))
                    return false;

                break;

            case SmartActions.CallAreaexploredoreventhappens:
            case SmartActions.CallGroupeventhappens:
                var qid = _gameObjectManager.GetQuestTemplate(e.Action.quest.QuestId);

                if (qid != null)
                {
                    if (!qid.HasSpecialFlag(QuestSpecialFlags.ExplorationOrEvent))
                    {
                        Log.Logger.Error($"SmartAIMgr: {e} SpecialFlags for QuestId entry {e.Action.quest.QuestId} does not include FLAGS_EXPLORATION_OR_EVENT(2), skipped.");

                        return false;
                    }
                }
                else
                {
                    Log.Logger.Error($"SmartAIMgr: {e} uses non-existent QuestId entry {e.Action.quest.QuestId}, skipped.");

                    return false;
                }

                break;

            case SmartActions.SetEventPhase:
                if (e.Action.setEventPhase.Phase >= (uint)SmartPhase.Max)
                {
                    Log.Logger.Error($"SmartAIMgr: {e} attempts to set phase {e.Action.setEventPhase.Phase}. Phase mask cannot be used past phase {SmartPhase.Max - 1}, skipped.");

                    return false;
                }

                break;

            case SmartActions.IncEventPhase:
                if (e.Action.incEventPhase is { Inc: 0, Dec: 0 })
                {
                    Log.Logger.Error($"SmartAIMgr: {e} is incrementing phase by 0, skipped.");

                    return false;
                }
                else if (e.Action.incEventPhase.Inc > (uint)SmartPhase.Max || e.Action.incEventPhase.Dec > (uint)SmartPhase.Max)
                {
                    Log.Logger.Error($"SmartAIMgr: {e} attempts to increment phase by too large value, skipped.");

                    return false;
                }

                break;

            case SmartActions.RemoveAurasFromSpell:
                if (e.Action.removeAura.Spell != 0 && !IsSpellValid(e, e.Action.removeAura.Spell))
                    return false;

                TC_SAI_IS_BOOLEAN_VALID(e, e.Action.removeAura.OnlyOwnedAuras);

                break;

            case SmartActions.RandomPhase:
            {
                if (e.Action.randomPhase.Phase1 >= (uint)SmartPhase.Max ||
                    e.Action.randomPhase.Phase2 >= (uint)SmartPhase.Max ||
                    e.Action.randomPhase.Phase3 >= (uint)SmartPhase.Max ||
                    e.Action.randomPhase.Phase4 >= (uint)SmartPhase.Max ||
                    e.Action.randomPhase.Phase5 >= (uint)SmartPhase.Max ||
                    e.Action.randomPhase.Phase6 >= (uint)SmartPhase.Max)
                {
                    Log.Logger.Error($"SmartAIMgr: Entry {e.EntryOrGuid} SourceType {e.GetScriptType()} Event {e.EventId} Action {e.GetActionType()} attempts to set invalid phase, skipped.");

                    return false;
                }

                break;
            }
            case SmartActions.RandomPhaseRange: //PhaseMin, PhaseMax
            {
                if (e.Action.randomPhaseRange.PhaseMin >= (uint)SmartPhase.Max ||
                    e.Action.randomPhaseRange.PhaseMax >= (uint)SmartPhase.Max)
                {
                    Log.Logger.Error($"SmartAIMgr: {e} attempts to set invalid phase, skipped.");

                    return false;
                }

                if (!IsMinMaxValid(e, e.Action.randomPhaseRange.PhaseMin, e.Action.randomPhaseRange.PhaseMax))
                    return false;

                break;
            }
            case SmartActions.SummonCreature:
                if (!IsCreatureValid(e, e.Action.summonCreature.Creature))
                    return false;

                if (e.Action.summonCreature.Type is < (uint)TempSummonType.TimedOrDeadDespawn or > (uint)TempSummonType.ManualDespawn)
                {
                    Log.Logger.Error($"SmartAIMgr: {e} uses incorrect TempSummonType {e.Action.summonCreature.Type}, skipped.");

                    return false;
                }

                TC_SAI_IS_BOOLEAN_VALID(e, e.Action.summonCreature.AttackInvoker);

                break;

            case SmartActions.CallKilledmonster:
                if (!IsCreatureValid(e, e.Action.killedMonster.Creature))
                    return false;

                if (e.GetTargetType() == SmartTargets.Position)
                {
                    Log.Logger.Error($"SmartAIMgr: {e} uses incorrect TargetType {e.GetTargetType()}, skipped.");

                    return false;
                }

                break;

            case SmartActions.UpdateTemplate:
                if (e.Action.updateTemplate.Creature != 0 && !IsCreatureValid(e, e.Action.updateTemplate.Creature))
                    return false;

                TC_SAI_IS_BOOLEAN_VALID(e, e.Action.updateTemplate.UpdateLevel);

                break;

            case SmartActions.SetSheath:
                if (e.Action.setSheath.Sheath != 0 && e.Action.setSheath.Sheath >= (uint)SheathState.Max)
                {
                    Log.Logger.Error($"SmartAIMgr: {e} uses incorrect Sheath state {e.Action.setSheath.Sheath}, skipped.");

                    return false;
                }

                break;

            case SmartActions.SetReactState:
            {
                if (e.Action.react.State > (uint)ReactStates.Aggressive)
                {
                    Log.Logger.Error("SmartAIMgr: Creature {0} Event {1} Action {2} uses invalid React State {3}, skipped.", e.EntryOrGuid, e.EventId, e.GetActionType(), e.Action.react.State);

                    return false;
                }

                break;
            }
            case SmartActions.SummonGo:
                if (!IsGameObjectValid(e, e.Action.summonGO.Entry))
                    return false;

                break;

            case SmartActions.RemoveItem:
                if (!IsItemValid(e, e.Action.item.Entry))
                    return false;

                if (!NotNULL(e, e.Action.item.Count))
                    return false;

                break;

            case SmartActions.AddItem:
                if (!IsItemValid(e, e.Action.item.Entry))
                    return false;

                if (!NotNULL(e, e.Action.item.Count))
                    return false;

                break;

            case SmartActions.Teleport:
                if (!_cliDB.MapStorage.ContainsKey(e.Action.teleport.MapID))
                {
                    Log.Logger.Error($"SmartAIMgr: {e} uses non-existent Map entry {e.Action.teleport.MapID}, skipped.");

                    return false;
                }

                break;

            case SmartActions.WpStop:
                if (e.Action.wpStop.QuestId != 0 && !IsQuestValid(e, e.Action.wpStop.QuestId))
                    return false;

                TC_SAI_IS_BOOLEAN_VALID(e, e.Action.wpStop.Fail);

                break;

            case SmartActions.WpStart:
            {
                var path = GetPath(e.Action.wpStart.PathID);

                if (path == null || path.Nodes.Empty())
                {
                    Log.Logger.Error($"SmartAIMgr: {e} uses non-existent WaypointPath id {e.Action.wpStart.PathID}, skipped.");

                    return false;
                }

                if (e.Action.wpStart.QuestId != 0 && !IsQuestValid(e, e.Action.wpStart.QuestId))
                    return false;

                TC_SAI_IS_BOOLEAN_VALID(e, e.Action.wpStart.Run);
                TC_SAI_IS_BOOLEAN_VALID(e, e.Action.wpStart.Repeat);

                break;
            }
            case SmartActions.CreateTimedEvent:
            {
                if (!IsMinMaxValid(e, e.Action.timeEvent.Min, e.Action.timeEvent.Max))
                    return false;

                if (!IsMinMaxValid(e, e.Action.timeEvent.RepeatMin, e.Action.timeEvent.RepeatMax))
                    return false;

                break;
            }
            case SmartActions.CallRandomRangeTimedActionlist:
            {
                if (!IsMinMaxValid(e, e.Action.randRangeTimedActionList.IDMin, e.Action.randRangeTimedActionList.IDMax))
                    return false;

                break;
            }
            case SmartActions.SetPower:
            case SmartActions.AddPower:
            case SmartActions.RemovePower:
                if (e.Action.power.PowerType > (int)PowerType.Max)
                {
                    Log.Logger.Error($"SmartAIMgr: {e} uses non-existent Power {e.Action.power.PowerType}, skipped.");

                    return false;
                }

                break;

            case SmartActions.GameEventStop:
            {
                var eventId = e.Action.gameEventStop.ID;

                var events = _eventManager.GetEventMap();

                if (eventId < 1 || eventId >= events.Length)
                {
                    Log.Logger.Error($"SmartAIMgr: {e} uses non-existent event, eventId {e.Action.gameEventStop.ID}, skipped.");

                    return false;
                }

                var eventData = events[eventId];

                if (!eventData.IsValid())
                {
                    Log.Logger.Error($"SmartAIMgr: {e} uses non-existent event, eventId {e.Action.gameEventStop.ID}, skipped.");

                    return false;
                }

                break;
            }
            case SmartActions.GameEventStart:
            {
                var eventId = e.Action.gameEventStart.ID;

                var events = _eventManager.GetEventMap();

                if (eventId < 1 || eventId >= events.Length)
                {
                    Log.Logger.Error($"SmartAIMgr: {e} uses non-existent event, eventId {e.Action.gameEventStart.ID}, skipped.");

                    return false;
                }

                var eventData = events[eventId];

                if (!eventData.IsValid())
                {
                    Log.Logger.Error($"SmartAIMgr: {e} uses non-existent event, eventId {e.Action.gameEventStart.ID}, skipped.");

                    return false;
                }

                break;
            }
            case SmartActions.Equip:
            {
                if (e.GetScriptType() == SmartScriptType.Creature)
                {
                    var equipId = (sbyte)e.Action.equip.Entry;

                    if (equipId != 0 && _gameObjectManager.GetEquipmentInfo((uint)e.EntryOrGuid, equipId) == null)
                    {
                        Log.Logger.Error("SmartScript: SMART_ACTION_EQUIP uses non-existent equipment info id {0} for creature {1}, skipped.", equipId, e.EntryOrGuid);

                        return false;
                    }
                }

                break;
            }
            case SmartActions.SetInstData:
            {
                if (e.Action.setInstanceData.Type > 1)
                {
                    Log.Logger.Error($"SmartAIMgr: {e} uses invalid data type {e.Action.setInstanceData.Type} (value range 0-1), skipped.");

                    return false;
                }
                else if (e.Action.setInstanceData is { Type: 1, Data: > (int)EncounterState.ToBeDecided })
                {
                    Log.Logger.Error($"SmartAIMgr: {e} uses invalid boss state {e.Action.setInstanceData.Data} (value range 0-5), skipped.");

                    return false;
                }

                break;
            }
            case SmartActions.SetIngamePhaseId:
            {
                var phaseId = e.Action.ingamePhaseId.ID;
                var apply = e.Action.ingamePhaseId.Apply;

                if (apply != 0 && apply != 1)
                {
                    Log.Logger.Error("SmartScript: SMART_ACTION_SET_INGAME_PHASE_ID uses invalid apply value {0} (Should be 0 or 1) for creature {1}, skipped", apply, e.EntryOrGuid);

                    return false;
                }

                if (!_cliDB.PhaseStorage.ContainsKey(phaseId))
                {
                    Log.Logger.Error("SmartScript: SMART_ACTION_SET_INGAME_PHASE_ID uses invalid phaseid {0} for creature {1}, skipped", phaseId, e.EntryOrGuid);

                    return false;
                }

                break;
            }
            case SmartActions.SetIngamePhaseGroup:
            {
                var phaseGroup = e.Action.ingamePhaseGroup.GroupId;
                var apply = e.Action.ingamePhaseGroup.Apply;

                if (apply != 0 && apply != 1)
                {
                    Log.Logger.Error("SmartScript: SMART_ACTION_SET_INGAME_PHASE_GROUP uses invalid apply value {0} (Should be 0 or 1) for creature {1}, skipped", apply, e.EntryOrGuid);

                    return false;
                }

                if (_db2Manager.GetPhasesForGroup(phaseGroup).Empty())
                {
                    Log.Logger.Error("SmartScript: SMART_ACTION_SET_INGAME_PHASE_GROUP uses invalid phase group id {0} for creature {1}, skipped", phaseGroup, e.EntryOrGuid);

                    return false;
                }

                break;
            }
            case SmartActions.ScenePlay:
            {
                if (_gameObjectManager.GetSceneTemplate(e.Action.scene.SceneId) == null)
                {
                    Log.Logger.Error("SmartScript: SMART_ACTION_SCENE_PLAY uses sceneId {0} but scene don't exist, skipped", e.Action.scene.SceneId);

                    return false;
                }

                break;
            }
            case SmartActions.SceneCancel:
            {
                if (_gameObjectManager.GetSceneTemplate(e.Action.scene.SceneId) == null)
                {
                    Log.Logger.Error("SmartScript: SMART_ACTION_SCENE_CANCEL uses sceneId {0} but scene don't exist, skipped", e.Action.scene.SceneId);

                    return false;
                }

                break;
            }
            case SmartActions.RespawnBySpawnId:
            {
                if (_gameObjectManager.GetSpawnData((SpawnObjectType)e.Action.respawnData.SpawnType, e.Action.respawnData.SpawnId) == null)
                {
                    Log.Logger.Error($"Entry {e.EntryOrGuid} SourceType {e.GetScriptType()} Event {e.EventId} Action {e.GetActionType()} specifies invalid spawn data ({e.Action.respawnData.SpawnType},{e.Action.respawnData.SpawnId})");

                    return false;
                }

                break;
            }
            case SmartActions.EnableTempGobj:
            {
                if (e.Action.enableTempGO.Duration == 0)
                {
                    Log.Logger.Error($"Entry {e.EntryOrGuid} SourceType {e.GetScriptType()} Event {e.EventId} Action {e.GetActionType()} does not specify duration");

                    return false;
                }

                break;
            }
            case SmartActions.PlayCinematic:
            {
                if (!_cliDB.CinematicSequencesStorage.ContainsKey(e.Action.cinematic.Entry))
                {
                    Log.Logger.Error($"SmartAIMgr: SMART_ACTION_PLAY_CINEMATIC {e} uses invalid entry {e.Action.cinematic.Entry}, skipped.");

                    return false;
                }

                break;
            }
            case SmartActions.PauseMovement:
            {
                if (e.Action.pauseMovement.PauseTimer == 0)
                {
                    Log.Logger.Error($"SmartAIMgr: {e} does not specify pause duration");

                    return false;
                }

                TC_SAI_IS_BOOLEAN_VALID(e, e.Action.pauseMovement.Force);

                break;
            }
            case SmartActions.SetMovementSpeed:
            {
                if (e.Action.movementSpeed.MovementType >= (int)MovementGeneratorType.Max)
                {
                    Log.Logger.Error($"SmartAIMgr: Entry {e.EntryOrGuid} SourceType {e.GetScriptType()} Event {e.EventId} Action {e.GetActionType()} uses invalid movementType {e.Action.movementSpeed.MovementType}, skipped.");

                    return false;
                }

                if (e.Action.movementSpeed is { SpeedInteger: 0, SpeedFraction: 0 })
                {
                    Log.Logger.Error($"SmartAIMgr: Entry {e.EntryOrGuid} SourceType {e.GetScriptType()} Event {e.EventId} Action {e.GetActionType()} uses speed 0, skipped.");

                    return false;
                }

                break;
            }
            case SmartActions.OverrideLight:
            {
                var areaEntry = _cliDB.AreaTableStorage.LookupByKey(e.Action.overrideLight.ZoneId);

                if (areaEntry == null)
                {
                    Log.Logger.Error($"SmartAIMgr: {e} uses non-existent zoneId {e.Action.overrideLight.ZoneId}, skipped.");

                    return false;
                }

                if (areaEntry.ParentAreaID != 0)
                {
                    Log.Logger.Error($"SmartAIMgr: {e} uses subzone (ID: {e.Action.overrideLight.ZoneId}) instead of zone, skipped.");

                    return false;
                }

                if (!_cliDB.LightStorage.ContainsKey(e.Action.overrideLight.AreaLightId))
                {
                    Log.Logger.Error($"SmartAIMgr: {e} uses non-existent areaLightId {e.Action.overrideLight.AreaLightId}, skipped.");

                    return false;
                }

                if (e.Action.overrideLight.OverrideLightId != 0 && !_cliDB.LightStorage.ContainsKey(e.Action.overrideLight.OverrideLightId))
                {
                    Log.Logger.Error($"SmartAIMgr: {e} uses non-existent overrideLightId {e.Action.overrideLight.OverrideLightId}, skipped.");

                    return false;
                }

                break;
            }
            case SmartActions.OverrideWeather:
            {
                var areaEntry = _cliDB.AreaTableStorage.LookupByKey(e.Action.overrideWeather.ZoneId);

                if (areaEntry == null)
                {
                    Log.Logger.Error($"SmartAIMgr: {e} uses non-existent zoneId {e.Action.overrideWeather.ZoneId}, skipped.");

                    return false;
                }

                if (areaEntry.ParentAreaID != 0)
                {
                    Log.Logger.Error($"SmartAIMgr: {e} uses subzone (ID: {e.Action.overrideWeather.ZoneId}) instead of zone, skipped.");

                    return false;
                }

                break;
            }
            case SmartActions.SetAIAnimKit:
            {
                Log.Logger.Error($"SmartAIMgr: Deprecated Event:({e}) skipped.");

                break;
            }
            case SmartActions.SetHealthPct:
            {
                if (e.Action.setHealthPct.Percent is > 100 or 0)
                {
                    Log.Logger.Error($"SmartAIMgr: {e} is trying to set invalid HP percent {e.Action.setHealthPct.Percent}, skipped.");

                    return false;
                }

                break;
            }
            case SmartActions.AutoAttack:
            {
                TC_SAI_IS_BOOLEAN_VALID(e, e.Action.autoAttack.Attack);

                break;
            }
            case SmartActions.AllowCombatMovement:
            {
                TC_SAI_IS_BOOLEAN_VALID(e, e.Action.combatMove.Move);

                break;
            }
            case SmartActions.CallForHelp:
            {
                TC_SAI_IS_BOOLEAN_VALID(e, e.Action.callHelp.WithEmote);

                break;
            }
            case SmartActions.SetVisibility:
            {
                TC_SAI_IS_BOOLEAN_VALID(e, e.Action.visibility.State);

                break;
            }
            case SmartActions.SetActive:
            {
                TC_SAI_IS_BOOLEAN_VALID(e, e.Action.active.State);

                break;
            }
            case SmartActions.SetRun:
            {
                TC_SAI_IS_BOOLEAN_VALID(e, e.Action.setRun.Run);

                break;
            }
            case SmartActions.SetDisableGravity:
            {
                TC_SAI_IS_BOOLEAN_VALID(e, e.Action.setDisableGravity.Disable);

                break;
            }
            case SmartActions.SetCounter:
            {
                TC_SAI_IS_BOOLEAN_VALID(e, e.Action.setCounter.Reset);

                break;
            }
            case SmartActions.CallTimedActionlist:
            {
                TC_SAI_IS_BOOLEAN_VALID(e, e.Action.timedActionList.AllowOverride);

                break;
            }
            case SmartActions.InterruptSpell:
            {
                TC_SAI_IS_BOOLEAN_VALID(e, e.Action.interruptSpellCasting.WithDelayed);
                TC_SAI_IS_BOOLEAN_VALID(e, e.Action.interruptSpellCasting.WithInstant);

                break;
            }
            case SmartActions.FleeForAssist:
            {
                TC_SAI_IS_BOOLEAN_VALID(e, e.Action.fleeAssist.WithEmote);

                break;
            }
            case SmartActions.MoveToPos:
            {
                TC_SAI_IS_BOOLEAN_VALID(e, e.Action.moveToPos.Transport);
                TC_SAI_IS_BOOLEAN_VALID(e, e.Action.moveToPos.DisablePathfinding);

                break;
            }
            case SmartActions.SetRoot:
            {
                TC_SAI_IS_BOOLEAN_VALID(e, e.Action.setRoot.Root);

                break;
            }
            case SmartActions.DisableEvade:
            {
                TC_SAI_IS_BOOLEAN_VALID(e, e.Action.disableEvade.Disable);

                break;
            }
            case SmartActions.LoadEquipment:
            {
                TC_SAI_IS_BOOLEAN_VALID(e, e.Action.loadEquipment.Force);

                break;
            }
            case SmartActions.SetHover:
            {
                TC_SAI_IS_BOOLEAN_VALID(e, e.Action.setHover.Enable);

                break;
            }
            case SmartActions.Evade:
            {
                TC_SAI_IS_BOOLEAN_VALID(e, e.Action.evade.ToRespawnPosition);

                break;
            }
            case SmartActions.SetHealthRegen:
            {
                TC_SAI_IS_BOOLEAN_VALID(e, e.Action.setHealthRegen.RegenHealth);

                break;
            }
            case SmartActions.CreateConversation:
            {
                if (_conversationDataStorage.GetConversationTemplate(e.Action.conversation.ID) == null)
                {
                    Log.Logger.Error($"SmartAIMgr: SMART_ACTION_CREATE_CONVERSATION Entry {e.EntryOrGuid} SourceType {e.GetScriptType()} Event {e.EventId} Action {e.GetActionType()} uses invalid entry {e.Action.conversation.ID}, skipped.");

                    return false;
                }

                break;
            }
            case SmartActions.SetImmunePC:
            {
                TC_SAI_IS_BOOLEAN_VALID(e, e.Action.setImmunePC.ImmunePc);

                break;
            }
            case SmartActions.SetImmuneNPC:
            {
                TC_SAI_IS_BOOLEAN_VALID(e, e.Action.setImmuneNPC.ImmuneNPC);

                break;
            }
            case SmartActions.SetUninteractible:
            {
                TC_SAI_IS_BOOLEAN_VALID(e, e.Action.setUninteractible.Uninteractible);

                break;
            }
            case SmartActions.ActivateGameobject:
            {
                if (!NotNULL(e, e.Action.activateGameObject.GameObjectAction))
                    return false;

                if (e.Action.activateGameObject.GameObjectAction >= (uint)GameObjectActions.Max)
                {
                    Log.Logger.Error($"SmartAIMgr: {e} has gameObjectAction parameter out of range (max allowed {(uint)GameObjectActions.Max - 1}, current value {e.Action.activateGameObject.GameObjectAction}), skipped.");

                    return false;
                }

                break;
            }
            case SmartActions.StartClosestWaypoint:
            case SmartActions.Follow:
            case SmartActions.SetOrientation:
            case SmartActions.StoreTargetList:
            case SmartActions.CombatStop:
            case SmartActions.Die:
            case SmartActions.SetInCombatWithZone:
            case SmartActions.WpResume:
            case SmartActions.KillUnit:
            case SmartActions.SetInvincibilityHpLevel:
            case SmartActions.ResetGobject:
            case SmartActions.AttackStart:
            case SmartActions.ThreatAllPct:
            case SmartActions.ThreatSinglePct:
            case SmartActions.SetInstData64:
            case SmartActions.SetData:
            case SmartActions.AttackStop:
            case SmartActions.WpPause:
            case SmartActions.ForceDespawn:
            case SmartActions.Playmovie:
            case SmartActions.CloseGossip:
            case SmartActions.TriggerTimedEvent:
            case SmartActions.RemoveTimedEvent:
            case SmartActions.ActivateGobject:
            case SmartActions.CallScriptReset:
            case SmartActions.SetRangedMovement:
            case SmartActions.SetNpcFlag:
            case SmartActions.AddNpcFlag:
            case SmartActions.RemoveNpcFlag:
            case SmartActions.CallRandomTimedActionlist:
            case SmartActions.RandomMove:
            case SmartActions.SetUnitFieldBytes1:
            case SmartActions.RemoveUnitFieldBytes1:
            case SmartActions.JumpToPos:
            case SmartActions.SendGossipMenu:
            case SmartActions.GoSetLootState:
            case SmartActions.GoSetGoState:
            case SmartActions.SendTargetToTarget:
            case SmartActions.SetHomePos:
            case SmartActions.SummonCreatureGroup:
            case SmartActions.MoveOffset:
            case SmartActions.SetCorpseDelay:
            case SmartActions.AddThreat:
            case SmartActions.TriggerRandomTimedEvent:
            case SmartActions.SpawnSpawngroup:
            case SmartActions.AddToStoredTargetList:
            case SmartActions.DoAction:
                break;

            case SmartActions.BecomePersonalCloneForPlayer:
            {
                if (e.Action.becomePersonalClone.Type is < (uint)TempSummonType.TimedOrDeadDespawn or > (uint)TempSummonType.ManualDespawn)
                {
                    Log.Logger.Error($"SmartAIMgr: {e} uses incorrect TempSummonType {e.Action.becomePersonalClone.Type}, skipped.");

                    return false;
                }

                break;
            }
            case SmartActions.TriggerGameEvent:
            {
                TC_SAI_IS_BOOLEAN_VALID(e, e.Action.triggerGameEvent.UseSaiTargetAsGameEventSource);

                break;
            }
            // No longer supported
            case SmartActions.SetUnitFlag:
            case SmartActions.RemoveUnitFlag:
            case SmartActions.InstallAITemplate:
            case SmartActions.SetSwim:
            case SmartActions.AddAura:
            case SmartActions.OverrideScriptBaseObject:
            case SmartActions.ResetScriptBaseObject:
            case SmartActions.SendGoCustomAnim:
            case SmartActions.SetDynamicFlag:
            case SmartActions.AddDynamicFlag:
            case SmartActions.RemoveDynamicFlag:
            case SmartActions.SetGoFlag:
            case SmartActions.AddGoFlag:
            case SmartActions.RemoveGoFlag:
            case SmartActions.SetCanFly:
            case SmartActions.RemoveAurasByType:
            case SmartActions.SetSightDist:
            case SmartActions.Flee:
            case SmartActions.RemoveAllGameobjects:
                Log.Logger.Error($"SmartAIMgr: Unused action_type: {e} Skipped.");

                return false;

            default:
                Log.Logger.Error("SmartAIMgr: Not handled action_type({0}), event_type({1}), Entry {2} SourceType {3} Event {4}, skipped.", e.GetActionType(), e.GetEventType(), e.EntryOrGuid, e.GetScriptType(), e.EventId);

                return false;
        }

        if (!CheckUnusedActionParams(e))
            return false;

        return true;
    }

    private bool IsGameObjectValid(SmartScriptHolder e, uint entry)
    {
        if (_gameObjectManager.GetGameObjectTemplate(entry) == null)
        {
            Log.Logger.Error($"SmartAIMgr: {e} uses non-existent GameObject entry {entry}, skipped.");

            return false;
        }

        return true;
    }

    private bool IsItemValid(SmartScriptHolder e, uint entry)
    {
        if (!_cliDB.ItemSparseStorage.ContainsKey(entry))
        {
            Log.Logger.Error($"SmartAIMgr: {e} uses non-existent Item entry {entry}, skipped.");

            return false;
        }

        return true;
    }

    private bool IsQuestValid(SmartScriptHolder e, uint entry)
    {
        if (_gameObjectManager.GetQuestTemplate(entry) == null)
        {
            Log.Logger.Error($"SmartAIMgr: {e} uses non-existent QuestId entry {entry}, skipped.");

            return false;
        }

        return true;
    }

    private bool IsSoundValid(SmartScriptHolder e, uint entry)
    {
        if (!_cliDB.SoundKitStorage.ContainsKey(entry))
        {
            Log.Logger.Error($"SmartAIMgr: {e} uses non-existent Sound entry {entry}, skipped.");

            return false;
        }

        return true;
    }

    private bool IsSpellValid(SmartScriptHolder e, uint entry)
    {
        if (!_spellManager.HasSpellInfo(entry))
        {
            Log.Logger.Error($"SmartAIMgr: {e} uses non-existent Spell entry {entry}, skipped.");

            return false;
        }

        return true;
    }

    private bool IsSpellVisualKitValid(SmartScriptHolder e, uint entry)
    {
        if (!_cliDB.SpellVisualKitStorage.ContainsKey(entry))
        {
            Log.Logger.Error($"SmartAIMgr: Entry {e.EntryOrGuid} SourceType {e.GetScriptType()} Event {e.EventId} Action {e.GetActionType()} uses non-existent SpellVisualKit entry {entry}, skipped.");

            return false;
        }

        return true;
    }

    private bool IsTargetValid(SmartScriptHolder e)
    {
        if (Math.Abs(e.Target.o) > 2 * MathFunctions.PI)
            Log.Logger.Error($"SmartAIMgr: {e} has abs(`target.o` = {e.Target.o}) > 2*PI (orientation is expressed in radians)");

        switch (e.GetTargetType())
        {
            case SmartTargets.CreatureDistance:
            case SmartTargets.CreatureRange:
            {
                if (e.Target.unitDistance.Creature != 0 && _gameObjectManager.GetCreatureTemplate(e.Target.unitDistance.Creature) == null)
                {
                    Log.Logger.Error($"SmartAIMgr: {e} uses non-existent Creature entry {e.Target.unitDistance.Creature} as target_param1, skipped.");

                    return false;
                }

                break;
            }
            case SmartTargets.GameobjectDistance:
            case SmartTargets.GameobjectRange:
            {
                if (e.Target.goDistance.Entry != 0 && _gameObjectManager.GetGameObjectTemplate(e.Target.goDistance.Entry) == null)
                {
                    Log.Logger.Error($"SmartAIMgr: {e} uses non-existent GameObject entry {e.Target.goDistance.Entry} as target_param1, skipped.");

                    return false;
                }

                break;
            }
            case SmartTargets.CreatureGuid:
            {
                if (e.Target.unitGUID.Entry != 0 && !IsCreatureValid(e, e.Target.unitGUID.Entry))
                    return false;

                ulong guid = e.Target.unitGUID.DBGuid;
                var data = _gameObjectManager.GetCreatureData(guid);

                if (data == null)
                {
                    Log.Logger.Error($"SmartAIMgr: {e} using invalid creature guid {guid} as target_param1, skipped.");

                    return false;
                }
                else if (e.Target.unitGUID.Entry != 0 && e.Target.unitGUID.Entry != data.Id)
                {
                    Log.Logger.Error($"SmartAIMgr: {e} using invalid creature entry {e.Target.unitGUID.Entry} (expected {data.Id}) for guid {guid} as target_param1, skipped.");

                    return false;
                }

                break;
            }
            case SmartTargets.GameobjectGuid:
            {
                if (e.Target.goGUID.Entry != 0 && !IsGameObjectValid(e, e.Target.goGUID.Entry))
                    return false;

                ulong guid = e.Target.goGUID.DBGuid;
                var data = _gameObjectManager.GetGameObjectData(guid);

                if (data == null)
                {
                    Log.Logger.Error($"SmartAIMgr: {e} using invalid gameobject guid {guid} as target_param1, skipped.");

                    return false;
                }
                else if (e.Target.goGUID.Entry != 0 && e.Target.goGUID.Entry != data.Id)
                {
                    Log.Logger.Error($"SmartAIMgr: {e} using invalid gameobject entry {e.Target.goGUID.Entry} (expected {data.Id}) for guid {guid} as target_param1, skipped.");

                    return false;
                }

                break;
            }
            case SmartTargets.PlayerDistance:
            case SmartTargets.ClosestPlayer:
            {
                if (e.Target.playerDistance.Dist == 0)
                {
                    Log.Logger.Error($"SmartAIMgr: {e} has maxDist 0 as target_param1, skipped.");

                    return false;
                }

                break;
            }
            case SmartTargets.ActionInvoker:
            case SmartTargets.ActionInvokerVehicle:
            case SmartTargets.InvokerParty:
                if (e.GetScriptType() != SmartScriptType.TimedActionlist && e.GetEventType() != SmartEvents.Link && !EventHasInvoker(e.Event.type))
                {
                    Log.Logger.Error($"SmartAIMgr: Entry {e.EntryOrGuid} SourceType {e.GetScriptType()} Event {e.GetEventType()} Action {e.GetActionType()} has invoker target, but event does not provide any invoker!");

                    return false;
                }

                break;

            case SmartTargets.HostileSecondAggro:
            case SmartTargets.HostileLastAggro:
            case SmartTargets.HostileRandom:
            case SmartTargets.HostileRandomNotTop:
                TC_SAI_IS_BOOLEAN_VALID(e, e.Target.hostilRandom.PlayerOnly);

                break;

            case SmartTargets.Farthest:
                TC_SAI_IS_BOOLEAN_VALID(e, e.Target.farthest.PlayerOnly);
                TC_SAI_IS_BOOLEAN_VALID(e, e.Target.farthest.IsInLos);

                break;

            case SmartTargets.ClosestCreature:
                TC_SAI_IS_BOOLEAN_VALID(e, e.Target.unitClosest.Dead);

                break;

            case SmartTargets.ClosestEnemy:
                TC_SAI_IS_BOOLEAN_VALID(e, e.Target.closestAttackable.PlayerOnly);

                break;

            case SmartTargets.ClosestFriendly:
                TC_SAI_IS_BOOLEAN_VALID(e, e.Target.closestFriendly.PlayerOnly);

                break;

            case SmartTargets.OwnerOrSummoner:
                TC_SAI_IS_BOOLEAN_VALID(e, e.Target.owner.UseCharmerOrOwner);

                break;

            case SmartTargets.ClosestGameobject:
            case SmartTargets.PlayerRange:
            case SmartTargets.Self:
            case SmartTargets.Victim:
            case SmartTargets.Position:
            case SmartTargets.None:
            case SmartTargets.ThreatList:
            case SmartTargets.Stored:
            case SmartTargets.LootRecipients:
            case SmartTargets.VehiclePassenger:
            case SmartTargets.ClosestUnspawnedGameobject:
                break;

            default:
                Log.Logger.Error("SmartAIMgr: Not handled target_type({0}), Entry {1} SourceType {2} Event {3} Action {4}, skipped.", e.GetTargetType(), e.EntryOrGuid, e.GetScriptType(), e.EventId, e.GetActionType());

                return false;
        }

        if (!CheckUnusedTargetParams(e))
            return false;

        return true;
    }

    private bool IsTextEmoteValid(SmartScriptHolder e, uint entry)
    {
        if (!_cliDB.EmotesTextStorage.ContainsKey(entry))
        {
            Log.Logger.Error($"SmartAIMgr: {e} uses non-existent Text Emote entry {entry}, skipped.");

            return false;
        }

        return true;
    }

    private bool IsTextValid(SmartScriptHolder e, uint id)
    {
        if (e.GetScriptType() != SmartScriptType.Creature)
            return true;

        uint entry;

        if (e.GetEventType() == SmartEvents.TextOver)
            entry = e.Event.textOver.CreatureEntry;
        else
            switch (e.GetTargetType())
            {
                case SmartTargets.CreatureDistance:
                case SmartTargets.CreatureRange:
                case SmartTargets.ClosestCreature:
                    return true; // ignore
                default:
                    if (e.EntryOrGuid < 0)
                    {
                        var guid = (ulong)-e.EntryOrGuid;
                        var data = _gameObjectManager.GetCreatureData(guid);

                        if (data == null)
                        {
                            Log.Logger.Error($"SmartAIMgr: {e} using non-existent Creature guid {guid}, skipped.");

                            return false;
                        }
                        else
                        {
                            entry = data.Id;
                        }
                    }
                    else
                    {
                        entry = (uint)e.EntryOrGuid;
                    }

                    break;
            }

        if (entry == 0 || !_creatureTextManager.TextExist(entry, (byte)id))
        {
            Log.Logger.Error($"SmartAIMgr: {e} using non-existent Text id {id}, skipped.");

            return false;
        }

        return true;
    }
}