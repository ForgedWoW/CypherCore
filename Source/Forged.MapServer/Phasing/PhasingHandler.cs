// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Forged.MapServer.Chat;
using Forged.MapServer.Conditions;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Globals;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Grids;
using Forged.MapServer.Networking.Packets.Misc;
using Forged.MapServer.Networking.Packets.Party;
using Framework.Constants;

namespace Forged.MapServer.Phasing;

public class PhasingHandler
{
    private readonly CliDB _cliDB;
    private readonly ConditionManager _conditionManager;
    private readonly DB2Manager _db2Manager;
    private readonly GridDefines _gridDefines;
    private readonly ObjectAccessor _objectAccessor;
    private readonly GameObjectManager _objectManager;

    public PhasingHandler(DB2Manager db2Manager, ObjectAccessor objectAccessor, CliDB cliDB, GridDefines gridDefines, GameObjectManager objectManager, ConditionManager conditionManager)
    {
        _db2Manager = db2Manager;
        _objectAccessor = objectAccessor;
        _cliDB = cliDB;
        _gridDefines = gridDefines;
        _objectManager = objectManager;
        _conditionManager = conditionManager;
        AlwaysVisible = new PhaseShift();
        InitDbPhaseShift(AlwaysVisible, PhaseUseFlagsValues.AlwaysVisible, 0, 0);
    }

    public PhaseShift AlwaysVisible { get; }
    public PhaseShift EmptyPhaseShift { get; } = new();

    public void AddPhase(WorldObject obj, uint phaseId, bool updateVisibility)
    {
        ControlledUnitVisitor visitor = new(obj);
        AddPhase(obj, phaseId, obj.GUID, updateVisibility, visitor);
    }

    public void AddPhaseGroup(WorldObject obj, uint phaseGroupId, bool updateVisibility)
    {
        var phasesInGroup = _db2Manager.GetPhasesForGroup(phaseGroupId);

        if (phasesInGroup.Empty())
            return;

        ControlledUnitVisitor visitor = new(obj);
        AddPhaseGroup(obj, phasesInGroup, obj.GUID, updateVisibility, visitor);
    }

    public void AddVisibleMapId(WorldObject obj, uint visibleMapId)
    {
        ControlledUnitVisitor visitor = new(obj);
        AddVisibleMapId(obj, visibleMapId, visitor);
    }

    public void FillPartyMemberPhase(PartyMemberPhaseStates partyMemberPhases, PhaseShift phaseShift)
    {
        partyMemberPhases.PhaseShiftFlags = (int)phaseShift.Flags;
        partyMemberPhases.PersonalGUID = phaseShift.PersonalGuid;

        foreach (var pair in phaseShift.Phases)
            partyMemberPhases.List.Add(new PartyMemberPhase((uint)pair.Value.Flags, pair.Key));
    }

    public void ForAllControlled(Unit unit, Action<Unit> func)
    {
        for (var i = 0; i < unit.Controlled.Count; ++i)
        {
            var controlled = unit.Controlled[i];

            if (controlled.TypeId != TypeId.Player && controlled.Vehicle == null) // Player inside nested vehicle should not phase the root vehicle and its accessories (only direct root vehicle control does)
                func(controlled);
        }

        for (byte i = 0; i < SharedConst.MaxSummonSlot; ++i)
            if (!unit.SummonSlot[i].IsEmpty)
            {
                var summon = unit.Location.Map.GetCreature(unit.SummonSlot[i]);

                if (summon != null)
                    func(summon);
            }

        var vehicle = unit.VehicleKit;

        if (vehicle == null)
            return;

        foreach (var passenger in vehicle.Seats.Select(seat => _objectAccessor.GetUnit(unit, seat.Value.Passenger.Guid)).Where(passenger => passenger != null))
            func(passenger);
    }

    public string FormatPhases(PhaseShift phaseShift)
    {
        StringBuilder phases = new();

        foreach (var phaseId in phaseShift.Phases.Keys)
            phases.Append(phaseId + ',');

        return phases.ToString();
    }

