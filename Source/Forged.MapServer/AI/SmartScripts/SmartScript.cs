// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Autofac;
using Forged.MapServer.AI.CoreAI;
using Forged.MapServer.Conditions;
using Forged.MapServer.DataStorage.Structs.A;
using Forged.MapServer.Entities;
using Forged.MapServer.Entities.AreaTriggers;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Events;
using Forged.MapServer.Globals;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Checks;
using Forged.MapServer.Maps.GridNotifiers;
using Forged.MapServer.Movement;
using Forged.MapServer.Phasing;
using Forged.MapServer.Scripting.Interfaces.IAreaTrigger;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Forged.MapServer.Text;
using Forged.MapServer.Weather;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.AI.SmartScripts;

public class SmartScript
{
    public ObjectGuid LastInvoker;

    // Max number of nested ProcessEventsFor() calls to avoid infinite loops
    private const uint MaxNestedEvents = 10;

    private readonly ConditionManager _conditionManager;
    private readonly Dictionary<uint, uint> _counterList = new();
    private readonly CreatureTextManager _creatureTextManager;
    private readonly List<SmartScriptHolder> _events = new();
    private readonly GameEventManager _gameEventManager;
    private readonly CellCalculator _cellCalculator;
    private readonly PhasingHandler _phasingHandler;
    private readonly List<SmartScriptHolder> _installEvents = new();
    private readonly ObjectAccessor _objectAccessor;
    private readonly GameObjectManager _objectManager;
    private readonly List<uint> _remIDs = new();
    private readonly SmartAIManager _smartAIManager;
    private readonly List<SmartScriptHolder> _storedEvents = new();
    private readonly Dictionary<uint, ObjectGuidList> _storedTargets = new();
    private SmartEventFlags _allEventFlags;
    private AreaTrigger _areaTrigger;
    private uint _currentPriority;
    private uint _eventPhase;
    private bool _eventSortingRequired;
    private GameObject _go;
    private ObjectGuid _goOrigGUID;
    private uint _lastTextID;
    private Creature _me;
    private ObjectGuid _meOrigGUID;
    private ObjectGuid _mTimedActionListInvoker;
    private uint _nestedEventsCounter;
    private uint _pathId;
    private Player _player;
    private Quest.Quest _quest;
    private SceneTemplate _sceneTemplate;
    private SmartScriptType _scriptType;
    private uint _talkerEntry;
    private ObjectGuid _textGUID;
    private uint _textTimer;
    private List<SmartScriptHolder> _timedActionList = new();
    private AreaTriggerRecord _trigger;
    private bool _useTextTimer;

    public SmartScript(ConditionManager conditionManager, SmartAIManager smartAIManager, ObjectAccessor objectAccessor, CreatureTextManager creatureTextManager,
                       GameObjectManager objectManager, GameEventManager gameEventManager, CellCalculator cellCalculator, PhasingHandler phasingHandler)
    {
        _conditionManager = conditionManager;
        _smartAIManager = smartAIManager;
        _objectAccessor = objectAccessor;
        _creatureTextManager = creatureTextManager;
        _objectManager = objectManager;
        _gameEventManager = gameEventManager;
        _cellCalculator = cellCalculator;
        _phasingHandler = phasingHandler;
        _go = null;
        _me = null;
        _trigger = null;
        _eventPhase = 0;
        _pathId = 0;
        _textTimer = 0;
        _lastTextID = 0;
        _textGUID = ObjectGuid.Empty;
        _useTextTimer = false;
        _talkerEntry = 0;
        _meOrigGUID = ObjectGuid.Empty;
        _goOrigGUID = ObjectGuid.Empty;
        LastInvoker = ObjectGuid.Empty;
        _scriptType = SmartScriptType.Creature;
    }

    public bool CheckTimer(SmartScriptHolder e)
    {
        return e.Active;
    }

    public Unit DoSelectBelowHpPctFriendlyWithEntry(uint entry, float range, byte minHpDiff = 1, bool excludeSelf = true)
    {
        FriendlyBelowHpPctEntryInRange uCheck = new(_me, entry, range, minHpDiff, excludeSelf);
        UnitLastSearcher searcher = new(_me, uCheck, GridType.All);
        _cellCalculator.VisitGrid(_me, searcher, range);

        return searcher.GetTarget();
    }

    public uint GetPathId()
    {
        return _pathId;
    }

    public List<WorldObject> GetStoredTargetList(uint id, WorldObject obj)
    {
        var list = _storedTargets.LookupByKey(id);

        return list?.GetObjectList(obj);
    }

    public bool HasAnyEventWithFlag(SmartEventFlags flag)
    {
        return _allEventFlags.HasAnyFlag(flag);
    }

    public bool IsCharmedCreature(WorldObject obj)
    {
        return obj?.AsCreature is { IsCharmed: true };
    }

    public bool IsCreature(WorldObject obj)
    {
        return obj != null && obj.IsTypeId(TypeId.Unit);
    }

    public bool IsGameObject(WorldObject obj)
    {
        return obj != null && obj.IsTypeId(TypeId.GameObject);
    }

    public bool IsPlayer(WorldObject obj)
    {
        return obj != null && obj.IsTypeId(TypeId.Player);
    }

    public bool IsUnit(WorldObject obj)
    {
        return obj != null && (obj.IsTypeId(TypeId.Unit) || obj.IsTypeId(TypeId.Player));
    }

    public void OnInitialize(WorldObject obj, AreaTriggerRecord at = null, SceneTemplate scene = null, Quest.Quest qst = null)
    {
        if (at != null)
        {
            _scriptType = SmartScriptType.AreaTrigger;
            _trigger = at;
            _player = obj.AsPlayer;

            if (_player == null)
            {
                Log.Logger.Error($"SmartScript::OnInitialize: source is AreaTrigger with id {_trigger.Id}, missing trigger player");

                return;
            }

            Log.Logger.Debug($"SmartScript::OnInitialize: source is AreaTrigger with id {_trigger.Id}, triggered by player {_player.GUID}");
        }
        else if (scene != null)
        {
            _scriptType = SmartScriptType.Scene;
            _sceneTemplate = scene;
            _player = obj.AsPlayer;

            if (_player == null)
            {
                Log.Logger.Error($"SmartScript::OnInitialize: source is Scene with id {_sceneTemplate.SceneId}, missing trigger player");

                return;
            }

            Log.Logger.Debug($"SmartScript::OnInitialize: source is Scene with id {_sceneTemplate.SceneId}, triggered by player {_player.GUID}");
        }
        else if (qst != null)
        {
            _scriptType = SmartScriptType.Quest;
            _quest = qst;
            _player = obj.AsPlayer;

            if (_player == null)
            {
                Log.Logger.Error($"SmartScript::OnInitialize: source is QuestId with id {qst.Id}, missing trigger player");

                return;
            }

            Log.Logger.Debug($"SmartScript::OnInitialize: source is QuestId with id {qst.Id}, triggered by player {_player.GUID}");
        }
        else if (obj != null) // Handle object based scripts
        {
            switch (obj.TypeId)
            {
                case TypeId.Unit:
                    _scriptType = SmartScriptType.Creature;
                    _me = obj.AsCreature;
                    Log.Logger.Debug($"SmartScript.OnInitialize: source is Creature {_me.Entry}");

                    break;

                case TypeId.GameObject:
                    _scriptType = SmartScriptType.GameObject;
                    _go = obj.AsGameObject;
                    Log.Logger.Debug($"SmartScript.OnInitialize: source is GameObject {_go.Entry}");

                    break;

                case TypeId.AreaTrigger:
                    _areaTrigger = obj.AsAreaTrigger;
                    _scriptType = _areaTrigger.IsServerSide ? SmartScriptType.AreaTriggerEntityServerside : SmartScriptType.AreaTriggerEntity;
                    Log.Logger.Debug($"SmartScript.OnInitialize: source is AreaTrigger {_areaTrigger.Entry}, IsServerSide {_areaTrigger.IsServerSide}");

                    break;

                default:
                    Log.Logger.Error("SmartScript.OnInitialize: Unhandled TypeID !WARNING!");

                    return;
            }
        }
        else
        {
            Log.Logger.Error("SmartScript.OnInitialize: !WARNING! Initialized WorldObject is Null.");

            return;
        }

        GetScript(); //load copy of script

        lock (_events)
        {
            foreach (var holder in _events)
                InitTimer(holder); //calculate timers for first Time use
        }

        ProcessEventsFor(SmartEvents.AiInit);
        InstallEvents();
        ProcessEventsFor(SmartEvents.JustCreated);
        _counterList.Clear();
    }

    public void OnMoveInLineOfSight(Unit who)
    {
        if (_me == null)
            return;

        ProcessEventsFor(_me.IsEngaged ? SmartEvents.IcLos : SmartEvents.OocLos, who);
    }

    public void OnReset()
    {
        ResetBaseObject();

        lock (_events)
        {
            foreach (var holder in _events)
            {
                if (!holder.Event.EventFlags.HasAnyFlag(SmartEventFlags.DontReset))
                {
                    InitTimer(holder);
                    holder.RunOnce = false;
                }

                if (holder.Priority != SmartScriptHolder.DEFAULT_PRIORITY)
                {
                    holder.Priority = SmartScriptHolder.DEFAULT_PRIORITY;
                    _eventSortingRequired = true;
                }
            }
        }

        ProcessEventsFor(SmartEvents.Reset);
        LastInvoker.Clear();
    }

    public void OnUpdate(uint diff)
    {
        if (_scriptType is SmartScriptType.Creature or SmartScriptType.GameObject or SmartScriptType.AreaTriggerEntity or SmartScriptType.AreaTriggerEntityServerside && GetBaseObject() == null)
            return;

        if (_me is { IsInEvadeMode: true })
        {
            // Check if the timed action list finished and clear it if so.
            // This is required by SMART_ACTION_CALL_TIMED_ACTIONLIST failing if mTimedActionList is not empty.
            if (_timedActionList.Empty())
                return;

            var needCleanup1 = true;

            foreach (var _ in _timedActionList.Where(scriptholder => scriptholder.EnableTimed))
                needCleanup1 = false;

            if (needCleanup1)
                _timedActionList.Clear();

            return;
        }

        InstallEvents(); //before UpdateTimers

        if (_eventSortingRequired)
        {
            lock (_events)
            {
                SortEvents(_events);
            }

            _eventSortingRequired = false;
        }

        lock (_events)
        {
            foreach (var holder in _events)
                UpdateTimer(holder, diff);
        }

        if (!_storedEvents.Empty())
            foreach (var holder in _storedEvents)
                UpdateTimer(holder, diff);

        var needCleanup = true;

        if (!_timedActionList.Empty())
            for (var i = 0; i < _timedActionList.Count; ++i)
            {
                var scriptHolder = _timedActionList[i];

                if (scriptHolder.EnableTimed)
                {
                    UpdateTimer(scriptHolder, diff);
                    needCleanup = false;
                }
            }

        if (needCleanup)
            _timedActionList.Clear();

        if (!_remIDs.Empty())
        {
            foreach (var id in _remIDs)
                RemoveStoredEvent(id);

            _remIDs.Clear();
        }

        if (_useTextTimer && _me != null)
        {
            if (_textTimer < diff)
            {
                var textID = _lastTextID;
                _lastTextID = 0;
                var entry = _talkerEntry;
                _talkerEntry = 0;
                _textTimer = 0;
                _useTextTimer = false;
                ProcessEventsFor(SmartEvents.TextOver, null, textID, entry);
            }
            else
            {
                _textTimer -= diff;
            }
        }
    }

    public void ProcessEventsFor(SmartEvents e, Unit unit = null, uint var0 = 0, uint var1 = 0, bool bvar = false, SpellInfo spell = null, GameObject gob = null, string varString = "")
    {
        _nestedEventsCounter++;

        // Allow only a fixed number of nested ProcessEventsFor calls
        if (_nestedEventsCounter > MaxNestedEvents)
            Log.Logger.Warning($"SmartScript::ProcessEventsFor: reached the limit of max allowed nested ProcessEventsFor() calls with event {e}, skipping!\n{GetBaseObject().GetDebugInfo()}");
        else if (_nestedEventsCounter == 1)
            lock (_events) // only lock on the first event to prevent deadlock.
            {
                Process(e, unit, var0, var1, bvar, spell, gob, varString);
            }
        else
            Process(e, unit, var0, var1, bvar, spell, gob, varString);

        --_nestedEventsCounter;

        void Process(SmartEvents ev, Unit un, uint v0, uint v1, bool bv, SpellInfo spll, GameObject ob, string vString)
        {
            foreach (var @event in _events)
            {
                var eventType = @event.GetEventType();

                if (eventType == SmartEvents.Link) //special handling
                    continue;

                if (eventType != ev)
                    continue;

                if (_conditionManager.IsObjectMeetingSmartEventConditions(@event.EntryOrGuid, @event.EventId, @event.SourceType, un, GetBaseObject()))
                    ProcessEvent(@event, un, v0, v1, bv, spll, ob, vString);
            }
        }
    }

    public void SetPathId(uint id)
    {
        _pathId = id;
    }

    public void SetTimedActionList(SmartScriptHolder e, uint entry, Unit invoker, uint startFromEventId = 0)
    {
        // Do NOT allow to start a new actionlist if a previous one is already running, unless explicitly allowed. We need to always finish the current actionlist
        if (e.GetActionType() == SmartActions.CallTimedActionlist && e.Action.TimedActionList.AllowOverride == 0 && !_timedActionList.Empty())
            return;

        _timedActionList.Clear();
        _timedActionList = _smartAIManager.GetScript((int)entry, SmartScriptType.TimedActionlist);

        if (_timedActionList.Empty())
            return;

        _timedActionList.RemoveAll(script => script.EventId < startFromEventId);

        _mTimedActionListInvoker = invoker?.GUID ?? ObjectGuid.Empty;

        for (var i = 0; i < _timedActionList.Count; ++i)
        {
            var scriptHolder = _timedActionList[i];
            scriptHolder.EnableTimed = i == 0; //enable processing only for the first action

            scriptHolder.Event.Type = e.Action.TimedActionList.TimerType switch
            {
                0   => SmartEvents.UpdateOoc,
                1   => SmartEvents.UpdateIc,
                > 1 => SmartEvents.Update
            };

            InitTimer(scriptHolder);
        }
    }

    private void AddToStoredTargetList(List<WorldObject> targets, uint id)
    {
        var inserted = _storedTargets.TryAdd(id, new ObjectGuidList(targets));

        if (inserted)
            return;

        foreach (var obj in targets)
            _storedTargets[id].AddGuid(obj.GUID);
    }

    private SmartScriptHolder CreateSmartEvent(SmartEvents e, SmartEventFlags eventFlags, uint eventParam1, uint eventParam2, uint eventParam3, uint eventParam4, uint eventParam5,
                                               SmartActions action, uint actionParam1, uint actionParam2, uint actionParam3, uint actionParam4, uint actionParam5, uint actionParam6, uint actionParam7,
                                               SmartTargets t, uint targetParam1, uint targetParam2, uint targetParam3, uint targetParam4, uint phaseMask)
    {
        SmartScriptHolder script = new();
        script.Event.Type = e;
        script.Event.Raw.Param1 = eventParam1;
        script.Event.Raw.Param2 = eventParam2;
        script.Event.Raw.Param3 = eventParam3;
        script.Event.Raw.Param4 = eventParam4;
        script.Event.Raw.Param5 = eventParam5;
        script.Event.EventPhaseMask = phaseMask;
        script.Event.EventFlags = eventFlags;
        script.Event.EventChance = 100;

        script.Action.Type = action;
        script.Action.raw.Param1 = actionParam1;
        script.Action.raw.Param2 = actionParam2;
        script.Action.raw.Param3 = actionParam3;
        script.Action.raw.Param4 = actionParam4;
        script.Action.raw.Param5 = actionParam5;
        script.Action.raw.Param6 = actionParam6;
        script.Action.raw.Param7 = actionParam7;

        script.Target.Type = t;
        script.Target.Raw.Param1 = targetParam1;
        script.Target.Raw.Param2 = targetParam2;
        script.Target.Raw.Param3 = targetParam3;
        script.Target.Raw.Param4 = targetParam4;

        script.SourceType = SmartScriptType.Creature;
        InitTimer(script);

        return script;
    }

    private void DecPhase(uint p)
    {
        if (p >= _eventPhase)
            SetPhase(0);
        else
            SetPhase(_eventPhase - p);
    }

    private Unit DoFindClosestFriendlyInRange(float range)
    {
        if (_me == null)
            return null;

        var uCheck = new AnyFriendlyUnitInObjectRangeCheck(_me, _me, range);
        var searcher = new UnitLastSearcher(_me, uCheck, GridType.All);
        _cellCalculator.VisitGrid(_me, searcher, range);

        return searcher.GetTarget();
    }

    private void DoFindFriendlyCc(List<Creature> creatures, float range)
    {
        if (_me == null)
            return;

        var uCheck = new FriendlyCCedInRange(_me, range);
        var searcher = new CreatureListSearcher(_me, creatures, uCheck, GridType.Grid);
        _cellCalculator.VisitGrid(_me, searcher, range);
    }

    private void DoFindFriendlyMissingBuff(List<Creature> creatures, float range, uint spellid)
    {
        if (_me == null)
            return;

        var uCheck = new FriendlyMissingBuffInRange(_me, range, spellid);
        var searcher = new CreatureListSearcher(_me, creatures, uCheck, GridType.Grid);
        _cellCalculator.VisitGrid(_me, searcher, range);
    }

    private Unit DoSelectLowestHpPercentFriendly(float range, uint minHpPct, uint maxHpPct)
    {
        if (_me == null)
            return null;

        MostHpPercentMissingInRange uCheck = new(_me, range, minHpPct, maxHpPct);
        UnitLastSearcher searcher = new(_me, uCheck, GridType.Grid);
        _cellCalculator.VisitGrid(_me, searcher, range);

        return searcher.GetTarget();
    }

    private void FillScript(List<SmartScriptHolder> e, WorldObject obj, AreaTriggerRecord at, SceneTemplate scene, Quest.Quest quest)
    {
        if (e.Empty())
        {
            if (obj != null)
                Log.Logger.Debug($"SmartScript: EventMap for Entry {obj.Entry} is empty but is using SmartScript.");

            if (at != null)
                Log.Logger.Debug($"SmartScript: EventMap for AreaTrigger {at.Id} is empty but is using SmartScript.");

            if (scene != null)
                Log.Logger.Debug($"SmartScript: EventMap for SceneId {scene.SceneId} is empty but is using SmartScript.");

            if (quest != null)
                Log.Logger.Debug($"SmartScript: EventMap for QuestId {quest.Id} is empty but is using SmartScript.");

            return;
        }

        foreach (var holder in e)
        {
            if (holder.Event.EventFlags.HasAnyFlag(SmartEventFlags.DifficultyAll)) //if has instance Id add only if in it
            {
                if (!(obj != null && obj.Location.Map.IsDungeon))
                    continue;

                // TODO: fix it for new maps and difficulties
                switch (obj.Location.Map.DifficultyID)
                {
                    case Difficulty.Normal:
                    case Difficulty.Raid10N:
                        if (holder.Event.EventFlags.HasAnyFlag(SmartEventFlags.Difficulty0))
                            lock (_events)
                            {
                                _events.Add(holder);
                            }

                        break;

                    case Difficulty.Heroic:
                    case Difficulty.Raid25N:
                        if (holder.Event.EventFlags.HasAnyFlag(SmartEventFlags.Difficulty1))
                            lock (_events)
                            {
                                _events.Add(holder);
                            }

                        break;

                    case Difficulty.Raid10HC:
                        if (holder.Event.EventFlags.HasAnyFlag(SmartEventFlags.Difficulty2))
                            lock (_events)
                            {
                                _events.Add(holder);
                            }

                        break;

                    case Difficulty.Raid25HC:
                        if (holder.Event.EventFlags.HasAnyFlag(SmartEventFlags.Difficulty3))
                            lock (_events)
                            {
                                _events.Add(holder);
                            }

                        break;
                }
            }

            _allEventFlags |= holder.Event.EventFlags;

            lock (_events)
            {
                _events.Add(holder); //NOTE: 'world(0)' events still get processed in ANY instance mode
            }
        }
    }

    private Creature FindCreatureNear(WorldObject searchObject, ulong guid)
    {
        if (searchObject.Location.Map.CreatureBySpawnIdStore.TryGetValue(guid, out var bounds))
            return null;

        var foundCreature = bounds.Find(creature => creature.IsAlive);

        return foundCreature ?? bounds[0];
    }

