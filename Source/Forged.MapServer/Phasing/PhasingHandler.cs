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
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Grids;
using Forged.MapServer.Networking.Packets.Misc;
using Forged.MapServer.Networking.Packets.Party;
using Framework.Constants;

namespace Forged.MapServer.Phasing;

public class PhasingHandler
{
	public static PhaseShift EmptyPhaseShift = new();
	public static PhaseShift AlwaysVisible;

	static PhasingHandler()
	{
		AlwaysVisible = new PhaseShift();
		InitDbPhaseShift(AlwaysVisible, PhaseUseFlagsValues.AlwaysVisible, 0, 0);
	}

	public static PhaseFlags GetPhaseFlags(uint phaseId)
	{
		var phase = CliDB.PhaseStorage.LookupByKey(phaseId);

		if (phase != null)
		{
			if (phase.Flags.HasAnyFlag(PhaseEntryFlags.Cosmetic))
				return PhaseFlags.Cosmetic;

			if (phase.Flags.HasAnyFlag(PhaseEntryFlags.Personal))
				return PhaseFlags.Personal;
		}

		return PhaseFlags.None;
	}

	public static void ForAllControlled(Unit unit, Action<Unit> func)
	{
		for (var i = 0; i < unit.Controlled.Count; ++i)
		{
			var controlled = unit.Controlled[i];

			if (controlled.TypeId != TypeId.Player && controlled.Vehicle1 == null) // Player inside nested vehicle should not phase the root vehicle and its accessories (only direct root vehicle control does)
				func(controlled);
		}

		for (byte i = 0; i < SharedConst.MaxSummonSlot; ++i)
			if (!unit.SummonSlot[i].IsEmpty)
			{
				var summon = unit.Map.GetCreature(unit.SummonSlot[i]);

				if (summon)
					func(summon);
			}

		var vehicle = unit.VehicleKit1;

		if (vehicle != null)
			foreach (var seat in vehicle.Seats)
			{
				var passenger = Global.ObjAccessor.GetUnit(unit, seat.Value.Passenger.Guid);

				if (passenger != null)
					func(passenger);
			}
	}

	public static void AddPhase(WorldObject obj, uint phaseId, bool updateVisibility)
	{
		ControlledUnitVisitor visitor = new(obj);
		AddPhase(obj, phaseId, obj.GUID, updateVisibility, visitor);
	}

	public static void RemovePhase(WorldObject obj, uint phaseId, bool updateVisibility)
	{
		ControlledUnitVisitor visitor = new(obj);
		RemovePhase(obj, phaseId, updateVisibility, visitor);
	}

	public static void AddPhaseGroup(WorldObject obj, uint phaseGroupId, bool updateVisibility)
	{
		var phasesInGroup = Global.DB2Mgr.GetPhasesForGroup(phaseGroupId);

		if (phasesInGroup.Empty())
			return;

		ControlledUnitVisitor visitor = new(obj);
		AddPhaseGroup(obj, phasesInGroup, obj.GUID, updateVisibility, visitor);
	}

	public static void RemovePhaseGroup(WorldObject obj, uint phaseGroupId, bool updateVisibility)
	{
		var phasesInGroup = Global.DB2Mgr.GetPhasesForGroup(phaseGroupId);

		if (phasesInGroup.Empty())
			return;

		ControlledUnitVisitor visitor = new(obj);
		RemovePhaseGroup(obj, phasesInGroup, updateVisibility, visitor);
	}

	public static void AddVisibleMapId(WorldObject obj, uint visibleMapId)
	{
		ControlledUnitVisitor visitor = new(obj);
		AddVisibleMapId(obj, visibleMapId, visitor);
	}

	public static void RemoveVisibleMapId(WorldObject obj, uint visibleMapId)
	{
		ControlledUnitVisitor visitor = new(obj);
		RemoveVisibleMapId(obj, visibleMapId, visitor);
	}

	public static void ResetPhaseShift(WorldObject obj)
	{
		obj.PhaseShift.Clear();
		obj.SuppressedPhaseShift.Clear();
	}

	public static void InheritPhaseShift(WorldObject target, WorldObject source)
	{
		target.PhaseShift = source.PhaseShift;
		target.SuppressedPhaseShift = source.SuppressedPhaseShift;
	}