    public PhaseShift GetAlwaysVisiblePhaseShift()
    {
        return AlwaysVisible;
    }

    public PhaseFlags GetPhaseFlags(uint phaseId)
    {
        if (!_cliDB.PhaseStorage.TryGetValue(phaseId, out var phase))
            return PhaseFlags.None;

        if (phase.Flags.HasAnyFlag(PhaseEntryFlags.Cosmetic))
            return PhaseFlags.Cosmetic;

        return phase.Flags.HasAnyFlag(PhaseEntryFlags.Personal) ? PhaseFlags.Personal : PhaseFlags.None;
    }

    public uint GetTerrainMapId(PhaseShift phaseShift, uint mapId, TerrainInfo terrain, float x, float y)
    {
        if (phaseShift.VisibleMapIds.Empty())
            return mapId;

        if (phaseShift.VisibleMapIds.Count == 1)
            return phaseShift.VisibleMapIds.First().Key;

        var gridCoord = _gridDefines.ComputeGridCoord(x, y);
        var gx = (int)(MapConst.MaxGrids - 1 - gridCoord.X);
        var gy = (int)(MapConst.MaxGrids - 1 - gridCoord.Y);

        foreach (var visibleMap in phaseShift.VisibleMapIds.Where(visibleMap => terrain.HasChildTerrainGridFile(visibleMap.Key, gx, gy)))
            return visibleMap.Key;

        return mapId;
    }

    public bool InDbPhaseShift(WorldObject obj, PhaseUseFlagsValues phaseUseFlags, ushort phaseId, uint phaseGroupId)
    {
        PhaseShift phaseShift = new();
        InitDbPhaseShift(phaseShift, phaseUseFlags, phaseId, phaseGroupId);

        return obj.Location.PhaseShift.CanSee(phaseShift);
    }

    public void InheritPhaseShift(WorldObject target, WorldObject source)
    {
        target.Location.PhaseShift = source.Location.PhaseShift;
        target.Location.SuppressedPhaseShift = source.Location.SuppressedPhaseShift;
    }

    public void InitDbPersonalOwnership(PhaseShift phaseShift, ObjectGuid personalGuid)
    {
        phaseShift.PersonalGuid = personalGuid;
    }

    public void InitDbPhaseShift(PhaseShift phaseShift, PhaseUseFlagsValues phaseUseFlags, uint phaseId, uint phaseGroupId)
    {
        phaseShift.ClearPhases();
        phaseShift.IsDbPhaseShift = true;

        var flags = PhaseShiftFlags.None;

        if (phaseUseFlags.HasAnyFlag(PhaseUseFlagsValues.AlwaysVisible))
            flags = flags | PhaseShiftFlags.AlwaysVisible | PhaseShiftFlags.Unphased;

        if (phaseUseFlags.HasAnyFlag(PhaseUseFlagsValues.Inverse))
            flags |= PhaseShiftFlags.Inverse;

        if (phaseId != 0)
            phaseShift.AddPhase(phaseId, GetPhaseFlags(phaseId), null);
        else
        {
            var phasesInGroup = _db2Manager.GetPhasesForGroup(phaseGroupId);

            foreach (var phaseInGroup in phasesInGroup)
                phaseShift.AddPhase(phaseInGroup, GetPhaseFlags(phaseInGroup), null);
        }

        if (phaseShift.Phases.Empty() || phaseShift.HasPhase(169))
        {
            if (flags.HasFlag(PhaseShiftFlags.Inverse))
                flags |= PhaseShiftFlags.InverseUnphased;
            else
                flags |= PhaseShiftFlags.Unphased;
        }

        phaseShift.Flags = flags;
    }

    public void InitDbVisibleMapId(PhaseShift phaseShift, int visibleMapId)
    {
        phaseShift.VisibleMapIds.Clear();

        if (visibleMapId != -1)
            phaseShift.AddVisibleMapId((uint)visibleMapId, _objectManager.GetTerrainSwapInfo((uint)visibleMapId));
    }