    private GameObject FindGameObjectNear(WorldObject searchObject, ulong guid)
    {
        if (searchObject.Location.Map.GameObjectBySpawnIdStore.TryGetValue(guid, out var bounds))
            return null;

        return bounds[0];
    }

    private WorldObject GetBaseObject()
    {
        WorldObject obj = null;

        if (_me != null)
            obj = _me;
        else if (_go != null)
            obj = _go;
        else if (_areaTrigger != null)
            obj = _areaTrigger;
        else if (_player != null)
            obj = _player;

        return obj;
    }

    private WorldObject GetBaseObjectOrUnitInvoker(Unit invoker)
    {
        return GetBaseObject() ?? invoker;
    }

    private uint GetCounterValue(uint id)
    {
        if (_counterList.ContainsKey(id))
            return _counterList[id];

        return 0;
    }

    private Unit GetLastInvoker(Unit invoker = null)
    {
        // Look for invoker only on map of base object... Prevents multithreaded crashes
        var baseObject = GetBaseObject();

        if (baseObject != null)
            return _objectAccessor.GetUnit(baseObject, LastInvoker);
        // used for area triggers invoker cast
        else if (invoker != null)
            return _objectAccessor.GetUnit(invoker, LastInvoker);

        return null;
    }

    private void GetScript()
    {
        List<SmartScriptHolder> e;

        if (_me != null)
        {
            e = _smartAIManager.GetScript(-(int)_me.SpawnId, _scriptType);

            if (e.Empty())
                e = _smartAIManager.GetScript((int)_me.Entry, _scriptType);

            FillScript(e, _me, null, null, null);
        }
        else if (_go != null)
        {
            e = _smartAIManager.GetScript(-(int)_go.SpawnId, _scriptType);

            if (e.Empty())
                e = _smartAIManager.GetScript((int)_go.Entry, _scriptType);

            FillScript(e, _go, null, null, null);
        }
        else if (_trigger != null)
        {
            e = _smartAIManager.GetScript((int)_trigger.Id, _scriptType);
            FillScript(e, null, _trigger, null, null);
        }
        else if (_areaTrigger != null)
        {
            e = _smartAIManager.GetScript((int)_areaTrigger.Entry, _scriptType);
            FillScript(e, _areaTrigger, null, null, null);
        }
        else if (_sceneTemplate != null)
        {
            e = _smartAIManager.GetScript((int)_sceneTemplate.SceneId, _scriptType);
            FillScript(e, null, null, _sceneTemplate, null);
        }
        else if (_quest != null)
        {
            e = _smartAIManager.GetScript((int)_quest.Id, _scriptType);
            FillScript(e, null, null, null, _quest);
        }
    }

    private List<WorldObject> GetTargets(SmartScriptHolder e, WorldObject invoker = null)
    {
        WorldObject scriptTrigger = null;

        if (invoker != null)
        {
            scriptTrigger = invoker;
        }
        else
        {
            var tempLastInvoker = GetLastInvoker();

            if (tempLastInvoker != null)
                scriptTrigger = tempLastInvoker;
        }

        var baseObject = GetBaseObject();

        List<WorldObject> targets = new();

        switch (e.GetTargetType())
        {
            case SmartTargets.Self:
                if (baseObject != null)
                    targets.Add(baseObject);

                break;

            case SmartTargets.Victim:
                if (_me is { Victim: { } })
                    targets.Add(_me.Victim);

                break;

            case SmartTargets.HostileSecondAggro:
                if (_me != null)
                {
                    if (e.Target.HostilRandom.PowerType != 0)
                    {
                        var u = _me.AI.SelectTarget(SelectTargetMethod.MaxThreat, 1, new PowerUsersSelector(_me, (PowerType)(e.Target.HostilRandom.PowerType - 1), e.Target.HostilRandom.MaxDist, e.Target.HostilRandom.PlayerOnly != 0));

                        if (u != null)
                            targets.Add(u);
                    }
                    else
                    {
                        var u = _me.AI.SelectTarget(SelectTargetMethod.MaxThreat, 1, e.Target.HostilRandom.MaxDist, e.Target.HostilRandom.PlayerOnly != 0);

                        if (u != null)
                            targets.Add(u);
                    }
                }

                break;

            case SmartTargets.HostileLastAggro:
                if (_me != null)
                {
                    if (e.Target.HostilRandom.PowerType != 0)
                    {
                        var u = _me.AI.SelectTarget(SelectTargetMethod.MinThreat, 1, new PowerUsersSelector(_me, (PowerType)(e.Target.HostilRandom.PowerType - 1), e.Target.HostilRandom.MaxDist, e.Target.HostilRandom.PlayerOnly != 0));

                        if (u != null)
                            targets.Add(u);
                    }
                    else
                    {
                        var u = _me.AI.SelectTarget(SelectTargetMethod.MinThreat, 1, e.Target.HostilRandom.MaxDist, e.Target.HostilRandom.PlayerOnly != 0);

                        if (u != null)
                            targets.Add(u);
                    }
                }

                break;

            case SmartTargets.HostileRandom:
                if (_me != null)
                {
                    if (e.Target.HostilRandom.PowerType != 0)
                    {
                        var u = _me.AI.SelectTarget(SelectTargetMethod.Random, 1, new PowerUsersSelector(_me, (PowerType)(e.Target.HostilRandom.PowerType - 1), e.Target.HostilRandom.MaxDist, e.Target.HostilRandom.PlayerOnly != 0));

                        if (u != null)
                            targets.Add(u);
                    }
                    else
                    {
                        var u = _me.AI.SelectTarget(SelectTargetMethod.Random, 1, e.Target.HostilRandom.MaxDist, e.Target.HostilRandom.PlayerOnly != 0);

                        if (u != null)
                            targets.Add(u);
                    }
                }

                break;

            case SmartTargets.HostileRandomNotTop:
                if (_me != null)
                {
                    if (e.Target.HostilRandom.PowerType != 0)
                    {
                        var u = _me.AI.SelectTarget(SelectTargetMethod.Random, 1, new PowerUsersSelector(_me, (PowerType)(e.Target.HostilRandom.PowerType - 1), e.Target.HostilRandom.MaxDist, e.Target.HostilRandom.PlayerOnly != 0));

                        if (u != null)
                            targets.Add(u);
                    }
                    else
                    {
                        var u = _me.AI.SelectTarget(SelectTargetMethod.Random, 1, e.Target.HostilRandom.MaxDist, e.Target.HostilRandom.PlayerOnly != 0);

                        if (u != null)
                            targets.Add(u);
                    }
                }

                break;

            case SmartTargets.Farthest:
                if (_me != null)
                {
                    var u = _me.AI.SelectTarget(SelectTargetMethod.MaxDistance, 0, new FarthestTargetSelector(_me, e.Target.Farthest.MaxDist, e.Target.Farthest.PlayerOnly != 0, e.Target.Farthest.IsInLos != 0));

                    if (u != null)
                        targets.Add(u);
                }

                break;

            case SmartTargets.ActionInvoker:
                if (scriptTrigger != null)
                    targets.Add(scriptTrigger);

                break;

            case SmartTargets.ActionInvokerVehicle:
                if (scriptTrigger is { AsUnit.Vehicle: { } } && scriptTrigger.AsUnit.Vehicle.GetBase() != null)
                    targets.Add(scriptTrigger.AsUnit.Vehicle.GetBase());

                break;

            case SmartTargets.InvokerParty:
                var player = scriptTrigger?.AsPlayer;

                if (player != null)
                {
                    var group = player.Group;

                    if (group != null)
                        for (var groupRef = group.FirstMember; groupRef != null; groupRef = groupRef.Next())
                        {
                            var member = groupRef.Source;

                            if (member == null)
                                continue;

                            if (member.Location.IsInMap(player))
                                targets.Add(member);
                        }
                    // We still add the player to the list if there is no group. If we do
                    // this even if there is a group (thus the else-check), it will add the
                    // same player to the list twice. We don't want that to happen.
                    else
                        targets.Add(scriptTrigger);
                }

                break;

            case SmartTargets.CreatureRange:
            {
                var refObj = baseObject;

                if (refObj == null)
                    refObj = scriptTrigger;

                if (refObj == null)
                {
                    Log.Logger.Error($"SMART_TARGET_CREATURE_RANGE: {e} is missing base object or invoker.");

                    break;
                }

                var units = GetWorldObjectsInDist(e.Target.UnitRange.MaxDist);

                foreach (var obj in units)
                {
                    if (!IsCreature(obj))
                        continue;

                    if (_me != null && _me == obj)
                        continue;

                    if ((e.Target.UnitRange.Creature == 0 || obj.AsCreature.Entry == e.Target.UnitRange.Creature) && refObj.Location.IsInRange(obj, e.Target.UnitRange.MinDist, e.Target.UnitRange.MaxDist))
                        targets.Add(obj);
                }

                if (e.Target.UnitRange.MaxSize != 0)
                    targets.RandomResize(e.Target.UnitRange.MaxSize);

                break;
            }
            case SmartTargets.CreatureDistance:
            {
                var units = GetWorldObjectsInDist(e.Target.UnitDistance.Dist);

                foreach (var obj in units)
                {
                    if (!IsCreature(obj))
                        continue;

                    if (_me != null && _me == obj)
                        continue;

                    if (e.Target.UnitDistance.Creature == 0 || obj.AsCreature.Entry == e.Target.UnitDistance.Creature)
                        targets.Add(obj);
                }

                if (e.Target.UnitDistance.MaxSize != 0)
                    targets.RandomResize(e.Target.UnitDistance.MaxSize);

                break;
            }
            case SmartTargets.GameobjectDistance:
            {
                var units = GetWorldObjectsInDist(e.Target.GoDistance.Dist);

                foreach (var obj in units)
                {
                    if (!IsGameObject(obj))
                        continue;

                    if (_go != null && _go == obj)
                        continue;

                    if (e.Target.GoDistance.Entry == 0 || obj.AsGameObject.Entry == e.Target.GoDistance.Entry)
                        targets.Add(obj);
                }

                if (e.Target.GoDistance.MaxSize != 0)
                    targets.RandomResize(e.Target.GoDistance.MaxSize);

                break;
            }
            case SmartTargets.GameobjectRange:
            {
                var refObj = baseObject;

                if (refObj == null)
                    refObj = scriptTrigger;

                if (refObj == null)
                {
                    Log.Logger.Error($"SMART_TARGET_GAMEOBJECT_RANGE: {e} is missing base object or invoker.");

                    break;
                }

                var units = GetWorldObjectsInDist(e.Target.GoRange.MaxDist);

                foreach (var obj in units)
                {
                    if (!IsGameObject(obj))
                        continue;

                    if (_go != null && _go == obj)
                        continue;

                    if ((e.Target.GoRange.Entry == 0 || obj.AsGameObject.Entry == e.Target.GoRange.Entry) && refObj.Location.IsInRange(obj, e.Target.GoRange.MinDist, e.Target.GoRange.MaxDist))
                        targets.Add(obj);
                }

                if (e.Target.GoRange.MaxSize != 0)
                    targets.RandomResize(e.Target.GoRange.MaxSize);

                break;
            }
            case SmartTargets.CreatureGuid:
            {
                if (scriptTrigger == null && baseObject == null)
                {
                    Log.Logger.Error($"SMART_TARGET_CREATURE_GUID {e} can not be used without invoker");

                    break;
                }

                var target = FindCreatureNear(scriptTrigger ?? baseObject, e.Target.UnitGUID.DBGuid);
                
                if (target != null && (e.Target.UnitGUID.Entry == 0 || target.Entry == e.Target.UnitGUID.Entry))
                    targets.Add(target);

                break;
            }
            case SmartTargets.GameobjectGuid:
            {
                if (scriptTrigger == null && baseObject == null)
                {
                    Log.Logger.Error($"SMART_TARGET_GAMEOBJECT_GUID {e} can not be used without invoker");

                    break;
                }

                var target = FindGameObjectNear(scriptTrigger ?? baseObject, e.Target.GoGUID.DBGuid);

                if (target != null && (e.Target.GoGUID.Entry == 0 || target.Entry == e.Target.GoGUID.Entry))
                    targets.Add(target);

                break;
            }
            case SmartTargets.PlayerRange:
            {
                var units = GetWorldObjectsInDist(e.Target.PlayerRange.MaxDist);

                if (!units.Empty() && baseObject != null)
                    foreach (var obj in units)
                        if (IsPlayer(obj) && baseObject.Location.IsInRange(obj, e.Target.PlayerRange.MinDist, e.Target.PlayerRange.MaxDist))
                            targets.Add(obj);

                break;
            }
            case SmartTargets.PlayerDistance:
            {
                var units = GetWorldObjectsInDist(e.Target.PlayerDistance.Dist);

                foreach (var obj in units)
                    if (IsPlayer(obj))
                        targets.Add(obj);

                break;
            }
            case SmartTargets.Stored:
            {
                var refObj = baseObject;

                if (refObj == null)
                    refObj = scriptTrigger;

                if (refObj == null)
                {
                    Log.Logger.Error($"SMART_TARGET_STORED: {e} is missing base object or invoker.");

                    break;
                }

                var stored = GetStoredTargetList(e.Target.Stored.ID, refObj);

                if (stored != null)
                    targets.AddRange(stored);

                break;
            }
            case SmartTargets.ClosestCreature:
            {
                var refObj = baseObject;

                if (refObj == null)
                    refObj = scriptTrigger;

                if (refObj == null)
                {
                    Log.Logger.Error($"SMART_TARGET_CLOSEST_CREATURE: {e} is missing base object or invoker.");

                    break;
                }

                var target = refObj.Location.FindNearestCreature(e.Target.UnitClosest.Entry, e.Target.UnitClosest.Dist != 0 ? e.Target.UnitClosest.Dist : 100, e.Target.UnitClosest.Dead == 0);

                if (target != null)
                    targets.Add(target);

                break;
            }
            case SmartTargets.ClosestGameobject:
            {
                var refObj = baseObject ?? scriptTrigger;

                if (refObj == null)
                {
                    Log.Logger.Error($"SMART_TARGET_CLOSEST_GAMEOBJECT: {e} is missing base object or invoker.");

                    break;
                }

                var target = refObj.Location.FindNearestGameObject(e.Target.GoClosest.Entry, e.Target.GoClosest.Dist != 0 ? e.Target.GoClosest.Dist : 100);

                if (target != null)
                    targets.Add(target);

                break;
            }
            case SmartTargets.ClosestPlayer:
            {
                var refObj = baseObject ?? scriptTrigger;

                if (refObj == null)
                {
                    Log.Logger.Error($"SMART_TARGET_CLOSEST_PLAYER: {e} is missing base object or invoker.");

                    break;
                }

                var target = refObj.Location.SelectNearestPlayer(e.Target.PlayerDistance.Dist);

                if (target != null)
                    targets.Add(target);

                break;
            }
            case SmartTargets.OwnerOrSummoner:
            {
                if (_me != null)
                {
                    var charmerOrOwnerGuid = _me.CharmerOrOwnerGUID;

                    if (charmerOrOwnerGuid.IsEmpty)
                    {
                        var tempSummon = _me.ToTempSummon();

                        if (tempSummon != null)
                        {
                            var summoner = tempSummon.GetSummoner();

                            if (summoner != null)
                                charmerOrOwnerGuid = summoner.GUID;
                        }
                    }

                    if (charmerOrOwnerGuid.IsEmpty)
                        charmerOrOwnerGuid = _me.CreatorGUID;

                    var owner = _objectAccessor.GetWorldObject(_me, charmerOrOwnerGuid);

                    if (owner != null)
                        targets.Add(owner);
                }
                else if (_go != null)
                {
                    var owner = _objectAccessor.GetUnit(_go, _go.OwnerGUID);

                    if (owner != null)
                        targets.Add(owner);
                }

                // Get owner of owner
                if (e.Target.Owner.UseCharmerOrOwner != 0 && !targets.Empty())
                {
                    var owner = targets.First();
                    targets.Clear();

                    var unitBase = _objectAccessor.GetUnit(owner, owner.CharmerOrOwnerGUID);

                    if (unitBase != null)
                        targets.Add(unitBase);
                }

                break;
            }
            case SmartTargets.ThreatList:
            {
                if (_me is { CanHaveThreatList: true })
                    foreach (var refe in _me.GetThreatManager().SortedThreatList)
                        if (e.Target.ThreatList.MaxDist == 0 || _me.IsWithinCombatRange(refe.Victim, e.Target.ThreatList.MaxDist))
                            targets.Add(refe.Victim);

                break;
            }
            case SmartTargets.ClosestEnemy:
            {
                var target = _me?.SelectNearestTarget(e.Target.ClosestAttackable.MaxDist);

                if (target != null)
                    targets.Add(target);

                break;
            }
            case SmartTargets.ClosestFriendly:
            {
                if (_me != null)
                {
                    var target = DoFindClosestFriendlyInRange(e.Target.ClosestFriendly.MaxDist);

                    if (target != null)
                        targets.Add(target);
                }

                break;
            }
            case SmartTargets.LootRecipients:
            {
                if (_me != null)
                    foreach (var tapperGuid in _me.TapList)
                    {
                        var tapper = _objectAccessor.GetPlayer(_me, tapperGuid);

                        if (tapper != null)
                            targets.Add(tapper);
                    }

                break;
            }
            case SmartTargets.VehiclePassenger:
            {
                if (_me != null && _me.IsVehicle)
                    foreach (var pair in _me.VehicleKit.Seats)
                        if (e.Target.Vehicle.SeatMask == 0 || (e.Target.Vehicle.SeatMask & (1 << pair.Key)) != 0)
                        {
                            var u = _objectAccessor.GetUnit(_me, pair.Value.Passenger.Guid);

                            if (u != null)
                                targets.Add(u);
                        }

                break;
            }
            case SmartTargets.ClosestUnspawnedGameobject:
            {
                var target = baseObject.Location.FindNearestUnspawnedGameObject(e.Target.GoClosest.Entry, e.Target.GoClosest.Dist != 0 ? e.Target.GoClosest.Dist : 100);

                if (target != null)
                    targets.Add(target);

                break;
            }
            case SmartTargets.Position:
            default:
                break;
        }

        return targets;
    }

    private List<WorldObject> GetWorldObjectsInDist(float dist)
    {
        List<WorldObject> targets = new();
        var obj = GetBaseObject();

        if (obj == null)
            return targets;

        var uCheck = new AllWorldObjectsInRange(obj, dist);
        var searcher = new WorldObjectListSearcher(obj, targets, uCheck);
        _cellCalculator.VisitGrid(obj, searcher, dist);

        return targets;
    }

    private void IncPhase(uint p)
    {
        // protect phase from overflowing
        SetPhase(Math.Min((uint)SmartPhase.Phase12, _eventPhase + p));
    }

    private void InitTimer(SmartScriptHolder e)
    {
        switch (e.GetEventType())
        {
            //set only events which have initial timers
            case SmartEvents.Update:
            case SmartEvents.UpdateIc:
            case SmartEvents.UpdateOoc:
                RecalcTimer(e, e.Event.MinMaxRepeat.Min, e.Event.MinMaxRepeat.Max);

                break;

            case SmartEvents.DistanceCreature:
            case SmartEvents.DistanceGameobject:
                RecalcTimer(e, e.Event.Distance.Repeat, e.Event.Distance.Repeat);

                break;

            default:
                e.Active = true;

                break;
        }
    }

    private void InstallEvents()
    {
        if (!_installEvents.Empty())
        {
            lock (_events)
            {
                foreach (var holder in _installEvents)
                    _events.Add(holder); //must be before UpdateTimers
            }

            _installEvents.Clear();
        }
    }

    private bool IsInPhase(uint p)
    {
        if (_eventPhase == 0)
            return false;

        return ((1 << (int)(_eventPhase - 1)) & p) != 0;
    }

    private bool IsSmart(Creature creature, bool silent = false)
    {
        if (creature == null)
            return false;

        var smart = creature.GetAI<SmartAI>() != null;

        if (!smart && !silent)
            Log.Logger.Error("SmartScript: Action target Creature (GUID: {0} Entry: {1}) is not using SmartAI, action skipped to prevent crash.", creature.SpawnId, creature.Entry);

        return smart;
    }