	public static void OnMapChange(WorldObject obj)
	{
		var phaseShift = obj.PhaseShift;
		var suppressedPhaseShift = obj.SuppressedPhaseShift;
		ConditionSourceInfo srcInfo = new(obj);

		obj.PhaseShift.VisibleMapIds.Clear();
		obj.PhaseShift.UiMapPhaseIds.Clear();
		obj.SuppressedPhaseShift.VisibleMapIds.Clear();

		foreach (var (mapId, visibleMapInfo) in Global.ObjectMgr.GetTerrainSwaps().KeyValueList)
			if (Global.ConditionMgr.IsObjectMeetingNotGroupedConditions(ConditionSourceType.TerrainSwap, visibleMapInfo.Id, srcInfo))
			{
				if (mapId == obj.Location.MapId)
					phaseShift.AddVisibleMapId(visibleMapInfo.Id, visibleMapInfo);

				// ui map is visible on all maps
				foreach (var uiMapPhaseId in visibleMapInfo.UiMapPhaseIDs)
					phaseShift.AddUiMapPhaseId(uiMapPhaseId);
			}
			else if (mapId == obj.Location.MapId)
			{
				suppressedPhaseShift.AddVisibleMapId(visibleMapInfo.Id, visibleMapInfo);
			}

		UpdateVisibilityIfNeeded(obj, false, true);
	}