    public bool IsPersonalPhase(uint phaseId)
    {
        return _cliDB.PhaseStorage.TryGetValue(phaseId, out var phase) && phase.Flags.HasFlag(PhaseEntryFlags.Personal);
    }

    public void OnAreaChange(WorldObject obj)
    {
        var phaseShift = obj.Location.PhaseShift;
        var suppressedPhaseShift = obj.Location.SuppressedPhaseShift;
        var oldPhases = phaseShift.Phases; // for comparison
        ConditionSourceInfo srcInfo = new(obj);

        obj.Location.PhaseShift.ClearPhases();
        obj.Location.SuppressedPhaseShift.ClearPhases();

        var areaId = obj.Location.Area;
        var areaEntry = _cliDB.AreaTableStorage.LookupByKey(areaId);

        while (areaEntry != null)
        {
            var newAreaPhases = _objectManager.GetPhasesForArea(areaEntry.Id);

            if (!newAreaPhases.Empty())
                foreach (var phaseArea in newAreaPhases)
                {
                    if (phaseArea.SubAreaExclusions.Contains(areaId))
                        continue;

                    var phaseId = phaseArea.PhaseInfo.Id;

                    if (_conditionManager.IsObjectMeetToConditions(srcInfo, phaseArea.Conditions))
                        phaseShift.AddPhase(phaseId, GetPhaseFlags(phaseId), phaseArea.Conditions);
                    else
                        suppressedPhaseShift.AddPhase(phaseId, GetPhaseFlags(phaseId), phaseArea.Conditions);
                }

            areaEntry = _cliDB.AreaTableStorage.LookupByKey(areaEntry.ParentAreaID);
        }

        var changed = phaseShift.Phases != oldPhases;
        var unit = obj.AsUnit;

        if (unit != null)
        {
            foreach (var aurEff in unit.GetAuraEffectsByType(AuraType.Phase))
            {
                var phaseId = (uint)aurEff.MiscValueB;
                changed = phaseShift.AddPhase(phaseId, GetPhaseFlags(phaseId), null) || changed;
            }

            foreach (var aurEff in unit.GetAuraEffectsByType(AuraType.PhaseGroup))
            {
                var phasesInGroup = _db2Manager.GetPhasesForGroup((uint)aurEff.MiscValueB);

                foreach (var phaseId in phasesInGroup)
                    changed = phaseShift.AddPhase(phaseId, GetPhaseFlags(phaseId), null) || changed;
            }

            if (phaseShift.PersonalReferences != 0)
                phaseShift.PersonalGuid = unit.GUID;

            if (changed)
                unit.OnPhaseChange();

            ControlledUnitVisitor visitor = new(unit);
            visitor.VisitControlledOf(unit, controlled => { InheritPhaseShift(controlled, unit); });

            if (changed)
                unit.RemoveNotOwnSingleTargetAuras(true);
        }
        else
        {
            if (phaseShift.PersonalReferences != 0)
                phaseShift.PersonalGuid = obj.GUID;
        }

        UpdateVisibilityIfNeeded(obj, true, changed);
    }