    private bool IsSmart(GameObject gameObject, bool silent = false)
    {
        if (gameObject == null)
            return false;

        var smart = gameObject.GetAI<SmartGameObjectAI>() != null;

        if (!smart && !silent)
            Log.Logger.Error("SmartScript: Action target GameObject (GUID: {0} Entry: {1}) is not using SmartGameObjectAI, action skipped to prevent crash.", gameObject.SpawnId, gameObject.Entry);

        return smart;
    }

    private bool IsSmart(bool silent = false)
    {
        if (_me != null)
            return IsSmart(_me, silent);

        if (_go != null)
            return IsSmart(_go, silent);

        return false;
    }

    private void ProcessAction(SmartScriptHolder e, Unit unit = null, uint var0 = 0, uint var1 = 0, bool bvar = false, SpellInfo spell = null, GameObject gob = null, string varString = "")
    {
        e.RunOnce = true; //used for repeat check

        //calc random
        if (e.GetEventType() != SmartEvents.Link && e.Event.EventChance < 100 && e.Event.EventChance != 0 && !e.Event.EventFlags.HasFlag(SmartEventFlags.TempIgnoreChanceRoll))
            if (RandomHelper.randChance(e.Event.EventChance))
                return;

        // Remove SMART_EVENT_FLAG_TEMP_IGNORE_CHANCE_ROLL Id after processing roll chances as it's not needed anymore
        e.Event.EventFlags &= ~SmartEventFlags.TempIgnoreChanceRoll;

        if (unit != null)
            LastInvoker = unit.GUID;

        var tempInvoker = GetLastInvoker();

        if (tempInvoker != null)
            Log.Logger.Debug("SmartScript.ProcessAction: Invoker: {0} (guidlow: {1})", tempInvoker.GetName(), tempInvoker.GUID.ToString());

        var targets = GetTargets(e, unit != null ? unit : gob);

        switch (e.GetActionType())
        {
            case SmartActions.Talk:
            {
                var talker = e.Target.Type == 0 ? _me : null;
                Unit talkTarget = null;

                foreach (var target in targets)
                    if (IsCreature(target) && !target.AsCreature.IsPet) // Prevented sending text to pets.
                    {
                        if (e.Action.Talk.UseTalkTarget != 0)
                        {
                            talker = _me;
                            talkTarget = target.AsCreature;
                        }
                        else
                        {
                            talker = target.AsCreature;
                        }

                        break;
                    }
                    else if (IsPlayer(target))
                    {
                        talker = _me;
                        talkTarget = target.AsPlayer;

                        break;
                    }

                if (talkTarget == null)
                    talkTarget = GetLastInvoker();

                if (talker == null)
                    break;

                _talkerEntry = talker.Entry;
                _lastTextID = e.Action.Talk.TextGroupId;
                _textTimer = e.Action.Talk.Duration;

                _useTextTimer = true;
                _creatureTextManager.SendChat(talker, (byte)e.Action.Talk.TextGroupId, talkTarget);

                Log.Logger.Debug("SmartScript.ProcessAction: SMART_ACTION_TALK: talker: {0} (Guid: {1}), textGuid: {2}",
                                 talker.GetName(),
                                 talker.GUID.ToString(),
                                 _textGUID.ToString());

                break;
            }
            case SmartActions.SimpleTalk:
            {
                foreach (var target in targets)
                {
                    if (IsCreature(target))
                    {
                        _creatureTextManager.SendChat(target.AsCreature, (byte)e.Action.SimpleTalk.TextGroupId, IsPlayer(GetLastInvoker()) ? GetLastInvoker() : null);
                    }
                    else if (IsPlayer(target) && _me != null)
                    {
                        var templastInvoker = GetLastInvoker();
                        _creatureTextManager.SendChat(_me, (byte)e.Action.SimpleTalk.TextGroupId, IsPlayer(templastInvoker) ? templastInvoker : null, ChatMsg.Addon, Language.Addon, CreatureTextRange.Normal, 0, SoundKitPlayType.Normal, TeamFaction.Other, false, target.AsPlayer);
                    }

                    Log.Logger.Debug("SmartScript.ProcessAction. SMART_ACTION_SIMPLE_TALK: talker: {0} (GuidLow: {1}), textGroupId: {2}",
                                     target.GetName(),
                                     target.GUID.ToString(),
                                     e.Action.SimpleTalk.TextGroupId);
                }

                break;
            }
            case SmartActions.PlayEmote:
            {
                foreach (var target in targets)
                    if (IsUnit(target))
                    {
                        target.AsUnit.HandleEmoteCommand((Emote)e.Action.Emote.EmoteId);

                        Log.Logger.Debug("SmartScript.ProcessAction. SMART_ACTION_PLAY_EMOTE: target: {0} (GuidLow: {1}), emote: {2}",
                                         target.GetName(),
                                         target.GUID.ToString(),
                                         e.Action.Emote.EmoteId);
                    }

                break;
            }
            case SmartActions.Sound:
            {
                foreach (var target in targets)
                    if (IsUnit(target))
                    {
                        if (e.Action.Sound.Distance == 1)
                            target.PlayDistanceSound(e.Action.Sound.SoundId, e.Action.Sound.OnlySelf != 0 ? target.AsPlayer : null);
                        else
                            target.PlayDirectSound(e.Action.Sound.SoundId, e.Action.Sound.OnlySelf != 0 ? target.AsPlayer : null, e.Action.Sound.KeyBroadcastTextId);

                        Log.Logger.Debug("SmartScript.ProcessAction. SMART_ACTION_SOUND: target: {0} (GuidLow: {1}), sound: {2}, onlyself: {3}",
                                         target.GetName(),
                                         target.GUID.ToString(),
                                         e.Action.Sound.SoundId,
                                         e.Action.Sound.OnlySelf);
                    }

                break;
            }
            case SmartActions.SetFaction:
            {
                foreach (var target in targets)
                    if (IsCreature(target))
                    {
                        if (e.Action.Faction.FactionId != 0)
                        {
                            target.AsCreature.Faction = e.Action.Faction.FactionId;

                            Log.Logger.Debug("SmartScript.ProcessAction. SMART_ACTION_SET_FACTION: Creature entry {0}, GuidLow {1} set faction to {2}",
                                             target.Entry,
                                             target.GUID.ToString(),
                                             e.Action.Faction.FactionId);
                        }
                        else
                        {
                            var ci = _objectManager.GetCreatureTemplate(target.AsCreature.Entry);

                            if (ci != null)
                                if (target.AsCreature.Faction != ci.Faction)
                                {
                                    target.AsCreature.Faction = ci.Faction;

                                    Log.Logger.Debug("SmartScript.ProcessAction. SMART_ACTION_SET_FACTION: Creature entry {0}, GuidLow {1} set faction to {2}",
                                                     target.Entry,
                                                     target.GUID.ToString(),
                                                     ci.Faction);
                                }
                        }
                    }

                break;
            }
            case SmartActions.MorphToEntryOrModel:
            {
                foreach (var target in targets)
                {
                    if (!IsCreature(target))
                        continue;

                    if (e.Action.MorphOrMount.Creature != 0 || e.Action.MorphOrMount.Model != 0)
                    {
                        //set model based on entry from creature_template
                        if (e.Action.MorphOrMount.Creature != 0)
                        {
                            var ci = _objectManager.GetCreatureTemplate(e.Action.MorphOrMount.Creature);

                            if (ci == null)
                                continue;

                            var model = _objectManager.ChooseDisplayId(ci);
                            target.AsCreature.SetDisplayId(model.CreatureDisplayId, model.DisplayScale);

                            Log.Logger.Debug("SmartScript.ProcessAction. SMART_ACTION_MORPH_TO_ENTRY_OR_MODEL: Creature entry {0}, GuidLow {1} set displayid to {2}",
                                             target.Entry,
                                             target.GUID.ToString(),
                                             model.CreatureDisplayId);
                        }
                        //if no param1, then use value from param2 (modelId)
                        else
                        {
                            target.AsCreature.SetDisplayId(e.Action.MorphOrMount.Model);

                            Log.Logger.Debug("SmartScript.ProcessAction. SMART_ACTION_MORPH_TO_ENTRY_OR_MODEL: Creature entry {0}, GuidLow {1} set displayid to {2}",
                                             target.Entry,
                                             target.GUID.ToString(),
                                             e.Action.MorphOrMount.Model);
                        }
                    }
                    else
                    {
                        target.AsCreature.DeMorph();

                        Log.Logger.Debug("SmartScript.ProcessAction. SMART_ACTION_MORPH_TO_ENTRY_OR_MODEL: Creature entry {0}, GuidLow {1} demorphs.",
                                         target.Entry,
                                         target.GUID.ToString());
                    }
                }

                break;
            }
            case SmartActions.FailQuest:
            {
                foreach (var target in targets)
                    if (IsPlayer(target))
                    {
                        target.AsPlayer.FailQuest(e.Action.Quest.QuestId);

                        Log.Logger.Debug("SmartScript.ProcessAction. SMART_ACTION_FAIL_QUEST: Player guidLow {0} fails quest {1}",
                                         target.GUID.ToString(),
                                         e.Action.Quest.QuestId);
                    }

                break;
            }
            case SmartActions.OfferQuest:
            {
                foreach (var target in targets)
                {
                    var player = target.AsPlayer;

                    if (player == null)
                        continue;

                    var quest = _objectManager.GetQuestTemplate(e.Action.QuestOffer.QuestId);

                    if (quest == null)
                        continue;

                    if (_me != null && e.Action.QuestOffer.DirectAdd == 0)
                    {
                        if (!player.CanTakeQuest(quest, true))
                            continue;

                        var session = player.Session;

                        if (session == null)
                            continue;

                        var menu = player.ClassFactory.Resolve<PlayerMenu>(new PositionalParameter(0, session));
                        menu.SendQuestGiverQuestDetails(quest, _me.GUID, true, false);
                        Log.Logger.Debug("SmartScript.ProcessAction:: SMART_ACTION_OFFER_QUEST: Player {0} - offering quest {1}", player.GUID.ToString(), e.Action.QuestOffer.QuestId);
                    }
                    else
                    {
                        player.AddQuestAndCheckCompletion(quest, null);
                        Log.Logger.Debug("SmartScript.ProcessAction: SMART_ACTION_ADD_QUEST: Player {0} add quest {1}", player.GUID.ToString(), e.Action.QuestOffer.QuestId);
                    }
                }

                break;
            }
            case SmartActions.SetReactState:
            {
                foreach (var target in targets.Where(target => IsCreature(target)))
                {
                    target.AsCreature.ReactState = (ReactStates)e.Action.React.State;
                }

                break;
            }
            case SmartActions.RandomEmote:
            {
                var randomEmote = e.Action.RandomEmote;

                List<uint> emotes = new[]
                    {
                        randomEmote.Emote1, randomEmote.Emote2, randomEmote.Emote3, randomEmote.Emote4, randomEmote.Emote5, randomEmote.Emote6,
                    }.Where(id => id != 0)
                     .ToList();

                foreach (var target in targets)
                    if (IsUnit(target))
                    {
                        var emote = emotes.SelectRandom();
                        target.AsUnit.HandleEmoteCommand((Emote)emote);

                        Log.Logger.Debug("SmartScript.ProcessAction. SMART_ACTION_RANDOM_EMOTE: Creature guidLow {0} handle random emote {1}",
                                         target.GUID.ToString(),
                                         emote);
                    }

                break;
            }
            case SmartActions.ThreatAllPct:
            {
                if (_me == null)
                    break;

                foreach (var refe in _me.GetThreatManager().GetModifiableThreatList())
                {
                    refe.ModifyThreatByPercent(Math.Max(-100, (int)(e.Action.ThreatPct.ThreatInc - e.Action.ThreatPct.ThreatDec)));
                    Log.Logger.Debug($"SmartScript.ProcessAction: SMART_ACTION_THREAT_ALL_PCT: Creature {_me.GUID} modify threat for {refe.Victim.GUID}, value {e.Action.ThreatPct.ThreatInc - e.Action.ThreatPct.ThreatDec}");
                }

                break;
            }
            case SmartActions.ThreatSinglePct:
            {
                if (_me == null)
                    break;

                foreach (var target in targets.Where(target => IsUnit(target)))
                {
                    _me.GetThreatManager().ModifyThreatByPercent(target.AsUnit, Math.Max(-100, (int)(e.Action.ThreatPct.ThreatInc - e.Action.ThreatPct.ThreatDec)));
                    Log.Logger.Debug($"SmartScript.ProcessAction: SMART_ACTION_THREAT_SINGLE_PCT: Creature {_me.GUID} modify threat for {target.GUID}, value {e.Action.ThreatPct.ThreatInc - e.Action.ThreatPct.ThreatDec}");
                }

                break;
            }
            case SmartActions.CallAreaexploredoreventhappens:
            {
                foreach (var target in targets)
                {
                    // Special handling for vehicles
                    if (IsUnit(target))
                    {
                        var vehicle = target.AsUnit.VehicleKit;

                        if (vehicle != null)
                            foreach (var seat in vehicle.Seats)
                            {
                                var player = _objectAccessor.GetPlayer(target, seat.Value.Passenger.Guid);

                                player?.AreaExploredOrEventHappens(e.Action.Quest.QuestId);
                            }
                    }

                    if (IsPlayer(target))
                    {
                        target.AsPlayer.AreaExploredOrEventHappens(e.Action.Quest.QuestId);

                        Log.Logger.Debug("SmartScript.ProcessAction. SMART_ACTION_CALL_AREAEXPLOREDOREVENTHAPPENS: {0} credited quest {1}",
                                         target.GUID.ToString(),
                                         e.Action.Quest.QuestId);
                    }
                }

                break;
            }
            case SmartActions.Cast:
            {
                if (e.Action.Cast.TargetsLimit > 0 && targets.Count > e.Action.Cast.TargetsLimit)
                    targets.RandomResize(e.Action.Cast.TargetsLimit);

                var failedSpellCast = false;
                var successfulSpellCast = false;

                foreach (var target in targets)
                {
                    _go?.SpellFactory.CastSpell(target.AsUnit, e.Action.Cast.Spell);

                    if (!IsUnit(target))
                        continue;

                    if (!e.Action.Cast.CastFlags.HasAnyFlag((uint)SmartCastFlags.AuraNotPresent) || !target.AsUnit.HasAura(e.Action.Cast.Spell))
                    {
                        var triggerFlag = TriggerCastFlags.None;

                        if (e.Action.Cast.CastFlags.HasAnyFlag((uint)SmartCastFlags.Triggered))
                        {
                            if (e.Action.Cast.TriggerFlags != 0)
                                triggerFlag = (TriggerCastFlags)e.Action.Cast.TriggerFlags;
                            else
                                triggerFlag = TriggerCastFlags.FullMask;
                        }

                        if (_me != null)
                        {
                            if (e.Action.Cast.CastFlags.HasAnyFlag((uint)SmartCastFlags.InterruptPrevious))
                                _me.InterruptNonMeleeSpells(false);

                            var result = _me.SpellFactory.CastSpell(target.AsUnit, e.Action.Cast.Spell, new CastSpellExtraArgs(triggerFlag));
                            var spellCastFailed = result != SpellCastResult.SpellCastOk && result != SpellCastResult.SpellInProgress;

                            if (e.Action.Cast.CastFlags.HasAnyFlag((uint)SmartCastFlags.CombatMove))
                                ((SmartAI)_me.AI).SetCombatMove(spellCastFailed, true);

                            if (spellCastFailed)
                                failedSpellCast = true;
                            else
                                successfulSpellCast = true;
                        }
                        else if (_go != null)
                        {
                            _go.SpellFactory.CastSpell(target.AsUnit, e.Action.Cast.Spell, new CastSpellExtraArgs(triggerFlag));
                        }
                    }
                    else
                    {
                        Log.Logger.Debug("Spell {0} not casted because it has Id SMARTCAST_AURA_NOT_PRESENT and the target (Guid: {1} Entry: {2} Type: {3}) already has the aura",
                                         e.Action.Cast.Spell,
                                         target.GUID,
                                         target.Entry,
                                         target.TypeId);
                    }
                }

                // If there is at least 1 failed cast and no successful casts at all, retry again on next loop
                if (failedSpellCast && !successfulSpellCast)
                {
                    RetryLater(e, true);

                    // Don't execute linked events
                    return;
                }

                break;
            }
            case SmartActions.SelfCast:
            {
                if (targets.Empty())
                    break;

                if (e.Action.Cast.TargetsLimit != 0)
                    targets.RandomResize(e.Action.Cast.TargetsLimit);

                var triggerFlags = TriggerCastFlags.None;

                if (e.Action.Cast.CastFlags.HasAnyFlag((uint)SmartCastFlags.Triggered))
                {
                    if (e.Action.Cast.TriggerFlags != 0)
                        triggerFlags = (TriggerCastFlags)e.Action.Cast.TriggerFlags;
                    else
                        triggerFlags = TriggerCastFlags.FullMask;
                }

                foreach (var target in targets)
                {
                    var uTarget = target.AsUnit;

                    if (uTarget == null)
                        continue;

                    if (!e.Action.Cast.CastFlags.HasAnyFlag((uint)SmartCastFlags.AuraNotPresent) || !uTarget.HasAura(e.Action.Cast.Spell))
                    {
                        if (e.Action.Cast.CastFlags.HasAnyFlag((uint)SmartCastFlags.InterruptPrevious))
                            uTarget.InterruptNonMeleeSpells(false);

                        uTarget.SpellFactory.CastSpell(uTarget, e.Action.Cast.Spell, new CastSpellExtraArgs(triggerFlags));
                    }
                }

                break;
            }
            case SmartActions.InvokerCast:
            {
                var tempLastInvoker = GetLastInvoker(unit);

                if (tempLastInvoker == null)
                    break;

                if (targets.Empty())
                    break;

                if (e.Action.Cast.TargetsLimit != 0)
                    targets.RandomResize(e.Action.Cast.TargetsLimit);

                foreach (var target in targets)
                {
                    if (!IsUnit(target))
                        continue;

                    if (!e.Action.Cast.CastFlags.HasAnyFlag((uint)SmartCastFlags.AuraNotPresent) || !target.AsUnit.HasAura(e.Action.Cast.Spell))
                    {
                        if (e.Action.Cast.CastFlags.HasAnyFlag((uint)SmartCastFlags.InterruptPrevious))
                            tempLastInvoker.InterruptNonMeleeSpells(false);

                        var triggerFlag = TriggerCastFlags.None;

                        if (e.Action.Cast.CastFlags.HasAnyFlag((uint)SmartCastFlags.Triggered))
                        {
                            if (e.Action.Cast.TriggerFlags != 0)
                                triggerFlag = (TriggerCastFlags)e.Action.Cast.TriggerFlags;
                            else
                                triggerFlag = TriggerCastFlags.FullMask;
                        }

                        tempLastInvoker.SpellFactory.CastSpell(target.AsUnit, e.Action.Cast.Spell, new CastSpellExtraArgs(triggerFlag));

                        Log.Logger.Debug("SmartScript.ProcessAction. SMART_ACTION_INVOKER_CAST: Invoker {0} casts spell {1} on target {2} with castflags {3}",
                                         tempLastInvoker.GUID.ToString(),
                                         e.Action.Cast.Spell,
                                         target.GUID.ToString(),
                                         e.Action.Cast.CastFlags);
                    }
                    else
                    {
                        Log.Logger.Debug("Spell {0} not cast because it has Id SMARTCAST_AURA_NOT_PRESENT and the target ({1}) already has the aura", e.Action.Cast.Spell, target.GUID.ToString());
                    }
                }

                break;
            }
            case SmartActions.ActivateGobject:
            {
                foreach (var target in targets)
                    if (IsGameObject(target))
                    {
                        // Activate
                        target. // Activate
                            AsGameObject.SetLootState(LootState.Ready);

                        target.AsGameObject.UseDoorOrButton(0, false, unit);

                        Log.Logger.Debug("SmartScript.ProcessAction. SMART_ACTION_ACTIVATE_GOBJECT. Gameobject {0} (entry: {1}) activated",
                                         target.GUID.ToString(),
                                         target.Entry);
                    }

                break;
            }
            case SmartActions.ResetGobject:
            {
                foreach (var target in targets)
                    if (IsGameObject(target))
                    {
                        target.AsGameObject.ResetDoorOrButton();

                        Log.Logger.Debug("SmartScript.ProcessAction. SMART_ACTION_RESET_GOBJECT. Gameobject {0} (entry: {1}) reset",
                                         target.GUID.ToString(),
                                         target.Entry);
                    }

                break;
            }
            case SmartActions.SetEmoteState:
            {
                foreach (var target in targets)
                    if (IsUnit(target))
                    {
                        target.AsUnit.EmoteState = (Emote)e.Action.Emote.EmoteId;

                        Log.Logger.Debug("SmartScript.ProcessAction. SMART_ACTION_SET_EMOTE_STATE. Unit {0} set emotestate to {1}",
                                         target.GUID.ToString(),
                                         e.Action.Emote.EmoteId);
                    }

                break;
            }
            case SmartActions.AutoAttack:
            {
                _me.CanMelee = e.Action.AutoAttack.Attack != 0;

                Log.Logger.Debug("SmartScript.ProcessAction. SMART_ACTION_AUTO_ATTACK: Creature: {0} bool on = {1}",
                                 _me.GUID.ToString(),
                                 e.Action.AutoAttack.Attack);

                break;
            }
            case SmartActions.AllowCombatMovement:
            {
                if (!IsSmart())
                    break;

                var move = e.Action.CombatMove.Move != 0;
                ((SmartAI)_me.AI).SetCombatMove(move);

                Log.Logger.Debug("SmartScript.ProcessAction. SMART_ACTION_ALLOW_COMBAT_MOVEMENT: Creature {0} bool on = {1}",
                                 _me.GUID.ToString(),
                                 e.Action.CombatMove.Move);

                break;
            }
            case SmartActions.SetEventPhase:
            {
                if (GetBaseObject() == null)
                    break;

                SetPhase(e.Action.SetEventPhase.Phase);

                Log.Logger.Debug("SmartScript.ProcessAction. SMART_ACTION_SET_EVENT_PHASE: Creature {0} set event phase {1}",
                                 GetBaseObject().GUID.ToString(),
                                 e.Action.SetEventPhase.Phase);

                break;
            }
            case SmartActions.IncEventPhase:
            {
                if (GetBaseObject() == null)
                    break;

                IncPhase(e.Action.IncEventPhase.Inc);
                DecPhase(e.Action.IncEventPhase.Dec);

                Log.Logger.Debug("SmartScript.ProcessAction. SMART_ACTION_INC_EVENT_PHASE: Creature {0} inc event phase by {1}, " +
                                 "decrease by {2}",
                                 GetBaseObject().GUID.ToString(),
                                 e.Action.IncEventPhase.Inc,
                                 e.Action.IncEventPhase.Dec);

                break;
            }
            case SmartActions.Evade:
            {
                if (_me == null)
                    break;

                // Reset home position to respawn position if specified in the parameters
                if (e.Action.Evade.ToRespawnPosition == 0)
                    _me.HomePosition = _me.RespawnPosition;

                _me.AI.EnterEvadeMode();
                Log.Logger.Debug("SmartScript.ProcessAction. SMART_ACTION_EVADE: Creature {0} EnterEvadeMode", _me.GUID.ToString());

                break;
            }
            case SmartActions.FleeForAssist:
            {
                if (_me == null)
                    break;

                _me.DoFleeToGetAssistance();

                if (e.Action.FleeAssist.WithEmote != 0)
                {
                    var builder = new BroadcastTextBuilder(_me, ChatMsg.MonsterEmote, (uint)BroadcastTextIds.FleeForAssist, _me.Gender);
                    _creatureTextManager.SendChatPacket(_me, builder, ChatMsg.Emote);
                }

                Log.Logger.Debug("SmartScript.ProcessAction. SMART_ACTION_FLEE_FOR_ASSIST: Creature {0} DoFleeToGetAssistance", _me.GUID.ToString());

                break;
            }
            case SmartActions.CallGroupeventhappens:
            {
                if (unit == null)
                    break;

                // If invoker was pet or charm
                var playerCharmed = unit.CharmerOrOwnerPlayerOrPlayerItself;

                if (playerCharmed != null && GetBaseObject() != null)
                {
                    playerCharmed.GroupEventHappens(e.Action.Quest.QuestId, GetBaseObject());

                    Log.Logger.Debug("SmartScript.ProcessAction: SMART_ACTION_CALL_GROUPEVENTHAPPENS: Player {0}, group credit for quest {1}",
                                     unit.GUID.ToString(),
                                     e.Action.Quest.QuestId);
                }

                // Special handling for vehicles
                var vehicle = unit.VehicleKit;

                if (vehicle != null)
                    foreach (var seat in vehicle.Seats)
                    {
                        var passenger = _objectAccessor.GetPlayer(unit, seat.Value.Passenger.Guid);

                        passenger?.GroupEventHappens(e.Action.Quest.QuestId, GetBaseObject());
                    }

                break;
            }
            case SmartActions.CombatStop:
            {
                if (_me == null)
                    break;

                _me.CombatStop(true);
                Log.Logger.Debug("SmartScript.ProcessAction: SMART_ACTION_COMBAT_STOP: {0} CombatStop", _me.GUID.ToString());

                break;
            }
            case SmartActions.RemoveAurasFromSpell:
            {
                foreach (var target in targets)
                {
                    if (!IsUnit(target))
                        continue;

                    if (e.Action.RemoveAura.Spell != 0)
                    {
                        ObjectGuid casterGUID = default;

                        if (e.Action.RemoveAura.OnlyOwnedAuras != 0)
                        {
                            if (_me == null)
                                break;

                            casterGUID = _me.GUID;
                        }

                        if (e.Action.RemoveAura.Charges != 0)
                        {
                            var aur = target.AsUnit.GetAura(e.Action.RemoveAura.Spell, casterGUID);

                            aur?.ModCharges(-(int)e.Action.RemoveAura.Charges, AuraRemoveMode.Expire);
                        }

                        target.AsUnit.RemoveAura(e.Action.RemoveAura.Spell);
                    }
                    else
                    {
                        target.AsUnit.RemoveAllAuras();
                    }

                    Log.Logger.Debug("SmartScript.ProcessAction: SMART_ACTION_REMOVEAURASFROMSPELL: Unit {0}, spell {1}",
                                     target.GUID.ToString(),
                                     e.Action.RemoveAura.Spell);
                }

                break;
            }
            case SmartActions.Follow:
            {
                if (!IsSmart())
                    break;

                if (targets.Empty())
                {
                    ((SmartAI)_me.AI).StopFollow(false);

                    break;
                }

                foreach (var target in targets)
                    if (IsUnit(target))
                    {
                        var angle = e.Action.Follow.Angle > 6 ? e.Action.Follow.Angle * (float)Math.PI / 180.0f : e.Action.Follow.Angle;
                        ((SmartAI)_me.AI).SetFollow(target.AsUnit, e.Action.Follow.Dist + 0.1f, angle, e.Action.Follow.Credit, e.Action.Follow.Entry, e.Action.Follow.CreditType);

                        Log.Logger.Debug("SmartScript.ProcessAction: SMART_ACTION_FOLLOW: Creature {0} following target {1}",
                                         _me.GUID.ToString(),
                                         target.GUID.ToString());

                        break;
                    }

                break;
            }
            case SmartActions.RandomPhase:
            {
                if (GetBaseObject() == null)
                    break;

                List<uint> phases = new();
                var randomPhase = e.Action.RandomPhase;

                foreach (var id in new[]
                         {
                             randomPhase.Phase1, randomPhase.Phase2, randomPhase.Phase3, randomPhase.Phase4, randomPhase.Phase5, randomPhase.Phase6
                         })
                    if (id != 0)
                        phases.Add(id);

                var phase = phases.SelectRandom();
                SetPhase(phase);

                Log.Logger.Debug("SmartScript.ProcessAction: SMART_ACTION_RANDOM_PHASE: Creature {0} sets event phase to {1}",
                                 GetBaseObject().GUID.ToString(),
                                 phase);

                break;
            }
            case SmartActions.RandomPhaseRange:
            {
                if (GetBaseObject() == null)
                    break;

                var phase = RandomHelper.URand(e.Action.RandomPhaseRange.PhaseMin, e.Action.RandomPhaseRange.PhaseMax);
                SetPhase(phase);

                Log.Logger.Debug("SmartScript.ProcessAction: SMART_ACTION_RANDOM_PHASE_RANGE: Creature {0} sets event phase to {1}",
                                 GetBaseObject().GUID.ToString(),
                                 phase);

                break;
            }
            case SmartActions.CallKilledmonster:
            {
                if (e.Target.Type is SmartTargets.None or SmartTargets.Self) // Loot recipient and his group members
                {
                    if (_me == null)
                        break;

                    foreach (var tapperGuid in _me.TapList)
                    {
                        var tapper = _objectAccessor.GetPlayer(_me, tapperGuid);

                        if (tapper != null)
                        {
                            tapper.KilledMonsterCredit(e.Action.KilledMonster.Creature, _me.GUID);
                            Log.Logger.Debug($"SmartScript::ProcessAction: SMART_ACTION_CALL_KILLEDMONSTER: Player {tapper.GUID}, Killcredit: {e.Action.KilledMonster.Creature}");
                        }
                    }
                }
                else // Specific target type
                {
                    foreach (var target in targets)
                        if (IsPlayer(target))
                        {
                            target.AsPlayer.KilledMonsterCredit(e.Action.KilledMonster.Creature);

                            Log.Logger.Debug("SmartScript.ProcessAction: SMART_ACTION_CALL_KILLEDMONSTER: Player {0}, Killcredit: {1}",
                                             target.GUID.ToString(),
                                             e.Action.KilledMonster.Creature);
                        }
                        else if (IsUnit(target)) // Special handling for vehicles
                        {
                            var vehicle = target.AsUnit.VehicleKit;

                            if (vehicle != null)
                                foreach (var seat in vehicle.Seats)
                                {
                                    var player = _objectAccessor.GetPlayer(target, seat.Value.Passenger.Guid);

                                    player?.KilledMonsterCredit(e.Action.KilledMonster.Creature);
                                }
                        }
                }

                break;
            }
            case SmartActions.SetInstData:
            {
                var obj = GetBaseObject() ?? unit;

                if (obj == null)
                    break;

                var instance = obj.Location.InstanceScript;

                if (instance == null)
                {
                    Log.Logger.Error("SmartScript: Event {0} attempt to set instance data without instance script. EntryOrGuid {1}", e.GetEventType(), e.EntryOrGuid);

                    break;
                }

                switch (e.Action.SetInstanceData.Type)
                {
                    case 0:
                        instance.SetData(e.Action.SetInstanceData.Field, e.Action.SetInstanceData.Data);

                        Log.Logger.Debug("SmartScript.ProcessAction: SMART_ACTION_SET_INST_DATA: SetData Field: {0}, data: {1}",
                                         e.Action.SetInstanceData.Field,
                                         e.Action.SetInstanceData.Data);

                        break;

                    case 1:
                        instance.SetBossState(e.Action.SetInstanceData.Field, (EncounterState)e.Action.SetInstanceData.Data);

                        Log.Logger.Debug("SmartScript.ProcessAction: SMART_ACTION_SET_INST_DATA: SetBossState BossId: {0}, State: {1} ({2})",
                                         e.Action.SetInstanceData.Field,
                                         e.Action.SetInstanceData.Data,
                                         (EncounterState)e.Action.SetInstanceData.Data);

                        break;
                }

                break;
            }
            case SmartActions.SetInstData64:
            {
                var obj = GetBaseObject();

                if (obj == null)
                    obj = unit;

                if (obj == null)
                    break;

                var instance = obj.Location.InstanceScript;

                if (instance == null)
                {
                    Log.Logger.Error("SmartScript: Event {0} attempt to set instance data without instance script. EntryOrGuid {1}", e.GetEventType(), e.EntryOrGuid);

                    break;
                }

                if (targets.Empty())
                    break;

                instance.SetGuidData(e.Action.SetInstanceData64.Field, targets.First().GUID);

                Log.Logger.Debug("SmartScript.ProcessAction: SMART_ACTION_SET_INST_DATA64: Field: {0}, data: {1}",
                                 e.Action.SetInstanceData64.Field,
                                 targets.First().GUID);

                break;
            }
            case SmartActions.UpdateTemplate:
            {
                foreach (var target in targets)
                    if (IsCreature(target))
                        target.AsCreature.UpdateEntry(e.Action.UpdateTemplate.Creature, null, e.Action.UpdateTemplate.UpdateLevel != 0);

                break;
            }
            case SmartActions.Die:
            {
                if (_me is { IsDead: false })
                {
                    _me.KillSelf();
                    Log.Logger.Debug("SmartScript.ProcessAction: SMART_ACTION_DIE: Creature {0}", _me.GUID.ToString());
                }

                break;
            }
            case SmartActions.SetInCombatWithZone:
            {
                if (_me is { IsAIEnabled: true })
                {
                    _me.AI.DoZoneInCombat();
                    Log.Logger.Debug($"SmartScript.ProcessAction: SMART_ACTION_SET_IN_COMBAT_WITH_ZONE: Creature: {_me.GUID}");
                }

                break;
            }
            case SmartActions.CallForHelp:
            {
                if (_me != null)
                {
                    _me.CallForHelp(e.Action.CallHelp.Range);

                    if (e.Action.CallHelp.WithEmote != 0)
                    {
                        var builder = new BroadcastTextBuilder(_me, ChatMsg.Emote, (uint)BroadcastTextIds.CallForHelp, _me.Gender);
                        _creatureTextManager.SendChatPacket(_me, builder, ChatMsg.MonsterEmote);
                    }

                    Log.Logger.Debug($"SmartScript.ProcessAction: SMART_ACTION_CALL_FOR_HELP: Creature: {_me.GUID}");
                }

                break;
            }
            case SmartActions.SetSheath:
            {
                if (_me != null)
                {
                    _me.Sheath = (SheathState)e.Action.SetSheath.Sheath;

                    Log.Logger.Debug("SmartScript.ProcessAction: SMART_ACTION_SET_SHEATH: Creature {0}, State: {1}",
                                     _me.GUID.ToString(),
                                     e.Action.SetSheath.Sheath);
                }

                break;
            }
            case SmartActions.ForceDespawn:
            {
                // there should be at least a world update tick before despawn, to avoid breaking linked actions
                var despawnDelay = TimeSpan.FromMilliseconds(e.Action.ForceDespawn.Delay);

                if (despawnDelay <= TimeSpan.Zero)
                    despawnDelay = TimeSpan.FromMilliseconds(1);

                var forceRespawnTimer = TimeSpan.FromSeconds(e.Action.ForceDespawn.ForceRespawnTimer);

                foreach (var target in targets)
                {
                    var creature = target.AsCreature;

                    if (creature != null)
                    {
                        creature.DespawnOrUnsummon(despawnDelay, forceRespawnTimer);
                    }
                    else
                    {
                        var go = target.AsGameObject;

                        go?.DespawnOrUnsummon(despawnDelay, forceRespawnTimer);
                    }
                }

                break;
            }
            case SmartActions.SetIngamePhaseId:
            {
                foreach (var target in targets)
                    if (e.Action.IngamePhaseId.Apply == 1)
                        _phasingHandler.AddPhase(target, e.Action.IngamePhaseId.ID, true);
                    else
                        _phasingHandler.RemovePhase(target, e.Action.IngamePhaseId.ID, true);

                break;
            }
            case SmartActions.SetIngamePhaseGroup:
            {
                foreach (var target in targets)
                    if (e.Action.IngamePhaseGroup.Apply == 1)
                        _phasingHandler.AddPhaseGroup(target, e.Action.IngamePhaseGroup.GroupId, true);
                    else
                        _phasingHandler.RemovePhaseGroup(target, e.Action.IngamePhaseGroup.GroupId, true);

                break;
            }
            case SmartActions.MountToEntryOrModel:
            {
                foreach (var target in targets)
                {
                    if (!IsUnit(target))
                        continue;

                    if (e.Action.MorphOrMount.Creature != 0 || e.Action.MorphOrMount.Model != 0)
                    {
                        if (e.Action.MorphOrMount.Creature > 0)
                        {
                            var cInfo = _objectManager.GetCreatureTemplate(e.Action.MorphOrMount.Creature);

                            if (cInfo != null)
                                target.AsUnit.Mount(_objectManager.ChooseDisplayId(cInfo).CreatureDisplayId);
                        }
                        else
                        {
                            target.AsUnit.Mount(e.Action.MorphOrMount.Model);
                        }
                    }
                    else
                    {
                        target.AsUnit.Dismount();
                    }
                }

                break;
            }
            case SmartActions.SetInvincibilityHpLevel:
            {
                foreach (var target in targets)
                    if (IsCreature(target))
                    {
                        var ai = (SmartAI)_me.AI;

                        if (ai == null)
                            continue;

                        if (e.Action.InvincHp.Percent != 0)
                            ai.SetInvincibilityHpLevel((uint)target.AsCreature.CountPctFromMaxHealth((int)e.Action.InvincHp.Percent));
                        else
                            ai.SetInvincibilityHpLevel(e.Action.InvincHp.MinHp);
                    }

                break;
            }
            case SmartActions.SetData:
            {
                foreach (var target in targets)
                {
                    var cTarget = target.AsCreature;

                    if (cTarget != null)
                    {
                        var ai = cTarget.AI;

                        if (IsSmart(cTarget, true))
                            ((SmartAI)ai).SetData(e.Action.SetData.Field, e.Action.SetData.Data, _me);
                        else
                            ai.SetData(e.Action.SetData.Field, e.Action.SetData.Data);
                    }
                    else
                    {
                        var oTarget = target.AsGameObject;

                        if (oTarget != null)
                        {
                            var ai = oTarget.AI;

                            if (IsSmart(oTarget, true))
                                ((SmartGameObjectAI)ai).SetData(e.Action.SetData.Field, e.Action.SetData.Data, _me);
                            else
                                ai.SetData(e.Action.SetData.Field, e.Action.SetData.Data);
                        }
                    }
                }

                break;
            }
            case SmartActions.AttackStop:
            {
                foreach (var target in targets)
                {
                    var unitTarget = target.AsUnit;

                    unitTarget?.AttackStop();
                }

                break;
            }
            case SmartActions.MoveOffset:
            {
                foreach (var target in targets)
                {
                    if (!IsCreature(target))
                        continue;

                    if (!e.Event.EventFlags.HasAnyFlag(SmartEventFlags.WhileCharmed) && IsCharmedCreature(target))
                        continue;

                    Position pos = target.Location;

                    // Use forward/backward/left/right cartesian plane movement
                    var o = pos.Orientation;
                    var x = (float)(pos.X + Math.Cos(o - Math.PI / 2) * e.Target.X + Math.Cos(o) * e.Target.Y);
                    var y = (float)(pos.Y + Math.Sin(o - Math.PI / 2) * e.Target.X + Math.Sin(o) * e.Target.Y);
                    var z = pos.Z + e.Target.Z;
                    target.AsCreature.MotionMaster.MovePoint(e.Action.MoveOffset.PointId, x, y, z);
                }

                break;
            }
            case SmartActions.SetVisibility:
            {
                foreach (var target in targets)
                    if (IsUnit(target))
                        target.AsUnit.SetVisible(e.Action.Visibility.State != 0);

                break;
            }
            case SmartActions.SetActive:
            {
                foreach (var target in targets)
                    target.SetActive(e.Action.Active.State != 0);

                break;
            }
            case SmartActions.AttackStart:
            {
                if (_me == null)
                    break;

                if (targets.Empty())
                    break;

                var target = targets.SelectRandom().AsUnit;

                if (target != null)
                    _me.AI.AttackStart(target);

                break;
            }
            case SmartActions.SummonCreature:
            {
                var flags = (SmartActionSummonCreatureFlags)e.Action.SummonCreature.Flags;
                var preferUnit = flags.HasAnyFlag(SmartActionSummonCreatureFlags.PreferUnit);
                var summoner = preferUnit ? unit : GetBaseObjectOrUnitInvoker(unit);

                if (summoner == null)
                    break;

                var privateObjectOwner = ObjectGuid.Empty;

                if (flags.HasAnyFlag(SmartActionSummonCreatureFlags.PersonalSpawn))
                    privateObjectOwner = summoner.IsPrivateObject ? summoner.PrivateObjectOwner : summoner.GUID;

                var spawnsCount = Math.Max(e.Action.SummonCreature.Count, 1u);

                foreach (var target in targets)
                {
                    var pos = target.Location.Copy();
                    pos.X += e.Target.X;
                    pos.Y += e.Target.Y;
                    pos.Z += e.Target.Z;
                    pos.Orientation += e.Target.O;

                    for (uint counter = 0; counter < spawnsCount; counter++)
                    {
                        Creature summon = summoner.SummonCreature(e.Action.SummonCreature.Creature, pos, (TempSummonType)e.Action.SummonCreature.Type, TimeSpan.FromMilliseconds(e.Action.SummonCreature.Duration), 0, 0, privateObjectOwner);

                        if (summon != null)
                            if (e.Action.SummonCreature.AttackInvoker != 0)
                                summon.AI.AttackStart(target.AsUnit);
                    }
                }

                if (e.GetTargetType() != SmartTargets.Position)
                    break;

                for (uint counter = 0; counter < spawnsCount; counter++)
                {
                    Creature summon = summoner.SummonCreature(e.Action.SummonCreature.Creature, new Position(e.Target.X, e.Target.Y, e.Target.Z, e.Target.O), (TempSummonType)e.Action.SummonCreature.Type, TimeSpan.FromMilliseconds(e.Action.SummonCreature.Duration), 0, 0, privateObjectOwner);

                    if (summon != null)
                        if (unit != null && e.Action.SummonCreature.AttackInvoker != 0)
                            summon.AI.AttackStart(unit);
                }

                break;
            }
            case SmartActions.SummonGo:
            {
                var summoner = GetBaseObjectOrUnitInvoker(unit);

                if (summoner == null)
                    break;

                foreach (var target in targets)
                {
                    var pos = target.Location.GetPositionWithOffset(new Position(e.Target.X, e.Target.Y, e.Target.Z, e.Target.O));
                    var rotation = Quaternion.CreateFromRotationMatrix(Extensions.fromEulerAnglesZYX(pos.Orientation, 0f, 0f));
                    summoner.SummonGameObject(e.Action.SummonGO.Entry, pos, rotation, TimeSpan.FromSeconds(e.Action.SummonGO.DespawnTime), (GameObjectSummonType)e.Action.SummonGO.SummonType);
                }

                if (e.GetTargetType() != SmartTargets.Position)
                    break;

                var rot = Quaternion.CreateFromRotationMatrix(Extensions.fromEulerAnglesZYX(e.Target.O, 0f, 0f));
                summoner.SummonGameObject(e.Action.SummonGO.Entry, new Position(e.Target.X, e.Target.Y, e.Target.Z, e.Target.O), rot, TimeSpan.FromSeconds(e.Action.SummonGO.DespawnTime), (GameObjectSummonType)e.Action.SummonGO.SummonType);

                break;
            }
            case SmartActions.KillUnit:
            {
                foreach (var target in targets)
                {
                    if (!IsUnit(target))
                        continue;

                    target.AsUnit.KillSelf();
                }

                break;
            }
            case SmartActions.AddItem:
            {
                foreach (var target in targets)
                {
                    if (!IsPlayer(target))
                        continue;

                    target.AsPlayer.AddItem(e.Action.Item.Entry, e.Action.Item.Count);
                }

                break;
            }
            case SmartActions.RemoveItem:
            {
                foreach (var target in targets)
                {
                    if (!IsPlayer(target))
                        continue;

                    target.AsPlayer.DestroyItemCount(e.Action.Item.Entry, e.Action.Item.Count, true);
                }

                break;
            }
            case SmartActions.StoreTargetList:
            {
                StoreTargetList(targets, e.Action.StoreTargets.ID);

                break;
            }
            case SmartActions.Teleport:
            {
                foreach (var target in targets)
                    if (IsPlayer(target))
                        target.AsPlayer.TeleportTo(e.Action.Teleport.MapID, e.Target.X, e.Target.Y, e.Target.Z, e.Target.O);
                    else if (IsCreature(target))
                        target.AsCreature.NearTeleportTo(e.Target.X, e.Target.Y, e.Target.Z, e.Target.O);

                break;
            }
            case SmartActions.SetDisableGravity:
            {
                if (!IsSmart())
                    break;

                ((SmartAI)_me.AI).SetDisableGravity(e.Action.SetDisableGravity.Disable != 0);

                break;
            }
            case SmartActions.SetRun:
            {
                if (!IsSmart())
                    break;

                ((SmartAI)_me.AI).SetRun(e.Action.SetRun.Run != 0);

                break;
            }
            case SmartActions.SetCounter:
            {
                if (!targets.Empty())
                {
                    foreach (var target in targets)
                        if (IsCreature(target))
                        {
                            var ai = (SmartAI)target.AsCreature.AI;

                            if (ai != null)
                                ai.GetScript().StoreCounter(e.Action.SetCounter.CounterId, e.Action.SetCounter.Value, e.Action.SetCounter.Reset);
                            else
                                Log.Logger.Error("SmartScript: Action target for SMART_ACTION_SET_COUNTER is not using SmartAI, skipping");
                        }
                        else if (IsGameObject(target))
                        {
                            var ai = (SmartGameObjectAI)target.AsGameObject.AI;

                            if (ai != null)
                                ai.GetScript().StoreCounter(e.Action.SetCounter.CounterId, e.Action.SetCounter.Value, e.Action.SetCounter.Reset);
                            else
                                Log.Logger.Error("SmartScript: Action target for SMART_ACTION_SET_COUNTER is not using SmartGameObjectAI, skipping");
                        }
                }
                else
                {
                    StoreCounter(e.Action.SetCounter.CounterId, e.Action.SetCounter.Value, e.Action.SetCounter.Reset);
                }

                break;
            }
            case SmartActions.WpStart:
            {
                if (!IsSmart())
                    break;

                var run = e.Action.WpStart.Run != 0;
                var entry = e.Action.WpStart.PathID;
                var repeat = e.Action.WpStart.Repeat != 0;

                foreach (var target in targets)
                    if (IsPlayer(target))
                    {
                        StoreTargetList(targets, SharedConst.SmartEscortTargets);

                        break;
                    }

                _me.GetAI<SmartAI>().StartPath(run, entry, repeat, unit);

                var quest = e.Action.WpStart.QuestId;
                var despawnTime = e.Action.WpStart.DespawnTime;
                _me.GetAI<SmartAI>().EscortQuestID = quest;
                _me.GetAI<SmartAI>().SetDespawnTime(despawnTime);

                break;
            }
            case SmartActions.WpPause:
            {
                if (!IsSmart())
                    break;

                var delay = e.Action.WpPause.Delay;
                ((SmartAI)_me.AI).PausePath(delay, true);

                break;
            }
            case SmartActions.WpStop:
            {
                if (!IsSmart())
                    break;

                var despawnTime = e.Action.WpStop.DespawnTime;
                var quest = e.Action.WpStop.QuestId;
                var fail = e.Action.WpStop.Fail != 0;
                ((SmartAI)_me.AI).StopPath(despawnTime, quest, fail);

                break;
            }
            case SmartActions.WpResume:
            {
                if (!IsSmart())
                    break;

                // Set the timer to 1 ms so the path will be resumed on next update loop
                if (_me.GetAI<SmartAI>().CanResumePath())
                    _me.GetAI<SmartAI>().SetWpPauseTimer(1);

                break;
            }
            case SmartActions.SetOrientation:
            {
                if (_me == null)
                    break;

                if (e.GetTargetType() == SmartTargets.Self)
                    _me.SetFacingTo((_me.Transport != null ? _me.TransportHomePosition : _me.HomePosition).Orientation);
                else if (e.GetTargetType() == SmartTargets.Position)
                    _me.SetFacingTo(e.Target.O);
                else if (!targets.Empty())
                    _me.SetFacingToObject(targets.First());

                break;
            }
            case SmartActions.Playmovie:
            {
                foreach (var target in targets)
                {
                    if (!IsPlayer(target))
                        continue;

                    target.AsPlayer.SendMovieStart(e.Action.Movie.Entry);
                }

                break;
            }
            case SmartActions.MoveToPos:
            {
                if (!IsSmart())
                    break;

                WorldObject target = null;

                /*if (e.GetTargetType() == SmartTargets.CreatureRange || e.GetTargetType() == SmartTargets.CreatureGuid ||
                    e.GetTargetType() == SmartTargets.CreatureDistance || e.GetTargetType() == SmartTargets.GameobjectRange ||
                    e.GetTargetType() == SmartTargets.GameobjectGuid || e.GetTargetType() == SmartTargets.GameobjectDistance ||
                    e.GetTargetType() == SmartTargets.ClosestCreature || e.GetTargetType() == SmartTargets.ClosestGameobject ||
                    e.GetTargetType() == SmartTargets.OwnerOrSummoner || e.GetTargetType() == SmartTargets.ActionInvoker ||
                    e.GetTargetType() == SmartTargets.ClosestEnemy || e.GetTargetType() == SmartTargets.ClosestFriendly)*/
                {
                    // we want to move to random element
                    if (!targets.Empty())
                        target = targets.SelectRandom();
                }

                if (target == null)
                {
                    Position dest = new(e.Target.X, e.Target.Y, e.Target.Z);

                    if (e.Action.MoveToPos.Transport != 0)
                    {
                        var trans = _me.DirectTransport;

                        trans?.CalculatePassengerPosition(dest);
                    }

                    _me.MotionMaster.MovePoint(e.Action.MoveToPos.PointId, dest, e.Action.MoveToPos.DisablePathfinding == 0);
                }
                else
                {
                    var pos = target.Location.Copy();

                    if (e.Action.MoveToPos.ContactDistance > 0)
                        target.Location.GetContactPoint(_me, pos, e.Action.MoveToPos.ContactDistance);

                    _me.MotionMaster.MovePoint(e.Action.MoveToPos.PointId, pos.X + e.Target.X, pos.Y + e.Target.Y, pos.Z + e.Target.Z, e.Action.MoveToPos.DisablePathfinding == 0);
                }

                break;
            }
            case SmartActions.EnableTempGobj:
            {
                foreach (var target in targets)
                    if (IsCreature(target))
                    {
                        Log.Logger.Warning($"Invalid creature target '{target.GetName()}' (entry {target.Entry}, spawnId {target.AsCreature.SpawnId}) specified for SMART_ACTION_ENABLE_TEMP_GOBJ");
                    }
                    else if (IsGameObject(target))
                    {
                        if (target.AsGameObject.IsSpawnedByDefault)
                            Log.Logger.Warning($"Invalid gameobject target '{target.GetName()}' (entry {target.Entry}, spawnId {target.AsGameObject.SpawnId}) for SMART_ACTION_ENABLE_TEMP_GOBJ - the object is spawned by default");
                        else
                            target.AsGameObject.SetRespawnTime((int)e.Action.EnableTempGO.Duration);
                    }

                break;
            }
            case SmartActions.CloseGossip:
            {
                foreach (var target in targets)
                    if (IsPlayer(target))
                        target.AsPlayer.PlayerTalkClass.SendCloseGossip();

                break;
            }
            case SmartActions.Equip:
            {
                foreach (var target in targets)
                {
                    var npc = target.AsCreature;

                    if (npc != null)
                    {
                        var slot = new EquipmentItem[SharedConst.MaxEquipmentItems];
                        var equipId = (sbyte)e.Action.Equip.Entry;

                        if (equipId != 0)
                        {
                            var eInfo = _objectManager.GetEquipmentInfo(npc.Entry, equipId);

                            if (eInfo == null)
                            {
                                Log.Logger.Error("SmartScript: SMART_ACTION_EQUIP uses non-existent equipment info id {0} for creature {1}", equipId, npc.Entry);

                                break;
                            }

                            npc.CurrentEquipmentId = (byte)equipId;
                            Array.Copy(eInfo.Items, slot, SharedConst.MaxEquipmentItems);
                        }
                        else
                        {
                            slot[0].ItemId = e.Action.Equip.Slot1;
                            slot[1].ItemId = e.Action.Equip.Slot2;
                            slot[2].ItemId = e.Action.Equip.Slot3;
                        }

                        for (uint i = 0; i < SharedConst.MaxEquipmentItems; ++i)
                            if (e.Action.Equip.Mask == 0 || (e.Action.Equip.Mask & (1 << (int)i)) != 0)
                                npc.SetVirtualItem(i, slot[i].ItemId, slot[i].AppearanceModId, slot[i].ItemVisual);
                    }
                }

                break;
            }
            case SmartActions.CreateTimedEvent:
            {
                SmartEvent ne = new()
                {
                    Type = SmartEvents.Update,
                    EventChance = e.Action.TimeEvent.Chance
                };

                if (ne.EventChance == 0)
                    ne.EventChance = 100;

                ne.MinMaxRepeat.Min = e.Action.TimeEvent.Min;
                ne.MinMaxRepeat.Max = e.Action.TimeEvent.Max;
                ne.MinMaxRepeat.RepeatMin = e.Action.TimeEvent.RepeatMin;
                ne.MinMaxRepeat.RepeatMax = e.Action.TimeEvent.RepeatMax;

                ne.EventFlags = 0;

                if (ne.MinMaxRepeat is { RepeatMin: 0, RepeatMax: 0 })
                    ne.EventFlags |= SmartEventFlags.NotRepeatable;

                SmartAction ac = new()
                {
                    Type = SmartActions.TriggerTimedEvent
                };

                ac.TimeEvent.ID = e.Action.TimeEvent.ID;

                SmartScriptHolder ev = new()
                {
                    Event = ne,
                    EventId = e.Action.TimeEvent.ID,
                    Target = e.Target,
                    Action = ac
                };

                InitTimer(ev);
                _storedEvents.Add(ev);

                break;
            }
            case SmartActions.TriggerTimedEvent:
                ProcessEventsFor(SmartEvents.TimedEventTriggered, null, e.Action.TimeEvent.ID);

                // remove this event if not repeatable
                if (e.Event.EventFlags.HasAnyFlag(SmartEventFlags.NotRepeatable))
                    _remIDs.Add(e.Action.TimeEvent.ID);

                break;

            case SmartActions.RemoveTimedEvent:
                _remIDs.Add(e.Action.TimeEvent.ID);

                break;

            case SmartActions.CallScriptReset:
                SetPhase(0);
                OnReset();

                break;

            case SmartActions.SetRangedMovement:
            {
                if (!IsSmart())
                    break;

                float attackDistance = e.Action.SetRangedMovement.Distance;
                var attackAngle = e.Action.SetRangedMovement.Angle / 180.0f * MathFunctions.PI;

                foreach (var target in targets)
                {
                    var creature = target.AsCreature;

                    if (creature != null)
                        if (IsSmart(creature) && creature.Victim != null)
                            if (((SmartAI)creature.AI).CanCombatMove())
                                creature.MotionMaster.MoveChase(creature.Victim, attackDistance, attackAngle);
                }

                break;
            }
            case SmartActions.CallTimedActionlist:
            {
                if (e.GetTargetType() == SmartTargets.None)
                {
                    Log.Logger.Error("SmartScript: Entry {0} SourceType {1} Event {2} Action {3} is using TARGET_NONE(0) for Script9 target. Please correct target_type in database.", e.EntryOrGuid, e.GetScriptType(), e.GetEventType(), e.GetActionType());

                    break;
                }

                foreach (var target in targets)
                {
                    var creature = target.AsCreature;

                    if (creature != null)
                    {
                        if (IsSmart(creature))
                            creature.GetAI<SmartAI>().SetTimedActionList(e, e.Action.TimedActionList.ID, GetLastInvoker());
                    }
                    else
                    {
                        var go = target.AsGameObject;

                        if (go != null)
                        {
                            if (IsSmart(go))
                                go.GetAI<SmartGameObjectAI>().SetTimedActionList(e, e.Action.TimedActionList.ID, GetLastInvoker());
                        }
                        else
                        {
                            var areaTriggerTarget = target.AsAreaTrigger;

                            areaTriggerTarget?.ForEachAreaTriggerScript<IAreaTriggerSmartScript>(a => a.SetTimedActionList(e, e.Action.TimedActionList.ID, GetLastInvoker()));
                        }
                    }
                }

                break;
            }
            case SmartActions.SetNpcFlag:
            {
                foreach (var target in targets)
                    if (IsUnit(target))
                        target.AsUnit.ReplaceAllNpcFlags((NPCFlags)e.Action.Flag.Id);

                break;
            }
            case SmartActions.AddNpcFlag:
            {
                foreach (var target in targets)
                    if (IsUnit(target))
                        target.AsUnit.SetNpcFlag((NPCFlags)e.Action.Flag.Id);

                break;
            }
            case SmartActions.RemoveNpcFlag:
            {
                foreach (var target in targets)
                    if (IsUnit(target))
                        target.AsUnit.RemoveNpcFlag((NPCFlags)e.Action.Flag.Id);

                break;
            }
            case SmartActions.CrossCast:
            {
                if (targets.Empty())
                    break;

                var casters = GetTargets(CreateSmartEvent(SmartEvents.UpdateIc, 0, 0, 0, 0, 0, 0, SmartActions.None, 0, 0, 0, 0, 0, 0, 0, (SmartTargets)e.Action.CrossCast.TargetType, e.Action.CrossCast.TargetParam1, e.Action.CrossCast.TargetParam2, e.Action.CrossCast.TargetParam3, 0, 0), unit);

                foreach (var caster in casters)
                {
                    if (!IsUnit(caster))
                        continue;

                    var casterUnit = caster.AsUnit;
                    var interruptedSpell = false;

                    foreach (var target in targets)
                    {
                        if (!IsUnit(target))
                            continue;

                        if (!e.Action.CrossCast.CastFlags.HasAnyFlag((uint)SmartCastFlags.AuraNotPresent) || !target.AsUnit.HasAura(e.Action.CrossCast.Spell))
                        {
                            if (!interruptedSpell && e.Action.CrossCast.CastFlags.HasAnyFlag((uint)SmartCastFlags.InterruptPrevious))
                            {
                                casterUnit.InterruptNonMeleeSpells(false);
                                interruptedSpell = true;
                            }

                            casterUnit.SpellFactory.CastSpell(target.AsUnit, e.Action.CrossCast.Spell, e.Action.CrossCast.CastFlags.HasAnyFlag((uint)SmartCastFlags.Triggered));
                        }
                        else
                        {
                            Log.Logger.Debug("Spell {0} not cast because it has Id SMARTCAST_AURA_NOT_PRESENT and the target ({1}) already has the aura", e.Action.CrossCast.Spell, target.GUID.ToString());
                        }
                    }
                }

                break;
            }
            case SmartActions.CallRandomTimedActionlist:
            {
                List<uint> actionLists = new();
                var randTimedActionList = e.Action.RandTimedActionList;

                foreach (var id in new[]
                         {
                             randTimedActionList.ActionList1, randTimedActionList.ActionList2, randTimedActionList.ActionList3, randTimedActionList.ActionList4, randTimedActionList.ActionList5, randTimedActionList.ActionList6
                         })
                    if (id != 0)
                        actionLists.Add(id);

                if (e.GetTargetType() == SmartTargets.None)
                {
                    Log.Logger.Error("SmartScript: Entry {0} SourceType {1} Event {2} Action {3} is using TARGET_NONE(0) for Script9 target. Please correct target_type in database.", e.EntryOrGuid, e.GetScriptType(), e.GetEventType(), e.GetActionType());

                    break;
                }

                var randomId = actionLists.SelectRandom();

                foreach (var target in targets)
                {
                    var creature = target.AsCreature;

                    if (creature != null)
                    {
                        if (IsSmart(creature))
                            creature.GetAI<SmartAI>().SetTimedActionList(e, randomId, GetLastInvoker());
                    }
                    else
                    {
                        var go = target.AsGameObject;

                        if (go != null)
                        {
                            if (IsSmart(go))
                                go.GetAI<SmartGameObjectAI>().SetTimedActionList(e, randomId, GetLastInvoker());
                        }
                        else
                        {
                            var areaTriggerTarget = target.AsAreaTrigger;

                            areaTriggerTarget?.ForEachAreaTriggerScript<IAreaTriggerSmartScript>(a => a.SetTimedActionList(e, randomId, GetLastInvoker()));
                        }
                    }
                }

                break;
            }
            case SmartActions.CallRandomRangeTimedActionlist:
            {
                var id = RandomHelper.URand(e.Action.RandRangeTimedActionList.IDMin, e.Action.RandRangeTimedActionList.IDMax);

                if (e.GetTargetType() == SmartTargets.None)
                {
                    Log.Logger.Error("SmartScript: Entry {0} SourceType {1} Event {2} Action {3} is using TARGET_NONE(0) for Script9 target. Please correct target_type in database.", e.EntryOrGuid, e.GetScriptType(), e.GetEventType(), e.GetActionType());

                    break;
                }

                foreach (var target in targets)
                {
                    var creature = target.AsCreature;

                    if (creature != null)
                    {
                        if (IsSmart(creature))
                            creature.GetAI<SmartAI>().SetTimedActionList(e, id, GetLastInvoker());
                    }
                    else
                    {
                        var go = target.AsGameObject;

                        if (go != null)
                        {
                            if (IsSmart(go))
                                go.GetAI<SmartGameObjectAI>().SetTimedActionList(e, id, GetLastInvoker());
                        }
                        else
                        {
                            var areaTriggerTarget = target.AsAreaTrigger;

                            areaTriggerTarget?.ForEachAreaTriggerScript<IAreaTriggerSmartScript>(a => a.SetTimedActionList(e, id, GetLastInvoker()));
                        }
                    }
                }

                break;
            }
            case SmartActions.ActivateTaxi:
            {
                foreach (var target in targets)
                    if (IsPlayer(target))
                        target.AsPlayer.ActivateTaxiPathTo(e.Action.Taxi.ID);

                break;
            }
            case SmartActions.RandomMove:
            {
                var foundTarget = false;

                foreach (var obj in targets)
                    if (IsCreature(obj))
                    {
                        if (e.Action.MoveRandom.Distance != 0)
                            obj.AsCreature.MotionMaster.MoveRandom(e.Action.MoveRandom.Distance);
                        else
                            obj.AsCreature.MotionMaster.MoveIdle();
                    }

                if (!foundTarget && _me != null && IsCreature(_me))
                {
                    if (e.Action.MoveRandom.Distance != 0)
                        _me.MotionMaster.MoveRandom(e.Action.MoveRandom.Distance);
                    else
                        _me.MotionMaster.MoveIdle();
                }

                break;
            }
            case SmartActions.SetUnitFieldBytes1:
            {
                foreach (var target in targets)
                    if (IsUnit(target))
                        switch (e.Action.SetunitByte.Type)
                        {
                            case 0:
                                target.AsUnit.SetStandState((UnitStandStateType)e.Action.SetunitByte.Byte1);

                                break;

                            case 1:
                                // pet talent points
                                break;

                            case 2:
                                target.AsUnit.SetVisFlag((UnitVisFlags)e.Action.SetunitByte.Byte1);

                                break;

                            case 3:
                                target.AsUnit.SetAnimTier((AnimTier)e.Action.SetunitByte.Byte1);

                                break;
                        }

                break;
            }
            case SmartActions.RemoveUnitFieldBytes1:
            {
                foreach (var target in targets)
                    if (IsUnit(target))
                        switch (e.Action.SetunitByte.Type)
                        {
                            case 0:
                                target.AsUnit.SetStandState(UnitStandStateType.Stand);

                                break;

                            case 1:
                                // pet talent points
                                break;

                            case 2:
                                target.AsUnit.RemoveVisFlag((UnitVisFlags)e.Action.SetunitByte.Byte1);

                                break;

                            case 3:
                                target.AsUnit.SetAnimTier(AnimTier.Ground);

                                break;
                        }

                break;
            }
            case SmartActions.InterruptSpell:
            {
                foreach (var target in targets)
                    if (IsUnit(target))
                        target.AsUnit.InterruptNonMeleeSpells(e.Action.InterruptSpellCasting.WithDelayed != 0, e.Action.InterruptSpellCasting.SpellID, e.Action.InterruptSpellCasting.WithInstant != 0);

                break;
            }
            case SmartActions.AddDynamicFlag:
            {
                foreach (var target in targets)
                    if (IsUnit(target))
                        target.AsUnit.SetDynamicFlag((UnitDynFlags)e.Action.Flag.Id);

                break;
            }
            case SmartActions.RemoveDynamicFlag:
            {
                foreach (var target in targets)
                    if (IsUnit(target))
                        target.AsUnit.RemoveDynamicFlag((UnitDynFlags)e.Action.Flag.Id);

                break;
            }
            case SmartActions.JumpToPos:
            {
                WorldObject target = null;

                if (!targets.Empty())
                    target = targets.SelectRandom();

                Position pos = new(e.Target.X, e.Target.Y, e.Target.Z);

                if (target != null)
                {
                    var tpos = target.Location.Copy();

                    if (e.Action.Jump.ContactDistance > 0)
                        target.Location.GetContactPoint(_me, tpos, e.Action.Jump.ContactDistance);

                    pos = new Position(tpos.X + e.Target.X, tpos.Y + e.Target.Y, tpos.Z + e.Target.Z);
                }

                if (e.Action.Jump.Gravity != 0 || e.Action.Jump.UseDefaultGravity != 0)
                {
                    var gravity = e.Action.Jump.UseDefaultGravity != 0 ? (float)MotionMaster.GRAVITY : e.Action.Jump.Gravity;
                    _me.MotionMaster.MoveJumpWithGravity(pos, e.Action.Jump.SpeedXy, gravity, e.Action.Jump.PointId);
                }
                else
                {
                    _me.MotionMaster.MoveJump(pos, e.Action.Jump.SpeedXy, e.Action.Jump.SpeedZ, e.Action.Jump.PointId);
                }

                break;
            }
            case SmartActions.GoSetLootState:
            {
                foreach (var target in targets)
                    if (IsGameObject(target))
                        target.AsGameObject.SetLootState((LootState)e.Action.SetGoLootState.State);

                break;
            }
            case SmartActions.GoSetGoState:
            {
                foreach (var target in targets)
                    if (IsGameObject(target))
                        target.AsGameObject.SetGoState((GameObjectState)e.Action.GoState.State);

                break;
            }
            case SmartActions.SendTargetToTarget:
            {
                var baseObject = GetBaseObject();

                if (baseObject == null)
                    baseObject = unit;

                if (baseObject == null)
                    break;

                var storedTargets = GetStoredTargetList(e.Action.SendTargetToTarget.ID, baseObject);

                if (storedTargets == null)
                    break;

                foreach (var target in targets)
                    if (IsCreature(target))
                    {
                        var ai = (SmartAI)target.AsCreature.AI;

                        if (ai != null)
                            ai.GetScript().StoreTargetList(new List<WorldObject>(storedTargets), e.Action.SendTargetToTarget.ID); // store a copy of target list
                        else
                            Log.Logger.Error("SmartScript: Action target for SMART_ACTION_SEND_TARGET_TO_TARGET is not using SmartAI, skipping");
                    }
                    else if (IsGameObject(target))
                    {
                        var ai = (SmartGameObjectAI)target.AsGameObject.AI;

                        if (ai != null)
                            ai.GetScript().StoreTargetList(new List<WorldObject>(storedTargets), e.Action.SendTargetToTarget.ID); // store a copy of target list
                        else
                            Log.Logger.Error("SmartScript: Action target for SMART_ACTION_SEND_TARGET_TO_TARGET is not using SmartGameObjectAI, skipping");
                    }

                break;
            }
            case SmartActions.SendGossipMenu:
            {
                if (GetBaseObject() == null || !IsSmart())
                    break;

                Log.Logger.Debug("SmartScript.ProcessAction. SMART_ACTION_SEND_GOSSIP_MENU: gossipMenuId {0}, gossipNpcTextId {1}",
                                 e.Action.SendGossipMenu.GossipMenuId,
                                 e.Action.SendGossipMenu.GossipNpcTextId);

                // override default gossip
                if (_me != null)
                    ((SmartAI)_me.AI).SetGossipReturn(true);
                else if (_go != null)
                    ((SmartGameObjectAI)_go.AI).SetGossipReturn(true);

                foreach (var target in targets)
                {
                    var player = target.AsPlayer;

                    if (player == null)
                        continue;

                    if (e.Action.SendGossipMenu.GossipMenuId != 0)
                        player.PrepareGossipMenu(GetBaseObject(), e.Action.SendGossipMenu.GossipMenuId, true);
                    else
                        player.PlayerTalkClass.ClearMenus();

                    var gossipNpcTextId = e.Action.SendGossipMenu.GossipNpcTextId;

                    if (gossipNpcTextId == 0)
                        gossipNpcTextId = player.GetGossipTextId(e.Action.SendGossipMenu.GossipMenuId, GetBaseObject());

                    player.PlayerTalkClass.SendGossipMenu(gossipNpcTextId, GetBaseObject().GUID);
                }

                break;
            }
            case SmartActions.SetHomePos:
            {
                foreach (var target in targets)
                    if (IsCreature(target))
                    {
                        if (e.GetTargetType() == SmartTargets.Self)
                            target.AsCreature.SetHomePosition(_me.Location.X, _me.Location.Y, _me.Location.Z, _me.Location.Orientation);
                        else if (e.GetTargetType() == SmartTargets.Position)
                            target.AsCreature.SetHomePosition(e.Target.X, e.Target.Y, e.Target.Z, e.Target.O);
                        else if (e.GetTargetType() == SmartTargets.CreatureRange ||
                                 e.GetTargetType() == SmartTargets.CreatureGuid ||
                                 e.GetTargetType() == SmartTargets.CreatureDistance ||
                                 e.GetTargetType() == SmartTargets.GameobjectRange ||
                                 e.GetTargetType() == SmartTargets.GameobjectGuid ||
                                 e.GetTargetType() == SmartTargets.GameobjectDistance ||
                                 e.GetTargetType() == SmartTargets.ClosestCreature ||
                                 e.GetTargetType() == SmartTargets.ClosestGameobject ||
                                 e.GetTargetType() == SmartTargets.OwnerOrSummoner ||
                                 e.GetTargetType() == SmartTargets.ActionInvoker ||
                                 e.GetTargetType() == SmartTargets.ClosestEnemy ||
                                 e.GetTargetType() == SmartTargets.ClosestFriendly ||
                                 e.GetTargetType() == SmartTargets.ClosestUnspawnedGameobject)
                            target.AsCreature.SetHomePosition(target.Location.X, target.Location.Y, target.Location.Z, target.Location.Orientation);
                        else
                            Log.Logger.Error("SmartScript: Action target for SMART_ACTION_SET_HOME_POS is invalid, skipping");
                    }

                break;
            }
            case SmartActions.SetHealthRegen:
            {
                foreach (var target in targets)
                    if (IsCreature(target))
                        target.AsCreature.SetRegenerateHealth(e.Action.SetHealthRegen.RegenHealth != 0);

                break;
            }
            case SmartActions.SetRoot:
            {
                foreach (var target in targets)
                    if (IsCreature(target))
                        target.AsCreature.SetControlled(e.Action.SetRoot.Root != 0, UnitState.Root);

                break;
            }
            case SmartActions.SummonCreatureGroup:
            {
                GetBaseObject().SummonCreatureGroup((byte)e.Action.CreatureGroup.Group, out var summonList);

                foreach (var summon in summonList)
                    if (unit == null && e.Action.CreatureGroup.AttackInvoker != 0)
                        summon.AI.AttackStart(unit);

                break;
            }
            case SmartActions.SetPower:
            {
                foreach (var target in targets)
                    if (IsUnit(target))
                        target.AsUnit.SetPower((PowerType)e.Action.Power.PowerType, (int)e.Action.Power.NewPower);

                break;
            }
            case SmartActions.AddPower:
            {
                foreach (var target in targets)
                    if (IsUnit(target))
                        target.AsUnit.SetPower((PowerType)e.Action.Power.PowerType, target.AsUnit.GetPower((PowerType)e.Action.Power.PowerType) + (int)e.Action.Power.NewPower);

                break;
            }
            case SmartActions.RemovePower:
            {
                foreach (var target in targets)
                    if (IsUnit(target))
                        target.AsUnit.SetPower((PowerType)e.Action.Power.PowerType, target.AsUnit.GetPower((PowerType)e.Action.Power.PowerType) - (int)e.Action.Power.NewPower);

                break;
            }
            case SmartActions.GameEventStop:
            {
                var eventId = (ushort)e.Action.GameEventStop.ID;

                if (!_gameEventManager.IsActiveEvent(eventId))
                {
                    Log.Logger.Error("SmartScript.ProcessAction: At case SMART_ACTION_GAME_EVENT_STOP, inactive event (id: {0})", eventId);

                    break;
                }

                _gameEventManager.StopEvent(eventId, true);

                break;
            }
            case SmartActions.GameEventStart:
            {
                var eventId = (ushort)e.Action.GameEventStart.ID;

                if (_gameEventManager.IsActiveEvent(eventId))
                {
                    Log.Logger.Error("SmartScript.ProcessAction: At case SMART_ACTION_GAME_EVENT_START, already activated event (id: {0})", eventId);

                    break;
                }

                _gameEventManager.StartEvent(eventId, true);

                break;
            }
            case SmartActions.StartClosestWaypoint:
            {
                var closestWaypointFromList = e.Action.ClosestWaypointFromList;

                var waypoints = new[]
                    {
                        closestWaypointFromList.Wp1, closestWaypointFromList.Wp2, closestWaypointFromList.Wp3, closestWaypointFromList.Wp4, closestWaypointFromList.Wp5, closestWaypointFromList.Wp6
                    }.Where(id => id != 0)
                     .ToList();

                var distanceToClosest = float.MaxValue;
                uint closestPathId = 0;
                uint closestWaypointId = 0;

                foreach (var target in targets)
                {
                    var creature = target.AsCreature;

                    if (creature == null)
                        continue;

                    if (IsSmart(creature))
                    {
                        foreach (var pathId in waypoints)
                        {
                            var path = _smartAIManager.GetPath(pathId);

                            if (path == null || path.Nodes.Empty())
                                continue;

                            foreach (var waypoint in path.Nodes)
                            {
                                var distToThisPath = creature.Location.GetDistance(waypoint.X, waypoint.Y, waypoint.Z);

                                if (distToThisPath < distanceToClosest)
                                {
                                    distanceToClosest = distToThisPath;
                                    closestPathId = pathId;
                                    closestWaypointId = waypoint.ID;
                                }
                            }
                        }

                        if (closestPathId != 0)
                            ((SmartAI)creature.AI).StartPath(false, closestPathId, true, null, closestWaypointId);
                    }
                }

                break;
            }
            case SmartActions.RandomSound:
            {
                List<uint> sounds = new();
                var randomSound = e.Action.RandomSound;

                foreach (var id in new[]
                         {
                             randomSound.Sound1, randomSound.Sound2, randomSound.Sound3, randomSound.Sound4
                         })
                    if (id != 0)
                        sounds.Add(id);

                var onlySelf = e.Action.RandomSound.OnlySelf != 0;

                foreach (var target in targets)
                    if (IsUnit(target))
                    {
                        var sound = sounds.SelectRandom();

                        if (e.Action.RandomSound.Distance == 1)
                            target.PlayDistanceSound(sound, onlySelf ? target.AsPlayer : null);
                        else
                            target.PlayDirectSound(sound, onlySelf ? target.AsPlayer : null);

                        Log.Logger.Debug("SmartScript.ProcessAction:: SMART_ACTION_RANDOM_SOUND: target: {0} ({1}), sound: {2}, onlyself: {3}",
                                         target.GetName(),
                                         target.GUID.ToString(),
                                         sound,
                                         onlySelf);
                    }

                break;
            }
            case SmartActions.SetCorpseDelay:
            {
                foreach (var target in targets)
                    if (IsCreature(target))
                        target.AsCreature.SetCorpseDelay(e.Action.CorpseDelay.Timer, e.Action.CorpseDelay.IncludeDecayRatio == 0);

                break;
            }
            case SmartActions.SpawnSpawngroup:
            {
                if (e.Action.GroupSpawn is { MinDelay: 0, MaxDelay: 0 })
                {
                    var ignoreRespawn = (e.Action.GroupSpawn.Spawnflags & (uint)SmartAiSpawnFlags.IgnoreRespawn) != 0;
                    var force = (e.Action.GroupSpawn.Spawnflags & (uint)SmartAiSpawnFlags.ForceSpawn) != 0;

                    // Instant spawn
                    GetBaseObject().Location
                                   .
                                   // Instant spawn
                                   Map.SpawnGroupSpawn(e.Action.GroupSpawn.GroupId, ignoreRespawn, force);
                }
                else
                {
                    // Delayed spawn (use values from parameter to schedule event to call us back
                    SmartEvent ne = new()
                    {
                        Type = SmartEvents.Update,
                        EventChance = 100
                    };

                    ne.MinMaxRepeat.Min = e.Action.GroupSpawn.MinDelay;
                    ne.MinMaxRepeat.Max = e.Action.GroupSpawn.MaxDelay;
                    ne.MinMaxRepeat.RepeatMin = 0;
                    ne.MinMaxRepeat.RepeatMax = 0;

                    ne.EventFlags = 0;
                    ne.EventFlags |= SmartEventFlags.NotRepeatable;

                    SmartAction ac = new()
                    {
                        Type = SmartActions.SpawnSpawngroup
                    };

                    ac.GroupSpawn.GroupId = e.Action.GroupSpawn.GroupId;
                    ac.GroupSpawn.MinDelay = 0;
                    ac.GroupSpawn.MaxDelay = 0;
                    ac.GroupSpawn.Spawnflags = e.Action.GroupSpawn.Spawnflags;
                    ac.TimeEvent.ID = e.Action.TimeEvent.ID;

                    SmartScriptHolder ev = new()
                    {
                        Event = ne,
                        EventId = e.EventId,
                        Target = e.Target,
                        Action = ac
                    };

                    InitTimer(ev);
                    _storedEvents.Add(ev);
                }

                break;
            }
            case SmartActions.DespawnSpawngroup:
            {
                if (e.Action.GroupSpawn is { MinDelay: 0, MaxDelay: 0 })
                {
                    var deleteRespawnTimes = (e.Action.GroupSpawn.Spawnflags & (uint)SmartAiSpawnFlags.NosaveRespawn) != 0;

                    // Instant spawn
                    GetBaseObject().Location
                                   .
                                   // Instant spawn
                                   Map.SpawnGroupSpawn(e.Action.GroupSpawn.GroupId, deleteRespawnTimes);
                }
                else
                {
                    // Delayed spawn (use values from parameter to schedule event to call us back
                    SmartEvent ne = new()
                    {
                        Type = SmartEvents.Update,
                        EventChance = 100
                    };

                    ne.MinMaxRepeat.Min = e.Action.GroupSpawn.MinDelay;
                    ne.MinMaxRepeat.Max = e.Action.GroupSpawn.MaxDelay;
                    ne.MinMaxRepeat.RepeatMin = 0;
                    ne.MinMaxRepeat.RepeatMax = 0;

                    ne.EventFlags = 0;
                    ne.EventFlags |= SmartEventFlags.NotRepeatable;

                    SmartAction ac = new()
                    {
                        Type = SmartActions.DespawnSpawngroup
                    };

                    ac.GroupSpawn.GroupId = e.Action.GroupSpawn.GroupId;
                    ac.GroupSpawn.MinDelay = 0;
                    ac.GroupSpawn.MaxDelay = 0;
                    ac.GroupSpawn.Spawnflags = e.Action.GroupSpawn.Spawnflags;
                    ac.TimeEvent.ID = e.Action.TimeEvent.ID;

                    SmartScriptHolder ev = new()
                    {
                        Event = ne,
                        EventId = e.EventId,
                        Target = e.Target,
                        Action = ac
                    };

                    InitTimer(ev);
                    _storedEvents.Add(ev);
                }

                break;
            }
            case SmartActions.DisableEvade:
            {
                if (!IsSmart())
                    break;

                ((SmartAI)_me.AI).SetEvadeDisabled(e.Action.DisableEvade.Disable != 0);

                break;
            }
            case SmartActions.AddThreat:
            {
                if (!_me.CanHaveThreatList)
                    break;

                foreach (var target in targets)
                    if (IsUnit(target))
                        _me.GetThreatManager().AddThreat(target.AsUnit, e.Action.Threat.ThreatInc - (float)e.Action.Threat.ThreatDec, null, true, true);

                break;
            }
            case SmartActions.LoadEquipment:
            {
                foreach (var target in targets)
                    if (IsCreature(target))
                        target.AsCreature.LoadEquipment((int)e.Action.LoadEquipment.ID, e.Action.LoadEquipment.Force != 0);

                break;
            }
            case SmartActions.TriggerRandomTimedEvent:
            {
                var eventId = RandomHelper.URand(e.Action.RandomTimedEvent.MinId, e.Action.RandomTimedEvent.MaxId);
                ProcessEventsFor(SmartEvents.TimedEventTriggered, null, eventId);

                break;
            }
            case SmartActions.PauseMovement:
            {
                foreach (var target in targets)
                    if (IsUnit(target))
                        target.AsUnit.PauseMovement(e.Action.PauseMovement.PauseTimer, (MovementSlot)e.Action.PauseMovement.MovementSlot, e.Action.PauseMovement.Force != 0);

                break;
            }
            case SmartActions.RespawnBySpawnId:
            {
                Map map = null;
                var obj = GetBaseObject();

                if (obj != null)
                    map = obj.Location.Map;
                else if (!targets.Empty())
                    map = targets.First().Location.Map;

                if (map != null)
                    map.Respawn((SpawnObjectType)e.Action.RespawnData.SpawnType, e.Action.RespawnData.SpawnId);
                else
                    Log.Logger.Error($"SmartScript.ProcessAction: Entry {e.EntryOrGuid} SourceType {e.GetScriptType()}, Event {e.EventId} - tries to respawn by spawnId but does not provide a map");

                break;
            }
            case SmartActions.PlayAnimkit:
            {
                foreach (var target in targets)
                    if (IsCreature(target))
                    {
                        if (e.Action.AnimKit.Type == 0)
                            target.AsCreature.PlayOneShotAnimKitId((ushort)e.Action.AnimKit.Kit);
                        else if (e.Action.AnimKit.Type == 1)
                            target.AsCreature.SetAIAnimKitId((ushort)e.Action.AnimKit.Kit);
                        else if (e.Action.AnimKit.Type == 2)
                            target.AsCreature.SetMeleeAnimKitId((ushort)e.Action.AnimKit.Kit);
                        else if (e.Action.AnimKit.Type == 3)
                            target.AsCreature.SetMovementAnimKitId((ushort)e.Action.AnimKit.Kit);

                        Log.Logger.Debug($"SmartScript::ProcessAction:: SMART_ACTION_PLAY_ANIMKIT: target: {target.GetName()} ({target.GUID}), AnimKit: {e.Action.AnimKit.Kit}, Type: {e.Action.AnimKit.Type}");
                    }
                    else if (IsGameObject(target))
                    {
                        switch (e.Action.AnimKit.Type)
                        {
                            case 0:
                                target.AsGameObject.SetAnimKitId((ushort)e.Action.AnimKit.Kit, true);

                                break;

                            case 1:
                                target.AsGameObject.SetAnimKitId((ushort)e.Action.AnimKit.Kit, false);

                                break;
                        }

                        Log.Logger.Debug("SmartScript.ProcessAction:: SMART_ACTION_PLAY_ANIMKIT: target: {0} ({1}), AnimKit: {2}, Type: {3}", target.GetName(), target.GUID.ToString(), e.Action.AnimKit.Kit, e.Action.AnimKit.Type);
                    }

                break;
            }
            case SmartActions.ScenePlay:
            {
                foreach (var target in targets)
                    target.AsPlayer?.SceneMgr?.PlayScene(e.Action.Scene.SceneId);
                

                break;
            }
            case SmartActions.SceneCancel:
            {
                foreach (var target in targets)
                    target.AsPlayer?.SceneMgr?.CancelSceneBySceneId(e.Action.Scene.SceneId);
                

                break;
            }
            case SmartActions.PlayCinematic:
            {
                foreach (var target in targets)
                {
                    if (!IsPlayer(target))
                        continue;

                    target.AsPlayer.SendCinematicStart(e.Action.Cinematic.Entry);
                }

                break;
            }
            case SmartActions.SetMovementSpeed:
            {
                var speedInteger = e.Action.MovementSpeed.SpeedInteger;
                var speedFraction = e.Action.MovementSpeed.SpeedFraction;
                var speed = (float)((float)speedInteger + (float)speedFraction / Math.Pow(10, Math.Floor(Math.Log10((float)(speedFraction != 0 ? speedFraction : 1)) + 1)));

                foreach (var target in targets)
                    if (IsCreature(target))
                        target.AsCreature.SetSpeed((UnitMoveType)e.Action.MovementSpeed.MovementType, speed);

                break;
            }
            case SmartActions.PlaySpellVisualKit:
            {
                foreach (var target in targets)
                    if (IsUnit(target))
                    {
                        target.AsUnit.WorldObjectCombat.SendPlaySpellVisualKit(e.Action.SpellVisualKit.SpellVisualKitId,
                                                                               e.Action.SpellVisualKit.KitType,
                                                                               e.Action.SpellVisualKit.Duration);

                        Log.Logger.Debug($"SmartScript::ProcessAction:: SMART_ACTION_PLAY_SPELL_VISUAL_KIT: target: {target.GetName()} ({target.GUID}), SpellVisualKit: {e.Action.SpellVisualKit.SpellVisualKitId}");
                    }

                break;
            }
            case SmartActions.OverrideLight:
            {
                var obj = GetBaseObject();

                if (obj != null)
                {
                    obj.Location.Map.SetZoneOverrideLight(e.Action.OverrideLight.ZoneId, e.Action.OverrideLight.AreaLightId, e.Action.OverrideLight.OverrideLightId, TimeSpan.FromMilliseconds(e.Action.OverrideLight.TransitionMilliseconds));

                    Log.Logger.Debug($"SmartScript::ProcessAction: SMART_ACTION_OVERRIDE_LIGHT: {obj.GUID} sets zone override light (zoneId: {e.Action.OverrideLight.ZoneId}, " +
                                     $"areaLightId: {e.Action.OverrideLight.AreaLightId}, overrideLightId: {e.Action.OverrideLight.OverrideLightId}, transitionMilliseconds: {e.Action.OverrideLight.TransitionMilliseconds})");
                }

                break;
            }
            case SmartActions.OverrideWeather:
            {
                var obj = GetBaseObject();

                if (obj != null)
                {
                    obj.Location.Map.SetZoneWeather(e.Action.OverrideWeather.ZoneId, (WeatherState)e.Action.OverrideWeather.WeatherId, e.Action.OverrideWeather.Intensity);

                    Log.Logger.Debug($"SmartScript::ProcessAction: SMART_ACTION_OVERRIDE_WEATHER: {obj.GUID} sets zone weather (zoneId: {e.Action.OverrideWeather.ZoneId}, " +
                                     $"weatherId: {e.Action.OverrideWeather.WeatherId}, intensity: {e.Action.OverrideWeather.Intensity})");
                }

                break;
            }
            case SmartActions.SetHover:
            {
                foreach (var target in targets)
                    if (IsUnit(target))
                        target.AsUnit.SetHover(e.Action.SetHover.Enable != 0);

                break;
            }
            case SmartActions.SetHealthPct:
            {
                foreach (var target in targets)
                {
                    var targetUnit = target.AsUnit;

                    targetUnit?.SetHealth(targetUnit.CountPctFromMaxHealth((int)e.Action.SetHealthPct.Percent));
                }

                break;
            }
            case SmartActions.CreateConversation:
            {
                var baseObject = GetBaseObject();

                foreach (var target in targets)
                {
                    var playerTarget = target.AsPlayer;

                    if (playerTarget == null)
                        continue;

                    var conversation = Conversation.CreateConversation(e.Action.Conversation.ID, playerTarget, playerTarget.Location, playerTarget.GUID);

                    if (conversation == null)
                        Log.Logger.Warning($"SmartScript.ProcessAction: SMART_ACTION_CREATE_CONVERSATION: id {e.Action.Conversation.ID}, baseObject {baseObject?.GetName()}, target {playerTarget.GetName()} - failed to create");
                }

                break;
            }
            case SmartActions.SetImmunePC:
            {
                foreach (var target in targets)
                    if (IsUnit(target))
                    {
                        if (e.Action.SetImmunePc.ImmunePc != 0)
                            target.AsUnit.SetUnitFlag(UnitFlags.ImmuneToPc);
                        else
                            target.AsUnit.RemoveUnitFlag(UnitFlags.ImmuneToPc);
                    }

                break;
            }
            case SmartActions.SetImmuneNPC:
            {
                foreach (var target in targets)
                    if (IsUnit(target))
                    {
                        if (e.Action.SetImmuneNPC.ImmuneNPC != 0)
                            target.AsUnit.SetUnitFlag(UnitFlags.ImmuneToNpc);
                        else
                            target.AsUnit.RemoveUnitFlag(UnitFlags.ImmuneToNpc);
                    }

                break;
            }
            case SmartActions.SetUninteractible:
            {
                foreach (var target in targets)
                    if (IsUnit(target))
                    {
                        if (e.Action.SetUninteractible.Uninteractible != 0)
                            target.AsUnit.SetUnitFlag(UnitFlags.Uninteractible);
                        else
                            target.AsUnit.RemoveUnitFlag(UnitFlags.Uninteractible);
                    }

                break;
            }
            case SmartActions.ActivateGameobject:
            {
                foreach (var target in targets)
                {
                    var targetGo = target.AsGameObject;

                    targetGo?.ActivateObject((GameObjectActions)e.Action.ActivateGameObject.GameObjectAction, (int)e.Action.ActivateGameObject.Param, GetBaseObject());
                }

                break;
            }
            case SmartActions.AddToStoredTargetList:
            {
                if (!targets.Empty())
                {
                    AddToStoredTargetList(targets, e.Action.AddToStoredTargets.ID);
                }
                else
                {
                    var baseObject = GetBaseObject();
                    Log.Logger.Warning($"SmartScript::ProcessAction:: SMART_ACTION_ADD_TO_STORED_TARGET_LIST: var {e.Action.AddToStoredTargets.ID}, baseObject {(baseObject == null ? "" : baseObject.GetName())}, event {e.EventId} - tried to add no targets to stored target list");
                }

                break;
            }
            case SmartActions.BecomePersonalCloneForPlayer:
            {
                var baseObject = GetBaseObject();

                void DoCreatePersonalClone(Position position, Player privateObjectOwner)
                {
                    Creature summon = GetBaseObject().SummonPersonalClone(position, (TempSummonType)e.Action.BecomePersonalClone.Type, TimeSpan.FromMilliseconds(e.Action.BecomePersonalClone.Duration), 0, 0, privateObjectOwner);

                    if (summon != null)
                        if (IsSmart(summon))
                            ((SmartAI)summon.AI).SetTimedActionList(e, (uint)e.EntryOrGuid, privateObjectOwner, e.EventId + 1);
                }

                // if target is position then targets container was empty
                if (e.GetTargetType() != SmartTargets.Position)
                {
                    foreach (var target in targets)
                    {
                        var playerTarget = target?.AsPlayer;

                        if (playerTarget != null)
                            DoCreatePersonalClone(baseObject.Location, playerTarget);
                    }
                }
                else
                {
                    var invoker = GetLastInvoker()?.AsPlayer;

                    if (invoker != null)
                        DoCreatePersonalClone(new Position(e.Target.X, e.Target.Y, e.Target.Z, e.Target.O), invoker);
                }

                // action list will continue on personal clones
                _timedActionList.RemoveAll(script => { return script.EventId > e.EventId; });

                break;
            }
            case SmartActions.TriggerGameEvent:
            {
                var sourceObject = GetBaseObjectOrUnitInvoker(unit);

                foreach (var target in targets)
                    if (e.Action.TriggerGameEvent.UseSaiTargetAsGameEventSource != 0)
                        GameEvents.Trigger(e.Action.TriggerGameEvent.EventId, target, sourceObject);
                    else
                        GameEvents.Trigger(e.Action.TriggerGameEvent.EventId, sourceObject, target);

                break;
            }
            case SmartActions.DoAction:
            {
                foreach (var target in targets)
                {
                    var unitTarget = target?.AsUnit;

                    if (unitTarget != null)
                    {
                        unitTarget.AI?.DoAction((int)e.Action.doAction.ActionId);
                    }
                    else
                    {
                        var goTarget = target?.AsGameObject;

                        goTarget?.AI?.DoAction((int)e.Action.doAction.ActionId);
                    }
                }

                break;
            }
            default:
                Log.Logger.Error("SmartScript.ProcessAction: Entry {0} SourceType {1}, Event {2}, Unhandled Action type {3}", e.EntryOrGuid, e.GetScriptType(), e.EventId, e.GetActionType());

                break;
        }

        if (e.Link != 0 && e.Link != e.EventId)
        {
            var linked = _smartAIManager.FindLinkedEvent(_events, e.Link);

            if (linked != null)
                ProcessEvent(linked, unit, var0, var1, bvar, spell, gob, varString);
            else
                Log.Logger.Error("SmartScript.ProcessAction: Entry {0} SourceType {1}, Event {2}, Link Event {3} not found or invalid, skipped.", e.EntryOrGuid, e.GetScriptType(), e.EventId, e.Link);
        }
    }

