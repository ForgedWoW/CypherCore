// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Chrono;
using Forged.MapServer.DataStorage.Structs.A;
using Forged.MapServer.DataStorage.Structs.M;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Grids;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Networking.Packets.Instance;
using Forged.MapServer.Networking.Packets.Misc;
using Forged.MapServer.Networking.Packets.Movement;
using Forged.MapServer.Phasing;
using Forged.MapServer.Scripting.Interfaces.IPlayer;
using Framework.Constants;

namespace Forged.MapServer.Entities.Players;

public partial class Player
{
    public Difficulty DungeonDifficultyId { get; set; }

    public Difficulty LegacyRaidDifficultyId { get; set; }
    public ZonePVPTypeOverride OverrideZonePvpType
    {
        get => (ZonePVPTypeOverride)(uint)ActivePlayerData.OverrideZonePVPType;
        set => SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.OverrideZonePVPType), (uint)value);
    }

    public Difficulty RaidDifficultyId { get; set; }
    public static Difficulty CheckLoadedDungeonDifficultyId(Difficulty difficulty)
    {
        var difficultyEntry = CliDB.DifficultyStorage.LookupByKey(difficulty);

        if (difficultyEntry is not { InstanceType: MapTypes.Instance })
            return Difficulty.Normal;

        if (!difficultyEntry.Flags.HasAnyFlag(DifficultyFlags.CanSelect))
            return Difficulty.Normal;

        return difficulty;
    }

    public static Difficulty CheckLoadedLegacyRaidDifficultyId(Difficulty difficulty)
    {
        var difficultyEntry = CliDB.DifficultyStorage.LookupByKey(difficulty);

        if (difficultyEntry is not { InstanceType: MapTypes.Raid })
            return Difficulty.Raid10N;

        if (!difficultyEntry.Flags.HasAnyFlag(DifficultyFlags.CanSelect) || !difficultyEntry.Flags.HasAnyFlag(DifficultyFlags.Legacy))
            return Difficulty.Raid10N;

        return difficulty;
    }

    public static Difficulty CheckLoadedRaidDifficultyId(Difficulty difficulty)
    {
        var difficultyEntry = CliDB.DifficultyStorage.LookupByKey(difficulty);

        if (difficultyEntry is not { InstanceType: MapTypes.Raid })
            return Difficulty.NormalRaid;

        if (!difficultyEntry.Flags.HasAnyFlag(DifficultyFlags.CanSelect) || difficultyEntry.Flags.HasAnyFlag(DifficultyFlags.Legacy))
            return Difficulty.NormalRaid;

        return difficulty;
    }

    public void AddInstanceEnterTime(uint instanceId, long enterTime)
    {
        if (!_instanceResetTimes.ContainsKey(instanceId))
            _instanceResetTimes.Add(instanceId, enterTime + Time.HOUR);
    }

    public bool CheckInstanceCount(uint instanceId)
    {
        if (_instanceResetTimes.Count < GetDefaultValue("AccountInstancesPerHour", 5))
            return true;

        return _instanceResetTimes.ContainsKey(instanceId);
    }

    public bool CheckInstanceValidity(bool isLogin)
    {
        // GameInfo masters' instances are always valid
        if (IsGameMaster)
            return true;

        // non-instances are always valid
        var map = Location.Map;
        var instance = map?.ToInstanceMap;

        if (instance == null)
            return true;

        var group = Group;

        // raid instances require the player to be in a raid group to be valid
        if (map.IsRaid && !GetDefaultValue("Instance.IgnoreRaid", false) && (map.Entry.Expansion() >= (Expansion)GetDefaultValue("Expansion", (int)Expansion.Dragonflight)))
            if (group == null || group.IsRaidGroup)
                return false;

        if (group)
        {
            // check if player's group is bound to this instance
            if (group != instance.GetOwningGroup())
                return false;
        }
        else
        {
            // instance is invalid if we are not grouped and there are other players
            if (map.GetPlayersCountExceptGMs() > 1)
                return false;
        }

        return true;
    }

    public void ConfirmPendingBind()
    {
        var map = Location.Map.ToInstanceMap;

        if (map == null || map.InstanceId != _pendingBindId)
            return;

        if (!IsGameMaster)
            map.CreateInstanceLockForPlayer(this);
    }

    public Difficulty GetDifficultyId(MapRecord mapEntry)
    {
        if (!mapEntry.IsRaid())
            return DungeonDifficultyId;

        var defaultDifficulty = Global.DB2Mgr.GetDefaultMapDifficulty(mapEntry.Id);

        if (defaultDifficulty == null)
            return LegacyRaidDifficultyId;

        var difficulty = CliDB.DifficultyStorage.LookupByKey(defaultDifficulty.DifficultyID);

        if (difficulty == null || difficulty.Flags.HasAnyFlag(DifficultyFlags.Legacy))
            return LegacyRaidDifficultyId;

        return RaidDifficultyId;
    }
    public uint GetRecentInstanceId(uint mapId)
    {
        return _recentInstances.LookupByKey(mapId);
    }

    public bool IsLockedToDungeonEncounter(uint dungeonEncounterId)
    {
        var dungeonEncounter = CliDB.DungeonEncounterStorage.LookupByKey(dungeonEncounterId);

        if (dungeonEncounter == null)
            return false;

        var instanceLock = Global.InstanceLockMgr.FindActiveInstanceLock(GUID, new MapDb2Entries(Location.Map.Entry, Location.Map.MapDifficulty));

        if (instanceLock == null)
            return false;

        return (instanceLock.GetData().CompletedEncountersMask & (1u << dungeonEncounter.Bit)) != 0;
    }

    public override void ProcessTerrainStatusUpdate(ZLiquidStatus oldLiquidStatus, LiquidData newLiquidData)
    {
        // process liquid auras using generic unit code
        base.ProcessTerrainStatusUpdate(oldLiquidStatus, newLiquidData);

        _mirrorTimerFlags &= ~(PlayerUnderwaterState.InWater | PlayerUnderwaterState.InLava | PlayerUnderwaterState.InSlime | PlayerUnderwaterState.InDarkWater);

        // player specific logic for mirror timers
        if (Location.LiquidStatus != 0 && newLiquidData != null)
        {
            // Breath bar state (under water in any liquid type)
            if (newLiquidData.type_flags.HasAnyFlag(LiquidHeaderTypeFlags.AllLiquids))
                if (Location.LiquidStatus.HasAnyFlag(ZLiquidStatus.UnderWater))
                    _mirrorTimerFlags |= PlayerUnderwaterState.InWater;

            // Fatigue bar state (if not on flight path or transport)
            if (newLiquidData.type_flags.HasAnyFlag(LiquidHeaderTypeFlags.DarkWater) && !IsInFlight && Transport == null)
                _mirrorTimerFlags |= PlayerUnderwaterState.InDarkWater;

            // Lava state (any contact)
            if (newLiquidData.type_flags.HasAnyFlag(LiquidHeaderTypeFlags.Magma))
                if (Location.LiquidStatus.HasAnyFlag(ZLiquidStatus.InContact))
                    _mirrorTimerFlags |= PlayerUnderwaterState.InLava;

            // Slime state (any contact)
            if (newLiquidData.type_flags.HasAnyFlag(LiquidHeaderTypeFlags.Slime))
                if (Location.LiquidStatus.HasAnyFlag(ZLiquidStatus.InContact))
                    _mirrorTimerFlags |= PlayerUnderwaterState.InSlime;
        }

        if (HasAuraType(AuraType.ForceBeathBar))
            _mirrorTimerFlags |= PlayerUnderwaterState.InWater;
    }

    // Reset all solo instances and optionally send a message on success for each
    public void ResetInstances(InstanceResetMethod method)
    {
        foreach (var (mapId, instanceId) in _recentInstances.ToList())
        {
            var map = Global.MapMgr.FindMap(mapId, instanceId);
            var forgetInstance = false;

            if (map)
            {
                var instance = map.ToInstanceMap;

                if (instance != null)
                    switch (instance.Reset(method))
                    {
                        case InstanceResetResult.Success:
                            SendResetInstanceSuccess(map.Id);
                            forgetInstance = true;

                            break;
                        case InstanceResetResult.NotEmpty:
                            if (method == InstanceResetMethod.Manual)
                                SendResetInstanceFailed(ResetFailedReason.Failed, map.Id);
                            else if (method == InstanceResetMethod.OnChangeDifficulty)
                                forgetInstance = true;

                            break;
                        case InstanceResetResult.CannotReset:
                            break;
                    }
            }

            if (forgetInstance)
                _recentInstances.Remove(mapId);
        }
    }

    public bool Satisfy(AccessRequirement ar, uint targetMap, TransferAbortParams abortParams = null, bool report = false)
    {
        if (!IsGameMaster)
        {
            byte levelMin = 0;
            byte levelMax = 0;
            uint failedMapDifficultyXCondition = 0;
            uint missingItem = 0;
            uint missingQuest = 0;
            uint missingAchievement = 0;

            var mapEntry = CliDB.MapStorage.LookupByKey(targetMap);

            if (mapEntry == null)
                return false;

            var targetDifficulty = GetDifficultyId(mapEntry);
            var mapDiff = Global.DB2Mgr.GetDownscaledMapDifficultyData(targetMap, ref targetDifficulty);

            if (!GetDefaultValue("Instance.IgnoreLevel", false))
            {
                var mapDifficultyConditions = Global.DB2Mgr.GetMapDifficultyConditions(mapDiff.Id);

                foreach (var pair in mapDifficultyConditions)
                    if (!ConditionManager.IsPlayerMeetingCondition(this, pair.Item2))
                    {
                        failedMapDifficultyXCondition = pair.Item1;

                        break;
                    }
            }

            if (ar != null)
            {
                if (!GetDefaultValue("Instance.IgnoreLevel", false))
                {
                    if (ar.LevelMin != 0 && Level < ar.LevelMin)
                        levelMin = ar.LevelMin;

                    if (ar.LevelMax != 0 && Level > ar.LevelMax)
                        levelMax = ar.LevelMax;
                }

                if (ar.Item != 0)
                {
                    if (!HasItemCount(ar.Item) &&
                        (ar.Item2 == 0 || !HasItemCount(ar.Item2)))
                        missingItem = ar.Item;
                }
                else if (ar.Item2 != 0 && !HasItemCount(ar.Item2))
                {
                    missingItem = ar.Item2;
                }

                if (Team == TeamFaction.Alliance && ar.QuestA != 0 && !GetQuestRewardStatus(ar.QuestA))
                    missingQuest = ar.QuestA;
                else if (Team == TeamFaction.Horde && ar.QuestH != 0 && !GetQuestRewardStatus(ar.QuestH))
                    missingQuest = ar.QuestH;

                var leader = this;
                var leaderGuid = Group?.LeaderGUID ?? GUID;

                if (leaderGuid != GUID)
                    leader = Global.ObjAccessor.FindPlayer(leaderGuid);

                if (ar.Achievement != 0)
                    if (leader == null || !leader.HasAchieved(ar.Achievement))
                        missingAchievement = ar.Achievement;
            }

            if (levelMin != 0 || levelMax != 0 || failedMapDifficultyXCondition != 0 || missingItem != 0 || missingQuest != 0 || missingAchievement != 0)
            {
                if (abortParams != null)
                    abortParams.Reason = TransferAbortReason.Error;

                if (report)
                {
                    if (missingQuest != 0 && ar != null && !string.IsNullOrEmpty(ar.QuestFailedText))
                    {
                        SendSysMessage("{0}", ar.QuestFailedText);
                    }
                    else if (!mapDiff.Message[Global.WorldMgr.DefaultDbcLocale].IsEmpty() && mapDiff.Message[Global.WorldMgr.DefaultDbcLocale][0] != '\0' || failedMapDifficultyXCondition != 0) // if (missingAchievement) covered by this case
                    {
                        if (abortParams != null)
                        {
                            abortParams.Reason = TransferAbortReason.Difficulty;
                            abortParams.Arg = (byte)targetDifficulty;
                            abortParams.MapDifficultyXConditionId = failedMapDifficultyXCondition;
                        }
                    }
                    else if (missingItem != 0)
                    {
                        Session.SendNotification(Global.ObjectMgr.GetCypherString(CypherStrings.LevelMinrequiredAndItem), levelMin, Global.ObjectMgr.GetItemTemplate(missingItem).GetName());
                    }
                    else if (levelMin != 0)
                    {
                        Session.SendNotification(Global.ObjectMgr.GetCypherString(CypherStrings.LevelMinrequired), levelMin);
                    }
                }

                return false;
            }
        }

        return true;
    }

    public void SendDungeonDifficulty(int forcedDifficulty = -1)
    {
        DungeonDifficultySet dungeonDifficultySet = new()
        {
            DifficultyID = forcedDifficulty == -1 ? (int)DungeonDifficultyId : forcedDifficulty
        };

        SendPacket(dungeonDifficultySet);
    }

    public void SendRaidDifficulty(bool legacy, int forcedDifficulty = -1)
    {
        RaidDifficultySet raidDifficultySet = new()
        {
            DifficultyID = forcedDifficulty == -1 ? (int)(legacy ? LegacyRaidDifficultyId : RaidDifficultyId) : forcedDifficulty,
            Legacy = legacy
        };

        SendPacket(raidDifficultySet);
    }

    public void SendRaidGroupOnlyMessage(RaidGroupReason reason, int delay)
    {
        RaidGroupOnly raidGroupOnly = new()
        {
            Delay = delay,
            Reason = reason
        };

        SendPacket(raidGroupOnly);
    }

    public void SendRaidInfo()
    {
        var now = GameTime.SystemTime;

        var instanceLocks = Global.InstanceLockMgr.GetInstanceLocksForPlayer(GUID);

        InstanceInfoPkt instanceInfo = new();

        foreach (var instanceLock in instanceLocks)
        {
            InstanceLockPkt lockInfos = new()
            {
                InstanceID = instanceLock.GetInstanceId(),
                MapID = instanceLock.GetMapId(),
                DifficultyID = (uint)instanceLock.GetDifficultyId(),
                TimeRemaining = (int)Math.Max((instanceLock.GetEffectiveExpiryTime() - now).TotalSeconds, 0),
                CompletedMask = instanceLock.GetData().CompletedEncountersMask,
                Locked = !instanceLock.IsExpired(),
                Extended = instanceLock.IsExtended()
            };

            instanceInfo.LockList.Add(lockInfos);
        }

        SendPacket(instanceInfo);
    }

    public void SendResetFailedNotify(uint mapid)
    {
        SendPacket(new ResetFailedNotify());
    }

    public void SendResetInstanceFailed(ResetFailedReason reason, uint mapId)
    {
        InstanceResetFailed data = new()
        {
            MapID = mapId,
            ResetFailedReason = reason
        };

        SendPacket(data);
    }

    public void SendResetInstanceSuccess(uint mapId)
    {
        InstanceReset data = new()
        {
            MapID = mapId
        };

        SendPacket(data);
    }

    public void SendTransferAborted(uint mapid, TransferAbortReason reason, byte arg = 0, uint mapDifficultyXConditionId = 0)
    {
        TransferAborted transferAborted = new()
        {
            MapID = mapid,
            Arg = arg,
            TransfertAbort = reason,
            MapDifficultyXConditionID = mapDifficultyXConditionId
        };

        SendPacket(transferAborted);
    }

    public void SetPendingBind(uint instanceId, uint bindTimer)
    {
        _pendingBindId = instanceId;
        _pendingBindTimer = bindTimer;
    }

    public void SetRecentInstance(uint mapId, uint instanceId)
    {
        _recentInstances[mapId] = instanceId;
    }

    public void UpdateHostileAreaState(AreaTableRecord area)
    {
        var overrideZonePvpType = OverrideZonePvpType;

        PvpInfo.IsInHostileArea = false;

        if (area.IsSanctuary()) // sanctuary and arena cannot be overriden
        {
            PvpInfo.IsInHostileArea = false;
        }
        else if (area.HasFlag(AreaFlags.Arena))
        {
            PvpInfo.IsInHostileArea = true;
        }
        else if (overrideZonePvpType == ZonePVPTypeOverride.None)
        {
            if (InBattleground || area.HasFlag(AreaFlags.Combat) || (area.PvpCombatWorldStateID != -1 && Global.WorldStateMgr.GetValue(area.PvpCombatWorldStateID, Location.Map) != 0))
            {
                PvpInfo.IsInHostileArea = true;
            }
            else if (IsWarModeLocalActive || area.HasFlag(AreaFlags.Unk3))
            {
                if (area.HasFlag(AreaFlags.ContestedArea))
                {
                    PvpInfo.IsInHostileArea = IsWarModeLocalActive;
                }
                else
                {
                    var factionTemplate = WorldObjectCombat.GetFactionTemplateEntry();

                    if (factionTemplate == null || factionTemplate.FriendGroup.HasAnyFlag(area.FactionGroupMask))
                        PvpInfo.IsInHostileArea = false; // friend area are considered hostile if war mode is active
                    else if (factionTemplate.EnemyGroup.HasAnyFlag(area.FactionGroupMask))
                        PvpInfo.IsInHostileArea = true;
                    else
                        PvpInfo.IsInHostileArea = Global.WorldMgr.IsPvPRealm;
                }
            }
        }
        else
        {
            switch (overrideZonePvpType)
            {
                case ZonePVPTypeOverride.Friendly:
                    PvpInfo.IsInHostileArea = false;

                    break;
                case ZonePVPTypeOverride.Hostile:
                case ZonePVPTypeOverride.Contested:
                case ZonePVPTypeOverride.Combat:
                    PvpInfo.IsInHostileArea = true;

                    break;
            }
        }

        // Treat players having a quest flagging for PvP as always in hostile area
        PvpInfo.IsHostile = PvpInfo.IsInHostileArea || HasPvPForcingQuest() || IsWarModeLocalActive;
    }

    public void UpdateZone(uint newZone, uint newArea)
    {
        if (!Location.IsInWorld)
            return;

        var oldZone = _zoneUpdateId;
        _zoneUpdateId = newZone;
        _zoneUpdateTimer = 1 * Time.IN_MILLISECONDS;

        Location.Map.UpdatePlayerZoneStats(oldZone, newZone);

        // call leave script hooks immedately (before updating flags)
        if (oldZone != newZone)
        {
            Global.OutdoorPvPMgr.HandlePlayerLeaveZone(this, oldZone);
            Global.BattleFieldMgr.HandlePlayerLeaveZone(this, oldZone);
        }

        // group update
        if (Group)
        {
            SetGroupUpdateFlag(GroupUpdateFlags.Full);

            var pet = CurrentPet;

            if (pet)
                pet.GroupUpdateFlag = GroupUpdatePetFlags.Full;
        }

        // zone changed, so area changed as well, update it
        UpdateArea(newArea);

        var zone = CliDB.AreaTableStorage.LookupByKey(newZone);

        if (zone == null)
            return;

        if (GetDefaultValue("ActivateWeather", true))
            Location.Map.GetOrGenerateZoneDefaultWeather(newZone);

        Location.Map.SendZoneDynamicInfo(newZone, this);

        UpdateWarModeAuras();

        UpdateHostileAreaState(zone);

        if (zone.HasFlag(AreaFlags.Capital)) // Is in a capital city
        {
            if (!PvpInfo.IsInHostileArea || zone.IsSanctuary())
                RestMgr.SetRestFlag(RestFlag.City);

            PvpInfo.IsInNoPvPArea = true;
        }
        else
        {
            RestMgr.RemoveRestFlag(RestFlag.City);
        }

        UpdatePvPState();

        // remove items with area/map limitations (delete only for alive player to allow back in ghost mode)
        // if player resurrected at teleport this will be applied in resurrect code
        if (IsAlive)
            DestroyZoneLimitedItem(true, newZone);

        // check some item equip limitations (in result lost CanTitanGrip at talent reset, for example)
        AutoUnequipOffhandIfNeed();

        // recent client version not send leave/join channel packets for built-in local channels
        UpdateLocalChannels(newZone);

        UpdateZoneDependentAuras(newZone);

        // call enter script hooks after everyting else has processed
        Global.ScriptMgr.ForEach<IPlayerOnUpdateZone>(p => p.OnUpdateZone(this, newZone, newArea));

        if (oldZone != newZone)
        {
            Global.OutdoorPvPMgr.HandlePlayerEnterZone(this, newZone);
            Global.BattleFieldMgr.HandlePlayerEnterZone(this, newZone);
            SendInitWorldStates(newZone, newArea); // only if really enters to new zone, not just area change, works strange...
            var guild = Guild;

            if (guild)
                guild.UpdateMemberData(this, GuildMemberData.ZoneId, newZone);
        }
    }
    private bool IsInstanceLoginGameMasterException()
    {
        if (!CanBeGameMaster)
            return false;

        SendSysMessage(CypherStrings.InstanceLoginGamemasterException);

        return true;
    }

    private void UpdateArea(uint newArea)
    {
        // FFA_PVP flags are area and not zone id dependent
        // so apply them accordingly
        _areaUpdateId = newArea;

        var area = CliDB.AreaTableStorage.LookupByKey(newArea);
        var oldFfaPvPArea = PvpInfo.IsInFfaPvPArea;
        PvpInfo.IsInFfaPvPArea = area != null && area.HasFlag(AreaFlags.Arena);
        UpdatePvPState(true);

        // check if we were in ffa arena and we left
        if (oldFfaPvPArea && !PvpInfo.IsInFfaPvPArea)
            ValidateAttackersAndOwnTarget();

        PhasingHandler.OnAreaChange(this);
        UpdateAreaDependentAuras(newArea);

        if (IsAreaThatActivatesPvpTalents(newArea))
            EnablePvpRules();
        else
            DisablePvpRules();

        // previously this was in UpdateZone (but after UpdateArea) so nothing will break
        PvpInfo.IsInNoPvPArea = false;

        if (area != null && area.IsSanctuary()) // in sanctuary
        {
            SetPvpFlag(UnitPVPStateFlags.Sanctuary);
            PvpInfo.IsInNoPvPArea = true;

            if (Duel == null && GetCombatManager().HasPvPCombat())
                CombatStopWithPets();
        }
        else
        {
            RemovePvpFlag(UnitPVPStateFlags.Sanctuary);
        }

        var areaRestFlag = (Team == TeamFaction.Alliance) ? AreaFlags.RestZoneAlliance : AreaFlags.RestZoneHorde;

        if (area != null && area.HasFlag(areaRestFlag))
            RestMgr.SetRestFlag(RestFlag.FactionArea);
        else
            RestMgr.RemoveRestFlag(RestFlag.FactionArea);

        PushQuests();

        UpdateCriteria(CriteriaType.EnterTopLevelArea, newArea);

        UpdateMountCapability();
    }
}