    public bool OnConditionChange(WorldObject obj, bool updateVisibility = true)
    {
        var phaseShift = obj.Location.PhaseShift;
        var suppressedPhaseShift = obj.Location.SuppressedPhaseShift;
        PhaseShift newSuppressions = new();
        ConditionSourceInfo srcInfo = new(obj);
        var changed = false;

        foreach (var pair in phaseShift.Phases.ToList())
            if (pair.Value.AreaConditions != null && !_conditionManager.IsObjectMeetToConditions(srcInfo, pair.Value.AreaConditions))
            {
                newSuppressions.AddPhase(pair.Key, pair.Value.Flags, pair.Value.AreaConditions, pair.Value.References);
                phaseShift.ModifyPhasesReferences(pair.Key, pair.Value, -pair.Value.References);
                phaseShift.Phases.Remove(pair.Key);
            }

        foreach (var pair in suppressedPhaseShift.Phases.ToList())
            if (_conditionManager.IsObjectMeetToConditions(srcInfo, pair.Value.AreaConditions))
            {
                changed = phaseShift.AddPhase(pair.Key, pair.Value.Flags, pair.Value.AreaConditions, pair.Value.References) || changed;
                suppressedPhaseShift.ModifyPhasesReferences(pair.Key, pair.Value, -pair.Value.References);
                suppressedPhaseShift.Phases.Remove(pair.Key);
            }

        foreach (var pair in phaseShift.VisibleMapIds.ToList())
            if (!_conditionManager.IsObjectMeetingNotGroupedConditions(ConditionSourceType.TerrainSwap, pair.Key, srcInfo))
            {
                newSuppressions.AddVisibleMapId(pair.Key, pair.Value.VisibleMapInfo, pair.Value.References);

                changed = pair.Value.VisibleMapInfo.UiMapPhaseIDs.Aggregate(changed, (current, uiMapPhaseId) => phaseShift.RemoveUiMapPhaseId(uiMapPhaseId) || current);

                phaseShift.VisibleMapIds.Remove(pair.Key);
            }

        foreach (var pair in suppressedPhaseShift.VisibleMapIds.ToList())
            if (_conditionManager.IsObjectMeetingNotGroupedConditions(ConditionSourceType.TerrainSwap, pair.Key, srcInfo))
            {
                changed = phaseShift.AddVisibleMapId(pair.Key, pair.Value.VisibleMapInfo, pair.Value.References) || changed;

                foreach (var uiMapPhaseId in pair.Value.VisibleMapInfo.UiMapPhaseIDs)
                    changed = phaseShift.AddUiMapPhaseId(uiMapPhaseId) || changed;

                suppressedPhaseShift.VisibleMapIds.Remove(pair.Key);
            }

        var unit = obj.AsUnit;

        if (unit != null)
        {
            foreach (var aurEff in unit.GetAuraEffectsByType(AuraType.Phase))
            {
                var phaseId = (uint)aurEff.MiscValueB;

                // if condition was met previously there is nothing to erase
                if (newSuppressions.RemovePhase(phaseId))
                    phaseShift.AddPhase(phaseId, GetPhaseFlags(phaseId), null); //todo needs checked
            }

            foreach (var aurEff in unit.GetAuraEffectsByType(AuraType.PhaseGroup))
            {
                var phasesInGroup = _db2Manager.GetPhasesForGroup((uint)aurEff.MiscValueB);

                if (phasesInGroup.Empty())
                    continue;

                foreach (var phaseId in phasesInGroup)
                {
                    var eraseResult = newSuppressions.RemovePhase(phaseId);

                    // if condition was met previously there is nothing to erase
                    if (eraseResult)
                        phaseShift.AddPhase(phaseId, GetPhaseFlags(phaseId), null);
                }
            }
        }

        if (phaseShift.PersonalReferences != 0)
            phaseShift.PersonalGuid = obj.GUID;

        changed = changed || !newSuppressions.Phases.Empty() || !newSuppressions.VisibleMapIds.Empty();

        foreach (var pair in newSuppressions.Phases)
            suppressedPhaseShift.AddPhase(pair.Key, pair.Value.Flags, pair.Value.AreaConditions, pair.Value.References);

        foreach (var pair in newSuppressions.VisibleMapIds)
            suppressedPhaseShift.AddVisibleMapId(pair.Key, pair.Value.VisibleMapInfo, pair.Value.References);

        if (unit != null)
        {
            if (changed)
                unit.OnPhaseChange();

            ControlledUnitVisitor visitor = new(unit);
            visitor.VisitControlledOf(unit, controlled => { InheritPhaseShift(controlled, unit); });

            if (changed)
                unit.RemoveNotOwnSingleTargetAuras(true);
        }

        UpdateVisibilityIfNeeded(obj, updateVisibility, changed);

        return changed;
    }