    private void ProcessEvent(SmartScriptHolder e, Unit unit = null, uint var0 = 0, uint var1 = 0, bool bvar = false, SpellInfo spell = null, GameObject gob = null, string varString = "")
    {
        if (!e.Active && e.GetEventType() != SmartEvents.Link)
            return;

        if ((e.Event.EventPhaseMask != 0 && !IsInPhase(e.Event.EventPhaseMask)) || (e.Event.EventFlags.HasAnyFlag(SmartEventFlags.NotRepeatable) && e.RunOnce))
            return;

        if (!e.Event.EventFlags.HasAnyFlag(SmartEventFlags.WhileCharmed) && IsCharmedCreature(_me))
            return;

        switch (e.GetEventType())
        {
            case SmartEvents.Link: //special handling
                ProcessAction(e, unit, var0, var1, bvar, spell, gob);

                break;
            //called from Update tick
            case SmartEvents.Update:
                ProcessTimedAction(e, e.Event.MinMaxRepeat.RepeatMin, e.Event.MinMaxRepeat.RepeatMax);

                break;

            case SmartEvents.UpdateOoc:
                if (_me is { IsEngaged: true })
                    return;

                ProcessTimedAction(e, e.Event.MinMaxRepeat.RepeatMin, e.Event.MinMaxRepeat.RepeatMax);

                break;

            case SmartEvents.UpdateIc:
                if (_me == null || !_me.IsEngaged)
                    return;

                ProcessTimedAction(e, e.Event.MinMaxRepeat.RepeatMin, e.Event.MinMaxRepeat.RepeatMax);

                break;

            case SmartEvents.HealthPct:
            {
                if (_me == null || !_me.IsEngaged || _me.MaxHealth == 0)
                    return;

                var perc = (uint)_me.HealthPct;

                if (perc > e.Event.MinMaxRepeat.Max || perc < e.Event.MinMaxRepeat.Min)
                    return;

                ProcessTimedAction(e, e.Event.MinMaxRepeat.RepeatMin, e.Event.MinMaxRepeat.RepeatMax);

                break;
            }
            case SmartEvents.ManaPct:
            {
                if (_me == null || !_me.IsEngaged || _me.GetMaxPower(PowerType.Mana) == 0)
                    return;

                var perc = (uint)_me.GetPowerPct(PowerType.Mana);

                if (perc > e.Event.MinMaxRepeat.Max || perc < e.Event.MinMaxRepeat.Min)
                    return;

                ProcessTimedAction(e, e.Event.MinMaxRepeat.RepeatMin, e.Event.MinMaxRepeat.RepeatMax);

                break;
            }
            case SmartEvents.Range:
            {
                if (_me == null || !_me.IsEngaged || _me.Victim == null)
                    return;

                if (_me.Location.IsInRange(_me.Victim, e.Event.MinMaxRepeat.Min, e.Event.MinMaxRepeat.Max))
                    ProcessTimedAction(e, e.Event.MinMaxRepeat.RepeatMin, e.Event.MinMaxRepeat.RepeatMax, _me.Victim);
                else // make it predictable
                    RecalcTimer(e, 500, 500);

                break;
            }
            case SmartEvents.VictimCasting:
            {
                if (_me == null || !_me.IsEngaged)
                    return;

                var victim = _me.Victim;

                if (victim == null || !victim.IsNonMeleeSpellCast(false, false, true))
                    return;

                if (e.Event.TargetCasting.SpellId > 0)
                {
                    var currSpell = victim.GetCurrentSpell(CurrentSpellTypes.Generic);

                    if (currSpell != null)
                        if (currSpell.SpellInfo.Id != e.Event.TargetCasting.SpellId)
                            return;
                }

                ProcessTimedAction(e, e.Event.TargetCasting.RepeatMin, e.Event.TargetCasting.RepeatMax, _me.Victim);

                break;
            }
            case SmartEvents.FriendlyIsCc:
            {
                if (_me == null || !_me.IsEngaged)
                    return;

                List<Creature> creatures = new();
                DoFindFriendlyCc(creatures, e.Event.FriendlyCc.Radius);

                if (creatures.Empty())
                {
                    // if there are at least two same npcs, they will perform the same action immediately even if this is useless...
                    RecalcTimer(e, 1000, 3000);

                    return;
                }

                ProcessTimedAction(e, e.Event.FriendlyCc.RepeatMin, e.Event.FriendlyCc.RepeatMax, creatures.First());

                break;
            }
            case SmartEvents.FriendlyMissingBuff:
            {
                List<Creature> creatures = new();
                DoFindFriendlyMissingBuff(creatures, e.Event.MissingBuff.Radius, e.Event.MissingBuff.Spell);

                if (creatures.Empty())
                    return;

                ProcessTimedAction(e, e.Event.MissingBuff.RepeatMin, e.Event.MissingBuff.RepeatMax, creatures.SelectRandom());

                break;
            }
            case SmartEvents.HasAura:
            {
                if (_me == null)
                    return;

                var count = _me.GetAuraCount(e.Event.Aura.Spell);

                if ((e.Event.Aura.Count == 0 && count == 0) || (e.Event.Aura.Count != 0 && count >= e.Event.Aura.Count))
                    ProcessTimedAction(e, e.Event.Aura.RepeatMin, e.Event.Aura.RepeatMax);

                break;
            }
            case SmartEvents.TargetBuffed:
            {
                if (_me?.Victim == null)
                    return;

                var count = _me.Victim.GetAuraCount(e.Event.Aura.Spell);

                if (count < e.Event.Aura.Count)
                    return;

                ProcessTimedAction(e, e.Event.Aura.RepeatMin, e.Event.Aura.RepeatMax, _me.Victim);

                break;
            }
            case SmartEvents.Charmed:
            {
                if (bvar == (e.Event.Charm.OnRemove != 1))
                    ProcessAction(e, unit, var0, var1, bvar, spell, gob);

                break;
            }
            case SmartEvents.QuestAccepted:
            case SmartEvents.QuestCompletion:
            case SmartEvents.QuestFail:
            case SmartEvents.QuestRewarded:
            {
                ProcessAction(e, unit);

                break;
            }
            case SmartEvents.QuestObjCompletion:
            {
                if (var0 == e.Event.QuestObjective.ID)
                    ProcessAction(e, unit);

                break;
            }
            //no params
            case SmartEvents.Aggro:
            case SmartEvents.Death:
            case SmartEvents.Evade:
            case SmartEvents.ReachedHome:
            case SmartEvents.CorpseRemoved:
            case SmartEvents.AiInit:
            case SmartEvents.TransportAddplayer:
            case SmartEvents.TransportRemovePlayer:
            case SmartEvents.JustSummoned:
            case SmartEvents.Reset:
            case SmartEvents.JustCreated:
            case SmartEvents.FollowCompleted:
            case SmartEvents.OnSpellclick:
            case SmartEvents.OnDespawn:
                ProcessAction(e, unit, var0, var1, bvar, spell, gob);

                break;

            case SmartEvents.GossipHello:
            {
                switch (e.Event.GossipHello.Filter)
                {
                    case 0:
                        // no filter set, always execute action
                        break;

                    case 1:
                        // OnGossipHello only filter set, skip action if OnReportUse
                        if (var0 != 0)
                            return;

                        break;

                    case 2:
                        // OnReportUse only filter set, skip action if OnGossipHello
                        if (var0 == 0)
                            return;

                        break;
                }

                ProcessAction(e, unit, var0, var1, bvar, spell, gob);

                break;
            }
            case SmartEvents.ReceiveEmote:
                if (e.Event.Emote.EmoteId == var0)
                {
                    RecalcTimer(e, e.Event.Emote.CooldownMin, e.Event.Emote.CooldownMax);
                    ProcessAction(e, unit);
                }

                break;

            case SmartEvents.Kill:
            {
                if (_me == null || unit == null)
                    return;

                if (e.Event.Kill.PlayerOnly != 0 && !unit.IsTypeId(TypeId.Player))
                    return;

                if (e.Event.Kill.Creature != 0 && unit.Entry != e.Event.Kill.Creature)
                    return;

                RecalcTimer(e, e.Event.Kill.CooldownMin, e.Event.Kill.CooldownMax);
                ProcessAction(e, unit);

                break;
            }
            case SmartEvents.SpellHitTarget:
            case SmartEvents.SpellHit:
            {
                if (spell == null)
                    return;

                if ((e.Event.SpellHit.Spell == 0 || spell.Id == e.Event.SpellHit.Spell) &&
                    (e.Event.SpellHit.School == 0 || Convert.ToBoolean((uint)spell.SchoolMask & e.Event.SpellHit.School)))
                {
                    RecalcTimer(e, e.Event.SpellHit.CooldownMin, e.Event.SpellHit.CooldownMax);
                    ProcessAction(e, unit, 0, 0, bvar, spell, gob);
                }

                break;
            }
            case SmartEvents.OnSpellCast:
            case SmartEvents.OnSpellFailed:
            case SmartEvents.OnSpellStart:
            {
                if (spell == null)
                    return;

                if (spell.Id != e.Event.SpellCast.Spell)
                    return;

                RecalcTimer(e, e.Event.SpellCast.CooldownMin, e.Event.SpellCast.CooldownMax);
                ProcessAction(e, null, 0, 0, bvar, spell);

                break;
            }
            case SmartEvents.OocLos:
            {
                if (_me == null || _me.IsEngaged)
                    return;

                //can trigger if closer than fMaxAllowedRange
                float range = e.Event.Los.MaxDist;

                //if range is ok and we are actually in LOS
                if (_me.Location.IsWithinDistInMap(unit, range) && _me.Location.IsWithinLOSInMap(unit))
                {
                    var hostilityMode = (LOSHostilityMode)e.Event.Los.HostilityMode;

                    //if friendly event&&who is not hostile OR hostile event&&who is hostile
                    if (hostilityMode == LOSHostilityMode.Any || (hostilityMode == LOSHostilityMode.NotHostile && !_me.WorldObjectCombat.IsHostileTo(unit)) || (hostilityMode == LOSHostilityMode.Hostile && _me.WorldObjectCombat.IsHostileTo(unit)))
                    {
                        if (unit != null && e.Event.Los.PlayerOnly != 0 && !unit.IsTypeId(TypeId.Player))
                            return;

                        RecalcTimer(e, e.Event.Los.CooldownMin, e.Event.Los.CooldownMax);
                        ProcessAction(e, unit);
                    }
                }

                break;
            }
            case SmartEvents.IcLos:
            {
                if (_me is not { IsEngaged: true })
                    return;

                //can trigger if closer than fMaxAllowedRange
                float range = e.Event.Los.MaxDist;

                //if range is ok and we are actually in LOS
                if (_me.Location.IsWithinDistInMap(unit, range) && _me.Location.IsWithinLOSInMap(unit))
                {
                    var hostilityMode = (LOSHostilityMode)e.Event.Los.HostilityMode;

                    //if friendly event&&who is not hostile OR hostile event&&who is hostile
                    if (hostilityMode == LOSHostilityMode.Any || (hostilityMode == LOSHostilityMode.NotHostile && !_me.WorldObjectCombat.IsHostileTo(unit)) || (hostilityMode == LOSHostilityMode.Hostile && _me.WorldObjectCombat.IsHostileTo(unit)))
                    {
                        if (unit != null && e.Event.Los.PlayerOnly != 0 && !unit.IsTypeId(TypeId.Player))
                            return;

                        RecalcTimer(e, e.Event.Los.CooldownMin, e.Event.Los.CooldownMax);
                        ProcessAction(e, unit);
                    }
                }

                break;
            }
            case SmartEvents.Respawn:
            {
                if (GetBaseObject() == null)
                    return;

                switch (e.Event.Respawn.Type)
                {
                    case (uint)SmartRespawnCondition.Map when GetBaseObject().Location.MapId != e.Event.Respawn.Map:
                    case (uint)SmartRespawnCondition.Area when GetBaseObject().Location.Zone != e.Event.Respawn.Area:
                        return;
                }

                ProcessAction(e);

                break;
            }
            case SmartEvents.SummonedUnit:
            case SmartEvents.SummonedUnitDies:
            {
                if (!IsCreature(unit))
                    return;

                if (unit != null && e.Event.Summoned.Creature != 0 && unit.Entry != e.Event.Summoned.Creature)
                    return;

                RecalcTimer(e, e.Event.Summoned.CooldownMin, e.Event.Summoned.CooldownMax);
                ProcessAction(e, unit);

                break;
            }
            case SmartEvents.ReceiveHeal:
            case SmartEvents.Damaged:
            case SmartEvents.DamagedTarget:
            {
                if (var0 > e.Event.MinMaxRepeat.Max || var0 < e.Event.MinMaxRepeat.Min)
                    return;

                RecalcTimer(e, e.Event.MinMaxRepeat.RepeatMin, e.Event.MinMaxRepeat.RepeatMax);
                ProcessAction(e, unit);

                break;
            }
            case SmartEvents.Movementinform:
            {
                if ((e.Event.MovementInform.Type != 0 && var0 != e.Event.MovementInform.Type) || (e.Event.MovementInform.ID != 0xFFFFFFFF && var1 != e.Event.MovementInform.ID))
                    return;

                ProcessAction(e, unit, var0, var1);

                break;
            }
            case SmartEvents.TransportRelocate:
            {
                if (e.Event.TransportRelocate.PointID != 0 && var0 != e.Event.TransportRelocate.PointID)
                    return;

                ProcessAction(e, unit, var0);

                break;
            }
            case SmartEvents.WaypointReached:
            case SmartEvents.WaypointResumed:
            case SmartEvents.WaypointPaused:
            case SmartEvents.WaypointStopped:
            case SmartEvents.WaypointEnded:
            {
                if (_me == null || (e.Event.Waypoint.PointID != 0 && var0 != e.Event.Waypoint.PointID) || (e.Event.Waypoint.PathID != 0 && var1 != e.Event.Waypoint.PathID))
                    return;

                ProcessAction(e, unit);

                break;
            }
            case SmartEvents.SummonDespawned:
            {
                if (e.Event.Summoned.Creature != 0 && e.Event.Summoned.Creature != var0)
                    return;

                RecalcTimer(e, e.Event.Summoned.CooldownMin, e.Event.Summoned.CooldownMax);
                ProcessAction(e, unit, var0);

                break;
            }
            case SmartEvents.InstancePlayerEnter:
            {
                if (e.Event.InstancePlayerEnter.Team != 0 && var0 != e.Event.InstancePlayerEnter.Team)
                    return;

                RecalcTimer(e, e.Event.InstancePlayerEnter.CooldownMin, e.Event.InstancePlayerEnter.CooldownMax);
                ProcessAction(e, unit, var0);

                break;
            }
            case SmartEvents.AcceptedQuest:
            case SmartEvents.RewardQuest:
            {
                if (e.Event.Quest.QuestId != 0 && var0 != e.Event.Quest.QuestId)
                    return;

                RecalcTimer(e, e.Event.Quest.CooldownMin, e.Event.Quest.CooldownMax);
                ProcessAction(e, unit, var0);

                break;
            }
            case SmartEvents.TransportAddcreature:
            {
                if (e.Event.TransportAddCreature.Creature != 0 && var0 != e.Event.TransportAddCreature.Creature)
                    return;

                ProcessAction(e, unit, var0);

                break;
            }
            case SmartEvents.AreatriggerOntrigger:
            {
                if (e.Event.Areatrigger.ID != 0 && var0 != e.Event.Areatrigger.ID)
                    return;

                ProcessAction(e, unit, var0);

                break;
            }
            case SmartEvents.TextOver:
            {
                if (var0 != e.Event.TextOver.TextGroupID || (e.Event.TextOver.CreatureEntry != 0 && e.Event.TextOver.CreatureEntry != var1))
                    return;

                ProcessAction(e, unit, var0);

                break;
            }
            case SmartEvents.DataSet:
            {
                if (e.Event.DataSet.ID != var0 || e.Event.DataSet.Value != var1)
                    return;

                RecalcTimer(e, e.Event.DataSet.CooldownMin, e.Event.DataSet.CooldownMax);
                ProcessAction(e, unit, var0, var1);

                break;
            }
            case SmartEvents.PassengerRemoved:
            case SmartEvents.PassengerBoarded:
            {
                if (unit == null)
                    return;

                RecalcTimer(e, e.Event.MinMax.RepeatMin, e.Event.MinMax.RepeatMax);
                ProcessAction(e, unit);

                break;
            }
            case SmartEvents.TimedEventTriggered:
            {
                if (e.Event.TimedEvent.ID == var0)
                    ProcessAction(e, unit);

                break;
            }
            case SmartEvents.GossipSelect:
            {
                Log.Logger.Debug("SmartScript: Gossip Select:  menu {0} action {1}", var0, var1); //little help for scripters

                if (e.Event.Gossip.Sender != var0 || e.Event.Gossip.Action != var1)
                    return;

                ProcessAction(e, unit, var0, var1);

                break;
            }
            case SmartEvents.GameEventStart:
            case SmartEvents.GameEventEnd:
            {
                if (e.Event.GameEvent.GameEventId != var0)
                    return;

                ProcessAction(e, null, var0);

                break;
            }
            case SmartEvents.GoLootStateChanged:
            {
                if (e.Event.GoLootStateChanged.LootState != var0)
                    return;

                ProcessAction(e, unit, var0, var1);

                break;
            }
            case SmartEvents.GoEventInform:
            {
                if (e.Event.EventInform.EventId != var0)
                    return;

                ProcessAction(e, null, var0);

                break;
            }
            case SmartEvents.ActionDone:
            {
                if (e.Event.DoAction.EventId != var0)
                    return;

                ProcessAction(e, unit, var0);

                break;
            }
            case SmartEvents.FriendlyHealthPCT:
            {
                if (_me == null || !_me.IsEngaged)
                    return;

                Unit unitTarget = null;

                switch (e.GetTargetType())
                {
                    case SmartTargets.CreatureRange:
                    case SmartTargets.CreatureGuid:
                    case SmartTargets.CreatureDistance:
                    case SmartTargets.ClosestCreature:
                    case SmartTargets.ClosestPlayer:
                    case SmartTargets.PlayerRange:
                    case SmartTargets.PlayerDistance:
                    {
                        var targets = GetTargets(e);

                        foreach (var target in targets)
                            if (IsUnit(target) && _me.WorldObjectCombat.IsFriendlyTo(target.AsUnit) && target.AsUnit.IsAlive && target.AsUnit.IsInCombat)
                            {
                                var healthPct = (uint)target.AsUnit.HealthPct;

                                if (healthPct > e.Event.FriendlyHealthPct.MaxHpPct || healthPct < e.Event.FriendlyHealthPct.MinHpPct)
                                    continue;

                                unitTarget = target.AsUnit;

                                break;
                            }
                    }

                    break;

                    case SmartTargets.ActionInvoker:
                        unitTarget = DoSelectLowestHpPercentFriendly(e.Event.FriendlyHealthPct.Radius, e.Event.FriendlyHealthPct.MinHpPct, e.Event.FriendlyHealthPct.MaxHpPct);

                        break;

                    default:
                        return;
                }

                if (unitTarget == null)
                    return;

                ProcessTimedAction(e, e.Event.FriendlyHealthPct.RepeatMin, e.Event.FriendlyHealthPct.RepeatMax, unitTarget);

                break;
            }
            case SmartEvents.DistanceCreature:
            {
                if (_me == null)
                    return;

                Creature creature = null;

                if (e.Event.Distance.GUID != 0)
                {
                    creature = FindCreatureNear(_me, e.Event.Distance.GUID);

                    if (creature == null)
                        return;

                    if (!_me.Location.IsInRange(creature, 0, e.Event.Distance.Dist))
                        return;
                }
                else if (e.Event.Distance.Entry != 0)
                {
                    var list = _me.Location.GetCreatureListWithEntryInGrid(e.Event.Distance.Entry, e.Event.Distance.Dist);

                    if (!list.Empty())
                        creature = list.FirstOrDefault();
                }

                if (creature != null)
                    ProcessTimedAction(e, e.Event.Distance.Repeat, e.Event.Distance.Repeat, creature);

                break;
            }
            case SmartEvents.DistanceGameobject:
            {
                if (_me == null)
                    return;

                GameObject gameobject = null;

                if (e.Event.Distance.GUID != 0)
                {
                    gameobject = FindGameObjectNear(_me, e.Event.Distance.GUID);

                    if (gameobject == null)
                        return;

                    if (!_me.Location.IsInRange(gameobject, 0, e.Event.Distance.Dist))
                        return;
                }
                else if (e.Event.Distance.Entry != 0)
                {
                    var list = _me.Location.GetGameObjectListWithEntryInGrid(e.Event.Distance.Entry, e.Event.Distance.Dist);

                    if (!list.Empty())
                        gameobject = list.FirstOrDefault();
                }

                if (gameobject != null)
                    ProcessTimedAction(e, e.Event.Distance.Repeat, e.Event.Distance.Repeat, null, 0, 0, false, null, gameobject);

                break;
            }
            case SmartEvents.CounterSet:
                if (e.Event.Counter.ID != var0 || GetCounterValue(e.Event.Counter.ID) != e.Event.Counter.Value)
                    return;

                ProcessTimedAction(e, e.Event.Counter.CooldownMin, e.Event.Counter.CooldownMax);

                break;

            case SmartEvents.SceneStart:
            case SmartEvents.SceneCancel:
            case SmartEvents.SceneComplete:
            {
                ProcessAction(e, unit);

                break;
            }
            case SmartEvents.SceneTrigger:
            {
                if (e.Event.ParamString != varString)
                    return;

                ProcessAction(e, unit, var0, 0, false, null, null, varString);

                break;
            }
            default:
                Log.Logger.Error("SmartScript.ProcessEvent: Unhandled Event type {0}", e.GetEventType());

                break;
        }
    }