	public static void OnAreaChange(WorldObject obj)
	{
		var phaseShift = obj.PhaseShift;
		var suppressedPhaseShift = obj.SuppressedPhaseShift;
		var oldPhases = phaseShift.Phases; // for comparison
		ConditionSourceInfo srcInfo = new(obj);

		obj.PhaseShift.ClearPhases();
		obj.SuppressedPhaseShift.ClearPhases();

		var areaId = obj.Area;
		var areaEntry = CliDB.AreaTableStorage.LookupByKey(areaId);

		while (areaEntry != null)
		{
			var newAreaPhases = Global.ObjectMgr.GetPhasesForArea(areaEntry.Id);

			if (!newAreaPhases.Empty())
				foreach (var phaseArea in newAreaPhases)
				{
					if (phaseArea.SubAreaExclusions.Contains(areaId))
						continue;

					var phaseId = phaseArea.PhaseInfo.Id;

					if (Global.ConditionMgr.IsObjectMeetToConditions(srcInfo, phaseArea.Conditions))
						phaseShift.AddPhase(phaseId, GetPhaseFlags(phaseId), phaseArea.Conditions);
					else
						suppressedPhaseShift.AddPhase(phaseId, GetPhaseFlags(phaseId), phaseArea.Conditions);
				}

			areaEntry = CliDB.AreaTableStorage.LookupByKey(areaEntry.ParentAreaID);
		}

		var changed = phaseShift.Phases != oldPhases;
		var unit = obj.AsUnit;

		if (unit)
		{
			foreach (var aurEff in unit.GetAuraEffectsByType(AuraType.Phase))
			{
				var phaseId = (uint)aurEff.MiscValueB;
				changed = phaseShift.AddPhase(phaseId, GetPhaseFlags(phaseId), null) || changed;
			}

			foreach (var aurEff in unit.GetAuraEffectsByType(AuraType.PhaseGroup))
			{
				var phasesInGroup = Global.DB2Mgr.GetPhasesForGroup((uint)aurEff.MiscValueB);

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

	public static bool OnConditionChange(WorldObject obj, bool updateVisibility = true)
	{
		var phaseShift = obj.PhaseShift;
		var suppressedPhaseShift = obj.SuppressedPhaseShift;
		PhaseShift newSuppressions = new();
		ConditionSourceInfo srcInfo = new(obj);
		var changed = false;

		foreach (var pair in phaseShift.Phases.ToList())
			if (pair.Value.AreaConditions != null && !Global.ConditionMgr.IsObjectMeetToConditions(srcInfo, pair.Value.AreaConditions))
			{
				newSuppressions.AddPhase(pair.Key, pair.Value.Flags, pair.Value.AreaConditions, pair.Value.References);
				phaseShift.ModifyPhasesReferences(pair.Key, pair.Value, -pair.Value.References);
				phaseShift.Phases.Remove(pair.Key);
			}

		foreach (var pair in suppressedPhaseShift.Phases.ToList())
			if (Global.ConditionMgr.IsObjectMeetToConditions(srcInfo, pair.Value.AreaConditions))
			{
				changed = phaseShift.AddPhase(pair.Key, pair.Value.Flags, pair.Value.AreaConditions, pair.Value.References) || changed;
				suppressedPhaseShift.ModifyPhasesReferences(pair.Key, pair.Value, -pair.Value.References);
				suppressedPhaseShift.Phases.Remove(pair.Key);
			}

		foreach (var pair in phaseShift.VisibleMapIds.ToList())
			if (!Global.ConditionMgr.IsObjectMeetingNotGroupedConditions(ConditionSourceType.TerrainSwap, pair.Key, srcInfo))
			{
				newSuppressions.AddVisibleMapId(pair.Key, pair.Value.VisibleMapInfo, pair.Value.References);

				foreach (var uiMapPhaseId in pair.Value.VisibleMapInfo.UiMapPhaseIDs)
					changed = phaseShift.RemoveUiMapPhaseId(uiMapPhaseId) || changed;

				phaseShift.VisibleMapIds.Remove(pair.Key);
			}

		foreach (var pair in suppressedPhaseShift.VisibleMapIds.ToList())
			if (Global.ConditionMgr.IsObjectMeetingNotGroupedConditions(ConditionSourceType.TerrainSwap, pair.Key, srcInfo))
			{
				changed = phaseShift.AddVisibleMapId(pair.Key, pair.Value.VisibleMapInfo, pair.Value.References) || changed;

				foreach (var uiMapPhaseId in pair.Value.VisibleMapInfo.UiMapPhaseIDs)
					changed = phaseShift.AddUiMapPhaseId(uiMapPhaseId) || changed;

				suppressedPhaseShift.VisibleMapIds.Remove(pair.Key);
			}

		var unit = obj.AsUnit;

		if (unit)
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
				var phasesInGroup = Global.DB2Mgr.GetPhasesForGroup((uint)aurEff.MiscValueB);

				if (!phasesInGroup.Empty())
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

		if (unit)
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

	public static void SendToPlayer(Player player, PhaseShift phaseShift)
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

	public static void SendToPlayer(Player player)
	{
		SendToPlayer(player, player.PhaseShift);
	}

	public static void FillPartyMemberPhase(PartyMemberPhaseStates partyMemberPhases, PhaseShift phaseShift)
	{
		partyMemberPhases.PhaseShiftFlags = (int)phaseShift.Flags;
		partyMemberPhases.PersonalGUID = phaseShift.PersonalGuid;

		foreach (var pair in phaseShift.Phases)
			partyMemberPhases.List.Add(new PartyMemberPhase((uint)pair.Value.Flags, pair.Key));
	}

	public static PhaseShift GetAlwaysVisiblePhaseShift()
	{
		return AlwaysVisible;
	}

	public static void InitDbPhaseShift(PhaseShift phaseShift, PhaseUseFlagsValues phaseUseFlags, uint phaseId, uint phaseGroupId)
	{
		phaseShift.ClearPhases();
		phaseShift.IsDbPhaseShift = true;

		var flags = PhaseShiftFlags.None;

		if (phaseUseFlags.HasAnyFlag(PhaseUseFlagsValues.AlwaysVisible))
			flags = flags | PhaseShiftFlags.AlwaysVisible | PhaseShiftFlags.Unphased;

		if (phaseUseFlags.HasAnyFlag(PhaseUseFlagsValues.Inverse))
			flags |= PhaseShiftFlags.Inverse;

		if (phaseId != 0)
		{
			phaseShift.AddPhase(phaseId, GetPhaseFlags(phaseId), null);
		}
		else
		{
			var phasesInGroup = Global.DB2Mgr.GetPhasesForGroup(phaseGroupId);

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

	public static void InitDbPersonalOwnership(PhaseShift phaseShift, ObjectGuid personalGuid)
	{
		phaseShift.PersonalGuid = personalGuid;
	}

	public static void InitDbVisibleMapId(PhaseShift phaseShift, int visibleMapId)
	{
		phaseShift.VisibleMapIds.Clear();

		if (visibleMapId != -1)
			phaseShift.AddVisibleMapId((uint)visibleMapId, Global.ObjectMgr.GetTerrainSwapInfo((uint)visibleMapId));
	}

	public static bool InDbPhaseShift(WorldObject obj, PhaseUseFlagsValues phaseUseFlags, ushort phaseId, uint phaseGroupId)
	{
		PhaseShift phaseShift = new();
		InitDbPhaseShift(phaseShift, phaseUseFlags, phaseId, phaseGroupId);

		return obj.PhaseShift.CanSee(phaseShift);
	}

	public static uint GetTerrainMapId(PhaseShift phaseShift, uint mapId, TerrainInfo terrain, float x, float y)
	{
		if (phaseShift.VisibleMapIds.Empty())
			return mapId;

		if (phaseShift.VisibleMapIds.Count == 1)
			return phaseShift.VisibleMapIds.First().Key;

		var gridCoord = GridDefines.ComputeGridCoord(x, y);
		var gx = (int)((MapConst.MaxGrids - 1) - gridCoord.X_Coord);
		var gy = (int)((MapConst.MaxGrids - 1) - gridCoord.Y_Coord);

		foreach (var visibleMap in phaseShift.VisibleMapIds)
			if (terrain.HasChildTerrainGridFile(visibleMap.Key, gx, gy))
				return visibleMap.Key;

		return mapId;
	}

	public static void SetAlwaysVisible(WorldObject obj, bool apply, bool updateVisibility)
	{
		if (apply)
			obj.PhaseShift.Flags |= PhaseShiftFlags.AlwaysVisible;
		else
			obj.PhaseShift.Flags &= ~PhaseShiftFlags.AlwaysVisible;

		UpdateVisibilityIfNeeded(obj, updateVisibility, true);
	}

	public static void SetInversed(WorldObject obj, bool apply, bool updateVisibility)
	{
		if (apply)
			obj.PhaseShift.Flags |= PhaseShiftFlags.Inverse;
		else
			obj.PhaseShift.Flags &= ~PhaseShiftFlags.Inverse;

		obj.PhaseShift.UpdateUnphasedFlag();

		UpdateVisibilityIfNeeded(obj, updateVisibility, true);
	}

	public static void PrintToChat(CommandHandler chat, WorldObject target)
	{
		var phaseShift = target.PhaseShift;

		var phaseOwnerName = "N/A";

		if (phaseShift.HasPersonalPhase)
		{
			var personalGuid = Global.ObjAccessor.GetWorldObject(target, phaseShift.PersonalGuid);

			if (personalGuid != null)
				phaseOwnerName = personalGuid.GetName();
		}

		chat.SendSysMessage(CypherStrings.PhaseshiftStatus, phaseShift.Flags, phaseShift.PersonalGuid.ToString(), phaseOwnerName);

		if (!phaseShift.Phases.Empty())
		{
			StringBuilder phases = new();
			var cosmetic = Global.ObjectMgr.GetCypherString(CypherStrings.PhaseFlagCosmetic, chat.SessionDbcLocale);
			var personal = Global.ObjectMgr.GetCypherString(CypherStrings.PhaseFlagPersonal, chat.SessionDbcLocale);

			foreach (var pair in phaseShift.Phases)
			{
				phases.Append("\r\n");
				phases.Append("   ");
				phases.Append($"{pair.Key} ({Global.ObjectMgr.GetPhaseName(pair.Key)})'");

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

	public static string FormatPhases(PhaseShift phaseShift)
	{
		StringBuilder phases = new();

		foreach (var phaseId in phaseShift.Phases.Keys)
			phases.Append(phaseId + ',');

		return phases.ToString();
	}

	public static bool IsPersonalPhase(uint phaseId)
	{
		var phase = CliDB.PhaseStorage.LookupByKey(phaseId);

		if (phase != null)
			return phase.Flags.HasFlag(PhaseEntryFlags.Personal);

		return false;
	}

	static void AddPhase(WorldObject obj, uint phaseId, ObjectGuid personalGuid, bool updateVisibility, ControlledUnitVisitor visitor)
	{
		var changed = obj.PhaseShift.AddPhase(phaseId, GetPhaseFlags(phaseId), null);

		if (obj.PhaseShift.PersonalReferences != 0)
			obj.PhaseShift.PersonalGuid = personalGuid;

		var unit = obj.AsUnit;

		if (unit)
		{
			unit.OnPhaseChange();
			visitor.VisitControlledOf(unit, controlled => { AddPhase(controlled, phaseId, personalGuid, updateVisibility, visitor); });
			unit.RemoveNotOwnSingleTargetAuras(true);
		}

		UpdateVisibilityIfNeeded(obj, updateVisibility, changed);
	}

	static void RemovePhase(WorldObject obj, uint phaseId, bool updateVisibility, ControlledUnitVisitor visitor)
	{
		var changed = obj.PhaseShift.RemovePhase(phaseId);

		var unit = obj.AsUnit;

		if (unit)
		{
			unit.OnPhaseChange();
			visitor.VisitControlledOf(unit, controlled => { RemovePhase(controlled, phaseId, updateVisibility, visitor); });
			unit.RemoveNotOwnSingleTargetAuras(true);
		}

		UpdateVisibilityIfNeeded(obj, updateVisibility, changed);
	}

	static void AddPhaseGroup(WorldObject obj, List<uint> phasesInGroup, ObjectGuid personalGuid, bool updateVisibility, ControlledUnitVisitor visitor)
	{
		var changed = false;

		foreach (var phaseId in phasesInGroup)
			changed = obj.PhaseShift.AddPhase(phaseId, GetPhaseFlags(phaseId), null) || changed;

		if (obj.PhaseShift.PersonalReferences != 0)
			obj.PhaseShift.PersonalGuid = personalGuid;

		var unit = obj.AsUnit;

		if (unit)
		{
			unit.OnPhaseChange();
			visitor.VisitControlledOf(unit, controlled => { AddPhaseGroup(controlled, phasesInGroup, personalGuid, updateVisibility, visitor); });
			unit.RemoveNotOwnSingleTargetAuras(true);
		}

		UpdateVisibilityIfNeeded(obj, updateVisibility, changed);
	}

	static void RemovePhaseGroup(WorldObject obj, List<uint> phasesInGroup, bool updateVisibility, ControlledUnitVisitor visitor)
	{
		var changed = false;

		foreach (var phaseId in phasesInGroup)
			changed = obj.PhaseShift.RemovePhase(phaseId) || changed;

		var unit = obj.AsUnit;

		if (unit)
		{
			unit.OnPhaseChange();
			visitor.VisitControlledOf(unit, controlled => { RemovePhaseGroup(controlled, phasesInGroup, updateVisibility, visitor); });
			unit.RemoveNotOwnSingleTargetAuras(true);
		}

		UpdateVisibilityIfNeeded(obj, updateVisibility, changed);
	}

	static void AddVisibleMapId(WorldObject obj, uint visibleMapId, ControlledUnitVisitor visitor)
	{
		var terrainSwapInfo = Global.ObjectMgr.GetTerrainSwapInfo(visibleMapId);
		var changed = obj.PhaseShift.AddVisibleMapId(visibleMapId, terrainSwapInfo);

		foreach (var uiMapPhaseId in terrainSwapInfo.UiMapPhaseIDs)
			changed = obj.PhaseShift.AddUiMapPhaseId(uiMapPhaseId) || changed;

		var unit = obj.AsUnit;

		if (unit)
			visitor.VisitControlledOf(unit, controlled => { AddVisibleMapId(controlled, visibleMapId, visitor); });

		UpdateVisibilityIfNeeded(obj, false, changed);
	}

	static void RemoveVisibleMapId(WorldObject obj, uint visibleMapId, ControlledUnitVisitor visitor)
	{
		var terrainSwapInfo = Global.ObjectMgr.GetTerrainSwapInfo(visibleMapId);
		var changed = obj.PhaseShift.RemoveVisibleMapId(visibleMapId);

		foreach (var uiWorldMapAreaIDSwap in terrainSwapInfo.UiMapPhaseIDs)
			changed = obj.PhaseShift.RemoveUiMapPhaseId(uiWorldMapAreaIDSwap) || changed;

		var unit = obj.AsUnit;

		if (unit)
			visitor.VisitControlledOf(unit, controlled => { RemoveVisibleMapId(controlled, visibleMapId, visitor); });

		UpdateVisibilityIfNeeded(obj, false, changed);
	}

	static void UpdateVisibilityIfNeeded(WorldObject obj, bool updateVisibility, bool changed)
	{
		if (changed && obj.IsInWorld)
		{
			var player = obj.AsPlayer;

			if (player)
				SendToPlayer(player);

			if (updateVisibility)
			{
				if (player)
					player.Map.SendUpdateTransportVisibility(player);

				obj.UpdateObjectVisibility();
			}
		}
	}
}