    public void OnMapChange(WorldObject obj)
    {
        var phaseShift = obj.Location.PhaseShift;
        var suppressedPhaseShift = obj.Location.SuppressedPhaseShift;
        ConditionSourceInfo srcInfo = new(obj);

        obj.Location.PhaseShift.VisibleMapIds.Clear();
        obj.Location.PhaseShift.UiMapPhaseIds.Clear();
        obj.Location.SuppressedPhaseShift.VisibleMapIds.Clear();

        foreach (var (mapId, visibleMapInfo) in _objectManager.TerrainSwaps.KeyValueList)
            if (_conditionManager.IsObjectMeetingNotGroupedConditions(ConditionSourceType.TerrainSwap, visibleMapInfo.Id, srcInfo))
            {
                if (mapId == obj.Location.MapId)
                    phaseShift.AddVisibleMapId(visibleMapInfo.Id, visibleMapInfo);

                // ui map is visible on all maps
                foreach (var uiMapPhaseId in visibleMapInfo.UiMapPhaseIDs)
                    phaseShift.AddUiMapPhaseId(uiMapPhaseId);
            }
            else if (mapId == obj.Location.MapId)
                suppressedPhaseShift.AddVisibleMapId(visibleMapInfo.Id, visibleMapInfo);

        UpdateVisibilityIfNeeded(obj, false, true);
    }

    public void PrintToChat(CommandHandler chat, WorldObject target)
    {
        var phaseShift = target.Location.PhaseShift;

        var phaseOwnerName = "N/A";

        if (phaseShift.HasPersonalPhase)
        {
            var personalGuid = _objectAccessor.GetWorldObject(target, phaseShift.PersonalGuid);

            if (personalGuid != null)
                phaseOwnerName = personalGuid.GetName();
        }

        chat.SendSysMessage(CypherStrings.PhaseshiftStatus, phaseShift.Flags, phaseShift.PersonalGuid.ToString(), phaseOwnerName);

        if (!phaseShift.Phases.Empty())
        {
            StringBuilder phases = new();
            var cosmetic = _objectManager.GetCypherString(CypherStrings.PhaseFlagCosmetic, chat.SessionDbcLocale);
            var personal = _objectManager.GetCypherString(CypherStrings.PhaseFlagPersonal, chat.SessionDbcLocale);

            foreach (var pair in phaseShift.Phases)
            {
                phases.Append("\r\n");
                phases.Append("   ");
                phases.Append($"{pair.Key} ({_objectManager.GetPhaseName(pair.Key)})'");

                if (pair.Value.Flags.HasFlag(PhaseFlags.Cosmetic))
                    phases.Append($" ({cosmetic})");

                if (pair.Value.Flags.HasFlag(PhaseFlags.Personal))
                    phases.Append($" ({personal})");
            }

            chat.SendSysMessage(CypherStrings.PhaseshiftPhases, phases.ToString());
        }

        if (!phaseShift.VisibleMapIds.Empty())
        {
            StringBuilder visibleMapIds = new();

            foreach (var visibleMapId in phaseShift.VisibleMapIds)
                visibleMapIds.Append(visibleMapId.Key + ',' + ' ');

            chat.SendSysMessage(CypherStrings.PhaseshiftVisibleMapIds, visibleMapIds.ToString());
        }

        if (!phaseShift.UiMapPhaseIds.Empty())
        {
            StringBuilder uiWorldMapAreaIdSwaps = new();

            foreach (var uiWorldMapAreaIdSwap in phaseShift.UiMapPhaseIds)
                uiWorldMapAreaIdSwaps.AppendFormat($"{uiWorldMapAreaIdSwap.Key}, ");

            chat.SendSysMessage(CypherStrings.PhaseshiftUiWorldMapAreaSwaps, uiWorldMapAreaIdSwaps.ToString());
        }
    }

    public void RemovePhase(WorldObject obj, uint phaseId, bool updateVisibility)
    {
        ControlledUnitVisitor visitor = new(obj);
        RemovePhase(obj, phaseId, updateVisibility, visitor);
    }