    private void ProcessTimedAction(SmartScriptHolder e, uint min, uint max, Unit unit = null, uint var0 = 0, uint var1 = 0, bool bvar = false, SpellInfo spell = null, GameObject gob = null, string varString = "")
    {
        // We may want to execute action rarely and because of this if condition is not fulfilled the action will be rechecked in a long time
        if (_conditionManager.IsObjectMeetingSmartEventConditions(e.EntryOrGuid, e.EventId, e.SourceType, unit, GetBaseObject()))
        {
            RecalcTimer(e, min, max);
            ProcessAction(e, unit, var0, var1, bvar, spell, gob, varString);
        }
        else
        {
            RecalcTimer(e, Math.Min(min, 5000), Math.Min(min, 5000));
        }
    }

    private void RaisePriority(SmartScriptHolder e)
    {
        e.Timer = 1;

        // Change priority only if it's set to default, otherwise keep the current order of events
        if (e.Priority == SmartScriptHolder.DEFAULT_PRIORITY)
        {
            e.Priority = _currentPriority++;
            _eventSortingRequired = true;
        }
    }

    private void RecalcTimer(SmartScriptHolder e, uint min, uint max)
    {
        if (e.EntryOrGuid == 15294 && e.Timer != 0)
            Log.Logger.Error("Called RecalcTimer");

        // min/max was checked at loading!
        e.Timer = RandomHelper.URand(min, max);
        e.Active = e.Timer == 0;
    }

    private void RemoveStoredEvent(uint id)
    {
        if (!_storedEvents.Empty())
            foreach (var holder in _storedEvents)
                if (holder.EventId == id)
                {
                    _storedEvents.Remove(holder);

                    return;
                }
    }

    private void ResetBaseObject()
    {
        WorldObject lookupRoot = _me ?? (WorldObject)_go;

        if (lookupRoot != null)
        {
            if (!_meOrigGUID.IsEmpty)
            {
                var m = ObjectAccessor.GetCreature(lookupRoot, _meOrigGUID);

                if (m != null)
                {
                    _me = m;
                    _go = null;
                    _areaTrigger = null;
                }
            }

            if (!_goOrigGUID.IsEmpty)
            {
                var o = ObjectAccessor.GetGameObject(lookupRoot, _goOrigGUID);

                if (o != null)
                {
                    _me = null;
                    _go = o;
                    _areaTrigger = null;
                }
            }
        }

        _goOrigGUID.Clear();
        _meOrigGUID.Clear();
    }

    private void RetryLater(SmartScriptHolder e, bool ignoreChanceRoll = false)
    {
        RaisePriority(e);

        // This allows to retry the action later without rolling again the chance roll (which might fail and end up not executing the action)
        if (ignoreChanceRoll)
            e.Event.EventFlags |= SmartEventFlags.TempIgnoreChanceRoll;

        e.RunOnce = false;
    }