    public void RemovePhaseGroup(WorldObject obj, uint phaseGroupId, bool updateVisibility)
    {
        var phasesInGroup = _db2Manager.GetPhasesForGroup(phaseGroupId);

        if (phasesInGroup.Empty())
            return;

        ControlledUnitVisitor visitor = new(obj);
        RemovePhaseGroup(obj, phasesInGroup, updateVisibility, visitor);
    }

    public void RemoveVisibleMapId(WorldObject obj, uint visibleMapId)
    {
        ControlledUnitVisitor visitor = new(obj);
        RemoveVisibleMapId(obj, visibleMapId, visitor);
    }

    public void ResetPhaseShift(WorldObject obj)
    {
        obj.Location.PhaseShift.Clear();
        obj.Location.SuppressedPhaseShift.Clear();
    }

    public void SendToPlayer(Player player, PhaseShift phaseShift)
    {
        PhaseShiftChange phaseShiftChange = new()
        {
            Client = player.GUID,
            Phaseshift =
            {
                PhaseShiftFlags = (uint)phaseShift.Flags,
                PersonalGUID = phaseShift.PersonalGuid
            }
        };

        foreach (var pair in phaseShift.Phases)
            phaseShiftChange.Phaseshift.Phases.Add(new PhaseShiftDataPhase((uint)pair.Value.Flags, pair.Key));

        foreach (var visibleMapId in phaseShift.VisibleMapIds)
            phaseShiftChange.VisibleMapIDs.Add((ushort)visibleMapId.Key);

        foreach (var uiWorldMapAreaIdSwap in phaseShift.UiMapPhaseIds)
            phaseShiftChange.UiMapPhaseIDs.Add((ushort)uiWorldMapAreaIdSwap.Key);

        player.SendPacket(phaseShiftChange);
    }

    public void SendToPlayer(Player player)
    {
        SendToPlayer(player, player.Location.PhaseShift);
    }

    public void SetAlwaysVisible(WorldObject obj, bool apply, bool updateVisibility)
    {
        if (apply)
            obj.Location.PhaseShift.Flags |= PhaseShiftFlags.AlwaysVisible;
        else
            obj.Location.PhaseShift.Flags &= ~PhaseShiftFlags.AlwaysVisible;

        UpdateVisibilityIfNeeded(obj, updateVisibility, true);
    }

    public void SetInversed(WorldObject obj, bool apply, bool updateVisibility)
    {
        if (apply)
            obj.Location.PhaseShift.Flags |= PhaseShiftFlags.Inverse;
        else
            obj.Location.PhaseShift.Flags &= ~PhaseShiftFlags.Inverse;

        obj.Location.PhaseShift.UpdateUnphasedFlag();

        UpdateVisibilityIfNeeded(obj, updateVisibility, true);
    }

    private void AddPhase(WorldObject obj, uint phaseId, ObjectGuid personalGuid, bool updateVisibility, ControlledUnitVisitor visitor)
    {
        var changed = obj.Location.PhaseShift.AddPhase(phaseId, GetPhaseFlags(phaseId), null);

        if (obj.Location.PhaseShift.PersonalReferences != 0)
            obj.Location.PhaseShift.PersonalGuid = personalGuid;

        var unit = obj.AsUnit;

        if (unit != null)
        {
            unit.OnPhaseChange();
            visitor.VisitControlledOf(unit, controlled => { AddPhase(controlled, phaseId, personalGuid, updateVisibility, visitor); });
            unit.RemoveNotOwnSingleTargetAuras(true);
        }

        UpdateVisibilityIfNeeded(obj, updateVisibility, changed);
    }