    private void SetPhase(uint p)
    {
        _eventPhase = p;
    }

    private void SortEvents(List<SmartScriptHolder> events)
    {
        events.Sort();
    }

    private void StoreCounter(uint id, uint value, uint reset)
    {
        if (_counterList.ContainsKey(id))
        {
            if (reset == 0)
                _counterList[id] += value;
            else
                _counterList[id] = value;
        }
        else
        {
            _counterList.Add(id, value);
        }

        ProcessEventsFor(SmartEvents.CounterSet, null, id);
    }

    private void StoreTargetList(List<WorldObject> targets, uint id)
    {
        // insert or replace
        _storedTargets.Remove(id);
        _storedTargets.Add(id, new ObjectGuidList(targets));
    }

    private void UpdateTimer(SmartScriptHolder e, uint diff)
    {
        if (e.GetEventType() == SmartEvents.Link)
            return;

        if (e.Event.EventPhaseMask != 0 && !IsInPhase(e.Event.EventPhaseMask))
            return;

        if (e.GetEventType() == SmartEvents.UpdateIc && (_me == null || !_me.IsEngaged))
            return;

        if (e.GetEventType() == SmartEvents.UpdateOoc && _me is { IsEngaged: true }) //can be used with me=NULL (go script)
            return;

        if (e.Timer < diff)
        {
            // delay spell cast event if another spell is being casted
            if (e.GetActionType() == SmartActions.Cast)
                if (!Convert.ToBoolean(e.Action.Cast.CastFlags & (uint)SmartCastFlags.InterruptPrevious))
                    if (_me != null && _me.HasUnitState(UnitState.Casting))
                    {
                        RaisePriority(e);

                        return;
                    }

            // Delay flee for assist event if stunned or rooted
            if (e.GetActionType() == SmartActions.FleeForAssist)
                if (_me != null && _me.HasUnitState(UnitState.Root | UnitState.LostControl))
                {
                    e.Timer = 1;

                    return;
                }

            e.Active = true; //activate events with cooldown

            switch (e.GetEventType()) //process ONLY timed events
            {
                case SmartEvents.Update:
                case SmartEvents.UpdateIc:
                case SmartEvents.UpdateOoc:
                case SmartEvents.HealthPct:
                case SmartEvents.ManaPct:
                case SmartEvents.Range:
                case SmartEvents.VictimCasting:
                case SmartEvents.FriendlyIsCc:
                case SmartEvents.FriendlyMissingBuff:
                case SmartEvents.HasAura:
                case SmartEvents.TargetBuffed:
                case SmartEvents.FriendlyHealthPCT:
                case SmartEvents.DistanceCreature:
                case SmartEvents.DistanceGameobject:
                {
                    if (e.GetScriptType() == SmartScriptType.TimedActionlist)
                    {
                        Unit invoker = null;

                        if (_me != null && !_mTimedActionListInvoker.IsEmpty)
                            invoker = _objectAccessor.GetUnit(_me, _mTimedActionListInvoker);

                        ProcessEvent(e, invoker);
                        e.EnableTimed = false; //disable event if it is in an ActionList and was processed once

                        foreach (var holder in _timedActionList)
                            //find the first event which is not the current one and enable it
                            if (holder.EventId > e.EventId)
                            {
                                holder.EnableTimed = true;

                                break;
                            }
                    }
                    else
                    {
                        ProcessEvent(e);
                    }

                    break;
                }
            }

            if (e.Priority != SmartScriptHolder.DEFAULT_PRIORITY)
                // Reset priority to default one only if the event hasn't been rescheduled again to next loop
                if (e.Timer > 1)
                {
                    // Re-sort events if this was moved to the top of the queue
                    _eventSortingRequired = true;
                    // Reset priority to default one
                    e.Priority = SmartScriptHolder.DEFAULT_PRIORITY;
                }
        }
        else
        {
            e.Timer -= diff;

            if (e.EntryOrGuid == 15294 && _me.GUID.Counter == 55039 && e.Timer != 0)
                Log.Logger.Error("Called UpdateTimer: reduce timer: e.timer: {0}, diff: {1}  current time: {2}", e.Timer, diff, Time.MSTime);
        }
    }
}