    private void AddPhaseGroup(WorldObject obj, List<uint> phasesInGroup, ObjectGuid personalGuid, bool updateVisibility, ControlledUnitVisitor visitor)
    {
        var changed = false;

        foreach (var phaseId in phasesInGroup)
            changed = obj.Location.PhaseShift.AddPhase(phaseId, GetPhaseFlags(phaseId), null) || changed;

        if (obj.Location.PhaseShift.PersonalReferences != 0)
            obj.Location.PhaseShift.PersonalGuid = personalGuid;

        var unit = obj.AsUnit;

        if (unit != null)
        {
            unit.OnPhaseChange();
            visitor.VisitControlledOf(unit, controlled => { AddPhaseGroup(controlled, phasesInGroup, personalGuid, updateVisibility, visitor); });
            unit.RemoveNotOwnSingleTargetAuras(true);
        }

        UpdateVisibilityIfNeeded(obj, updateVisibility, changed);
    }

    private void AddVisibleMapId(WorldObject obj, uint visibleMapId, ControlledUnitVisitor visitor)
    {
        var terrainSwapInfo = _objectManager.GetTerrainSwapInfo(visibleMapId);
        var changed = obj.Location.PhaseShift.AddVisibleMapId(visibleMapId, terrainSwapInfo);

        foreach (var uiMapPhaseId in terrainSwapInfo.UiMapPhaseIDs)
            changed = obj.Location.PhaseShift.AddUiMapPhaseId(uiMapPhaseId) || changed;

        var unit = obj.AsUnit;

        if (unit != null)
            visitor.VisitControlledOf(unit, controlled => { AddVisibleMapId(controlled, visibleMapId, visitor); });

        UpdateVisibilityIfNeeded(obj, false, changed);
    }

    private void RemovePhase(WorldObject obj, uint phaseId, bool updateVisibility, ControlledUnitVisitor visitor)
    {
        var changed = obj.Location.PhaseShift.RemovePhase(phaseId);

        var unit = obj.AsUnit;

        if (unit != null)
        {
            unit.OnPhaseChange();
            visitor.VisitControlledOf(unit, controlled => { RemovePhase(controlled, phaseId, updateVisibility, visitor); });
            unit.RemoveNotOwnSingleTargetAuras(true);
        }

        UpdateVisibilityIfNeeded(obj, updateVisibility, changed);
    }

    private void RemovePhaseGroup(WorldObject obj, List<uint> phasesInGroup, bool updateVisibility, ControlledUnitVisitor visitor)
    {
        var changed = false;

        foreach (var phaseId in phasesInGroup)
            changed = obj.Location.PhaseShift.RemovePhase(phaseId) || changed;

        var unit = obj.AsUnit;

        if (unit != null)
        {
            unit.OnPhaseChange();
            visitor.VisitControlledOf(unit, controlled => { RemovePhaseGroup(controlled, phasesInGroup, updateVisibility, visitor); });
            unit.RemoveNotOwnSingleTargetAuras(true);
        }

        UpdateVisibilityIfNeeded(obj, updateVisibility, changed);
    }

    private void RemoveVisibleMapId(WorldObject obj, uint visibleMapId, ControlledUnitVisitor visitor)
    {
        var terrainSwapInfo = _objectManager.GetTerrainSwapInfo(visibleMapId);
        var changed = obj.Location.PhaseShift.RemoveVisibleMapId(visibleMapId);

        foreach (var uiWorldMapAreaIDSwap in terrainSwapInfo.UiMapPhaseIDs)
            changed = obj.Location.PhaseShift.RemoveUiMapPhaseId(uiWorldMapAreaIDSwap) || changed;

        var unit = obj.AsUnit;

        if (unit != null)
            visitor.VisitControlledOf(unit, controlled => { RemoveVisibleMapId(controlled, visibleMapId, visitor); });

        UpdateVisibilityIfNeeded(obj, false, changed);
    }

    private void UpdateVisibilityIfNeeded(WorldObject obj, bool updateVisibility, bool changed)
    {
        if (!changed || !obj.Location.IsInWorld)
            return;

        var player = obj.AsPlayer;

        if (player != null)
            SendToPlayer(player);

        if (!updateVisibility)
            return;

        player?.Location.Map.SendUpdateTransportVisibility(player);

        obj.UpdateObjectVisibility();
    }
}