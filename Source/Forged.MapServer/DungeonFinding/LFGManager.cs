// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Forged.MapServer.Chrono;
using Forged.MapServer.Conditions;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Events;
using Forged.MapServer.Globals;
using Forged.MapServer.Groups;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Networking.Packets.LFG;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.DungeonFinding;

public class LFGManager
{
    private readonly Dictionary<ObjectGuid, LfgPlayerBoot> _bootsStore = new();
    private readonly MultiMap<byte, uint> _cachedDungeonMapStore = new();
    private readonly CharacterDatabase _characterDatabase;
    private readonly CliDB _cliDB;
    private readonly IConfiguration _configuration;
    private readonly DB2Manager _db2Manager;
    private readonly DisableManager _disableManager;
    private readonly GameEventManager _gameEventManager;
    private readonly GroupManager _groupManager;
    private readonly Dictionary<ObjectGuid, LFGGroupData> _groupsStore = new();
    private readonly InstanceLockManager _instanceLockManager;
    private readonly Dictionary<uint, LFGDungeonData> _lfgDungeonStore = new();
    private readonly ObjectAccessor _objectAccessor;

    private readonly GameObjectManager _objectManager;

    //< Current player kicks
    private readonly Dictionary<ObjectGuid, LFGPlayerData> _playersStore = new();

    private readonly Dictionary<uint, LfgProposal> _proposalsStore = new();
    private readonly Dictionary<byte, LFGQueue> _queuesStore = new();

    private readonly MultiMap<uint, LfgReward> _rewardMapStore = new();

    //< Stores rewards for random dungeons
    // Rolecheck - Proposal - Vote Kicks
    private readonly Dictionary<ObjectGuid, LfgRoleCheck> _roleChecksStore = new();

    private readonly WorldDatabase _worldDatabase;
    //< Queues

    //< Stores all dungeons by groupType
    // Reward System

    //< Current Role checks
    //< Current Proposals
    //< Player data
    //< Group data

    private uint _lfgProposalId;

    //< used as internal counter for proposals
    private LfgOptions _options;

    // General variables
    private uint _queueTimer; //< used to check interval of update
    //< Stores config options

    public LFGManager(IConfiguration configuration, WorldDatabase worldDatabase, CharacterDatabase characterDatabase, GameObjectManager objectManager, CliDB cliDB,
                      DB2Manager db2Manager, GroupManager groupManager, ObjectAccessor objectAccessor, DisableManager disableManager,
                      InstanceLockManager instanceLockManager, GameEventManager gameEventManager)
    {
        _configuration = configuration;
        _worldDatabase = worldDatabase;
        _characterDatabase = characterDatabase;
        _objectManager = objectManager;
        _cliDB = cliDB;
        _db2Manager = db2Manager;
        _groupManager = groupManager;
        _objectAccessor = objectAccessor;
        _disableManager = disableManager;
        _instanceLockManager = instanceLockManager;
        _gameEventManager = gameEventManager;
        _lfgProposalId = 1;
        _options = (LfgOptions)configuration.GetDefaultValue("DungeonFinder.OptionsMask", 1);

        _ = new LFGPlayerScript();
        _ = new LFGGroupScript();
    }

    public void _LoadFromDB(SQLFields field, ObjectGuid guid)
    {
        if (field == null)
            return;

        if (!guid.IsParty)
            return;

        SetLeader(guid, ObjectGuid.Create(HighGuid.Player, field.Read<ulong>(0)));

        var dungeon = field.Read<uint>(18);
        var state = (LfgState)field.Read<byte>(19);

        if (dungeon == 0 || state == 0)
            return;

        SetDungeon(guid, dungeon);

        switch (state)
        {
            case LfgState.Dungeon:
            case LfgState.FinishedDungeon:
                SetState(guid, state);

                break;
            
        }
    }

    public void AddPlayerToGroup(ObjectGuid gguid, ObjectGuid guid)
    {
        if (!_groupsStore.ContainsKey(gguid))
            _groupsStore[gguid] = new LFGGroupData();

        _groupsStore[gguid].AddPlayer(guid);
    }

    public uint AddProposal(LfgProposal proposal)
    {
        proposal.ID = ++_lfgProposalId;
        _proposalsStore[_lfgProposalId] = proposal;

        return _lfgProposalId;
    }

    public bool AllQueued(List<ObjectGuid> check)
    {
        if (check.Empty())
            return false;

        foreach (var guid in check)
        {
            var state = GetState(guid);

            if (state != LfgState.Queued)
            {
                if (state != LfgState.Proposal)
                    Log.Logger.Debug("Unexpected state found while trying to form new group. Guid: {0}, State: {1}", guid.ToString(), state);

                return false;
            }
        }

        return true;
    }

    public bool CheckGroupRoles(Dictionary<ObjectGuid, LfgRoles> groles)
    {
        if (groles.Empty())
            return false;

        byte damage = 0;
        byte tank = 0;
        byte healer = 0;

        List<ObjectGuid> keys = new(groles.Keys);

        for (var i = 0; i < keys.Count; i++)
        {
            var role = groles[keys[i]] & ~LfgRoles.Leader;

            if (role == LfgRoles.None)
                return false;

            if (role.HasAnyFlag(LfgRoles.Damage))
            {
                if (role != LfgRoles.Damage)
                {
                    groles[keys[i]] -= LfgRoles.Damage;

                    if (CheckGroupRoles(groles))
                        return true;

                    groles[keys[i]] += (byte)LfgRoles.Damage;
                }
                else if (damage == SharedConst.LFGDPSNeeded)
                {
                    return false;
                }
                else
                {
                    damage++;
                }
            }

            if (role.HasAnyFlag(LfgRoles.Healer))
            {
                if (role != LfgRoles.Healer)
                {
                    groles[keys[i]] -= LfgRoles.Healer;

                    if (CheckGroupRoles(groles))
                        return true;

                    groles[keys[i]] += (byte)LfgRoles.Healer;
                }
                else if (healer == SharedConst.LFGHealersNeeded)
                {
                    return false;
                }
                else
                {
                    healer++;
                }
            }

            if (role.HasAnyFlag(LfgRoles.Tank))
            {
                if (role != LfgRoles.Tank)
                {
                    groles[keys[i]] -= LfgRoles.Tank;

                    if (CheckGroupRoles(groles))
                        return true;

                    groles[keys[i]] += (byte)LfgRoles.Tank;
                }
                else if (tank == SharedConst.LFGTanksNeeded)
                {
                    return false;
                }
                else
                {
                    tank++;
                }
            }
        }

        return (tank + healer + damage) == (byte)groles.Count;
    }

    // Only for debugging purposes
    public void Clean()
    {
        _queuesStore.Clear();
    }

    public string ConcatenateDungeons(List<uint> dungeons)
    {
        StringBuilder dungeonstr = new();

        if (!dungeons.Empty())
            foreach (var id in dungeons)
                if (dungeonstr.Capacity != 0)
                    dungeonstr.AppendFormat(", {0}", id);
                else
                    dungeonstr.AppendFormat("{0}", id);

        return dungeonstr.ToString();
    }

    public string DumpQueueInfo(bool full)
    {
        var size = (uint)_queuesStore.Count;

        var str = "Number of Queues: " + size + "\n";

        foreach (var pair in _queuesStore)
        {
            var queued = pair.Value.DumpQueueInfo();
            var compatibles = pair.Value.DumpCompatibleInfo(full);
            str += queued + compatibles;
        }

        return str;
    }

    public void FinishDungeon(ObjectGuid gguid, uint dungeonId, Map currMap)
    {
        var gDungeonId = GetDungeon(gguid);

        if (gDungeonId != dungeonId)
        {
            Log.Logger.Debug($"Group {gguid} finished dungeon {dungeonId} but queued for {gDungeonId}. Ignoring");

            return;
        }

        if (GetState(gguid) == LfgState.FinishedDungeon) // Shouldn't happen. Do not reward multiple times
        {
            Log.Logger.Debug($"Group {gguid} already rewarded");

            return;
        }

        SetState(gguid, LfgState.FinishedDungeon);

        var players = GetPlayers(gguid);

        foreach (var guid in players)
        {
            if (GetState(guid) == LfgState.FinishedDungeon)
            {
                Log.Logger.Debug($"Group: {gguid}, Player: {guid} already rewarded");

                continue;
            }

            uint rDungeonId = 0;
            var dungeons = GetSelectedDungeons(guid);

            if (!dungeons.Empty())
                rDungeonId = dungeons.First();

            SetState(guid, LfgState.FinishedDungeon);

            // Give rewards only if its a random dungeon
            var dungeon = GetLFGDungeon(rDungeonId);

            if (dungeon == null || (dungeon.Type != LfgType.Random && !dungeon.Seasonal))
            {
                Log.Logger.Debug($"Group: {gguid}, Player: {guid} dungeon {rDungeonId} is not random or seasonal");

                continue;
            }

            var player = _objectAccessor.FindPlayer(guid);

            if (player == null)
            {
                Log.Logger.Debug($"Group: {gguid}, Player: {guid} not found in world");

                continue;
            }

            if (player.Location.Map != currMap)
            {
                Log.Logger.Debug($"Group: {gguid}, Player: {guid} is in a different map");

                continue;
            }

            player.RemoveAura(SharedConst.LFGSpellDungeonCooldown);

            var dungeonDone = GetLFGDungeon(dungeonId);
            var mapId = dungeonDone?.Map ?? 0;

            if (player.Location.MapId != mapId)
            {
                Log.Logger.Debug($"Group: {gguid}, Player: {guid} is in map {player.Location.MapId} and should be in {mapId} to get reward");

                continue;
            }

            // Update achievements
            if (dungeon.Difficulty == Difficulty.Heroic)
            {
                byte lfdRandomPlayers = 0;
                var numParty = _playersStore[guid].GetNumberOfPartyMembersAtJoin();

                if (numParty != 0)
                    lfdRandomPlayers = (byte)(5 - numParty);
                else
                    lfdRandomPlayers = 4;

                player.UpdateCriteria(CriteriaType.CompletedLFGDungeonWithStrangers, lfdRandomPlayers);
            }

            var reward = GetRandomDungeonReward(rDungeonId, player.Level);

            if (reward == null)
                continue;

            var done = false;
            var quest = _objectManager.GetQuestTemplate(reward.FirstQuest);

            if (quest == null)
                continue;

            // if we can take the quest, means that we haven't done this kind of "run", IE: First Heroic Random of Day.
            if (player.CanRewardQuest(quest, false))
            {
                player.RewardQuest(quest, LootItemType.Item, 0, null, false);
            }
            else
            {
                done = true;
                quest = _objectManager.GetQuestTemplate(reward.OtherQuest);

                if (quest == null)
                    continue;

                // we give reward without informing client (retail does this)
                player.RewardQuest(quest, LootItemType.Item, 0, null, false);
            }

            // Give rewards
            var doneString = done ? "" : "not";
            Log.Logger.Debug($"Group: {gguid}, Player: {guid} done dungeon {GetDungeon(gguid)}, {doneString} previously done.");
            LfgPlayerRewardData data = new(dungeon.Entry(), GetDungeon(gguid, false), done, quest);
            player.Session.SendLfgPlayerReward(data);
        }
    }

    public uint GetDungeon(ObjectGuid guid, bool asId = true)
    {
        if (!_groupsStore.ContainsKey(guid))
            return 0;

        var dungeon = _groupsStore[guid].GetDungeon(asId);
        Log.Logger.Debug("GetDungeon: [{0}] asId: {1} = {2}", guid, asId, dungeon);

        return dungeon;
    }

    public uint GetDungeonMapId(ObjectGuid guid)
    {
        if (!_groupsStore.ContainsKey(guid))
            return 0;

        var dungeonId = _groupsStore[guid].GetDungeon();
        uint mapId = 0;

        if (dungeonId != 0)
        {
            var dungeon = GetLFGDungeon(dungeonId);

            if (dungeon != null)
                mapId = dungeon.Map;
        }

        Log.Logger.Error("GetDungeonMapId: [{0}] = {1} (DungeonId = {2})", guid, mapId, dungeonId);

        return mapId;
    }

    public LfgType GetDungeonType(uint dungeonId)
    {
        var dungeon = GetLFGDungeon(dungeonId);

        if (dungeon == null)
            return LfgType.None;

        return dungeon.Type;
    }

    public ObjectGuid GetGroup(ObjectGuid guid)
    {
        AddPlayerData(guid);

        return _playersStore[guid].GetGroup();
    }

    public byte GetKicksLeft(ObjectGuid guid)
    {
        var kicks = _groupsStore[guid].GetKicksLeft();
        Log.Logger.Debug("GetKicksLeft: [{0}] = {1}", guid, kicks);

        return kicks;
    }

    public ObjectGuid GetLeader(ObjectGuid guid)
    {
        return _groupsStore[guid].GetLeader();
    }

    public uint GetLFGDungeonEntry(uint id)
    {
        if (id != 0)
        {
            var dungeon = GetLFGDungeon(id);

            if (dungeon != null)
                return dungeon.Entry();
        }

        return 0;
    }

    public LfgUpdateData GetLfgStatus(ObjectGuid guid)
    {
        var playerData = _playersStore[guid];

        return new LfgUpdateData(LfgUpdateType.UpdateStatus, playerData.GetState(), playerData.GetSelectedDungeons());
    }

    public Dictionary<uint, LfgLockInfoData> GetLockedDungeons(ObjectGuid guid)
    {
        Dictionary<uint, LfgLockInfoData> lockDic = new();
        var player = _objectAccessor.FindConnectedPlayer(guid);

        if (!player)
        {
            Log.Logger.Warning("{0} not ingame while retrieving his LockedDungeons.", guid.ToString());

            return lockDic;
        }

        var level = player.Level;
        var expansion = player.Session.Expansion;
        var dungeons = GetDungeonsByRandom(0);
        var denyJoin = !player.Session.HasPermission(RBACPermissions.JoinDungeonFinder);

        foreach (var it in dungeons)
        {
            var dungeon = GetLFGDungeon(it);

            if (dungeon == null) // should never happen - We provide a list from sLFGDungeonStore
                continue;

            LfgLockStatusType lockStatus = 0;
            AccessRequirement ar;

            if (denyJoin)
            {
                lockStatus = LfgLockStatusType.RaidLocked;
            }
            else if (dungeon.Expansion > (uint)expansion)
            {
                lockStatus = LfgLockStatusType.InsufficientExpansion;
            }
            else if (_disableManager.IsDisabledFor(DisableType.Map, dungeon.Map, player))
            {
                lockStatus = LfgLockStatusType.NotInSeason;
            }
            else if (_disableManager.IsDisabledFor(DisableType.LFGMap, dungeon.Map, player))
            {
                lockStatus = LfgLockStatusType.RaidLocked;
            }
            else if (dungeon.Difficulty > Difficulty.Normal && _instanceLockManager.FindActiveInstanceLock(guid, new MapDb2Entries(dungeon.Map, dungeon.Difficulty)) != null)
            {
                lockStatus = LfgLockStatusType.RaidLocked;
            }
            else if (dungeon.Seasonal && !IsSeasonActive(dungeon.ID))
            {
                lockStatus = LfgLockStatusType.NotInSeason;
            }
            else if (dungeon.RequiredItemLevel > player.GetAverageItemLevel())
            {
                lockStatus = LfgLockStatusType.TooLowGearScore;
            }
            else if ((ar = _objectManager.GetAccessRequirement(dungeon.Map, dungeon.Difficulty)) != null)
            {
                if (ar.Achievement != 0 && !player.HasAchieved(ar.Achievement))
                {
                    lockStatus = LfgLockStatusType.MissingAchievement;
                }
                else if (player.Team == TeamFaction.Alliance && ar.QuestA != 0 && !player.GetQuestRewardStatus(ar.QuestA))
                {
                    lockStatus = LfgLockStatusType.QuestNotCompleted;
                }
                else if (player.Team == TeamFaction.Horde && ar.QuestH != 0 && !player.GetQuestRewardStatus(ar.QuestH))
                {
                    lockStatus = LfgLockStatusType.QuestNotCompleted;
                }
                else if (ar.Item != 0)
                {
                    if (!player.HasItemCount(ar.Item) && (ar.Item2 == 0 || !player.HasItemCount(ar.Item2)))
                        lockStatus = LfgLockStatusType.MissingItem;
                }
                else if (ar.Item2 != 0 && !player.HasItemCount(ar.Item2))
                {
                    lockStatus = LfgLockStatusType.MissingItem;
                }
            }
            else
            {
                var levels = _db2Manager.GetContentTuningData(dungeon.ContentTuningId, player.PlayerData.CtrOptions.Value.ContentTuningConditionMask);

                if (levels.HasValue)
                {
                    if (levels.Value.MinLevel > level)
                        lockStatus = LfgLockStatusType.TooLowLevel;

                    if (levels.Value.MaxLevel < level)
                        lockStatus = LfgLockStatusType.TooHighLevel;
                }
            }

            /* @todo VoA closed if WG is not under team control (LFG_LOCKSTATUS_RAID_LOCKED)
            lockData = LFG_LOCKSTATUS_TOO_HIGH_GEAR_SCORE;
            lockData = LFG_LOCKSTATUS_ATTUNEMENT_TOO_LOW_LEVEL;
            lockData = LFG_LOCKSTATUS_ATTUNEMENT_TOO_HIGH_LEVEL;
            */
            if (lockStatus != 0)
                lockDic[dungeon.Entry()] = new LfgLockInfoData(lockStatus, dungeon.RequiredItemLevel, player.GetAverageItemLevel());
        }

        return lockDic;
    }

    public LfgState GetOldState(ObjectGuid guid)
    {
        LfgState state;

        if (guid.IsParty)
        {
            state = _groupsStore[guid].GetOldState();
        }
        else
        {
            AddPlayerData(guid);
            state = _playersStore[guid].GetOldState();
        }

        Log.Logger.Debug("GetOldState: [{0}] = {1}", guid, state);

        return state;
    }

    public LfgOptions GetOptions()
    {
        return _options;
    }

    public byte GetPlayerCount(ObjectGuid guid)
    {
        return _groupsStore[guid].GetPlayerCount();
    }

    public LFGQueue GetQueue(ObjectGuid guid)
    {
        var queueId = GetQueueId(guid);

        if (!_queuesStore.ContainsKey(queueId))
            _queuesStore[queueId] = new LFGQueue();

        return _queuesStore[queueId];
    }

    public byte GetQueueId(ObjectGuid guid)
    {
        if (guid.IsParty)
        {
            var players = GetPlayers(guid);
            var pguid = players.Empty() ? ObjectGuid.Empty : players.First();

            if (!pguid.IsEmpty)
                return (byte)GetTeam(pguid);
        }

        return (byte)GetTeam(guid);
    }

    public long GetQueueJoinTime(ObjectGuid guid)
    {
        var queueId = GetQueueId(guid);
        var lfgQueue = _queuesStore.LookupByKey(queueId);

        if (lfgQueue != null)
            return lfgQueue.GetJoinTime(guid);

        return 0;
    }

    public List<uint> GetRandomAndSeasonalDungeons(uint level, uint expansion, uint contentTuningReplacementConditionMask)
    {
        List<uint> randomDungeons = new();

        foreach (var dungeon in _lfgDungeonStore.Values)
        {
            if (!(dungeon.Type == LfgType.Random || (dungeon.Seasonal && IsSeasonActive(dungeon.ID))))
                continue;

            if (dungeon.Expansion > expansion)
                continue;

            var levels = _db2Manager.GetContentTuningData(dungeon.ContentTuningId, contentTuningReplacementConditionMask);

            if (levels.HasValue)
                if (levels.Value.MinLevel > level || level > levels.Value.MaxLevel)
                    continue;

            randomDungeons.Add(dungeon.Entry());
        }

        return randomDungeons;
    }

    public LfgReward GetRandomDungeonReward(uint dungeon, uint level)
    {
        LfgReward reward = null;
        var bounds = _rewardMapStore.LookupByKey(dungeon & 0x00FFFFFF);

        foreach (var rew in bounds)
        {
            reward = rew;

            // ordered properly at loading
            if (rew.MaxLevel >= level)
                break;
        }

        return reward;
    }

    public LfgRoles GetRoles(ObjectGuid guid)
    {
        var roles = _playersStore[guid].GetRoles();
        Log.Logger.Debug("GetRoles: [{0}] = {1}", guid, roles);

        return roles;
    }

    public List<uint> GetSelectedDungeons(ObjectGuid guid)
    {
        Log.Logger.Debug("GetSelectedDungeons: [{0}]", guid);

        return _playersStore[guid].GetSelectedDungeons();
    }

    public uint GetSelectedRandomDungeon(ObjectGuid guid)
    {
        if (GetState(guid) != LfgState.None)
        {
            var dungeons = GetSelectedDungeons(guid);

            if (!dungeons.Empty())
            {
                var dungeon = GetLFGDungeon(dungeons.First());

                if (dungeon is { Type: LfgType.Raid })
                    return dungeons.First();
            }
        }

        return 0;
    }

    public LfgState GetState(ObjectGuid guid)
    {
        LfgState state;

        if (guid.IsParty)
        {
            if (!_groupsStore.ContainsKey(guid))
                return LfgState.None;

            state = _groupsStore[guid].GetState();
        }
        else
        {
            AddPlayerData(guid);
            state = _playersStore[guid].GetState();
        }

        Log.Logger.Debug("GetState: [{0}] = {1}", guid, state);

        return state;
    }

    public RideTicket GetTicket(ObjectGuid guid)
    {
        var palyerData = _playersStore.LookupByKey(guid);

        return palyerData?.GetTicket();
    }

    public bool HasIgnore(ObjectGuid guid1, ObjectGuid guid2)
    {
        var plr1 = _objectAccessor.FindPlayer(guid1);
        var plr2 = _objectAccessor.FindPlayer(guid2);

        return plr1 != null && plr2 != null && (plr1.Social.HasIgnore(guid2, plr2.Session.AccountGUID) || plr2.Social.HasIgnore(guid1, plr1.Session.AccountGUID));
    }

    public void InitBoot(ObjectGuid gguid, ObjectGuid kicker, ObjectGuid victim, string reason)
    {
        SetVoteKick(gguid, true);

        var boot = _bootsStore[gguid];
        boot.InProgress = true;
        boot.CancelTime = GameTime.CurrentTime + SharedConst.LFGTimeBoot;
        boot.Reason = reason;
        boot.Victim = victim;

        var players = GetPlayers(gguid);

        // Set votes
        foreach (var guid in players)
            boot.Votes[guid] = LfgAnswer.Pending;

        boot.Votes[victim] = LfgAnswer.Deny;  // Victim auto vote NO
        boot.Votes[kicker] = LfgAnswer.Agree; // Kicker auto vote YES

        // Notify players
        foreach (var it in players)
            SendLfgBootProposalUpdate(it, boot);
    }

    public bool InLfgDungeonMap(ObjectGuid guid, uint map, Difficulty difficulty)
    {
        if (!guid.IsParty)
            guid = GetGroup(guid);

        var dungeonId = GetDungeon(guid);

        if (dungeonId != 0)
        {
            var dungeon = GetLFGDungeon(dungeonId);

            if (dungeon != null)
                if (dungeon.Map == map && dungeon.Difficulty == difficulty)
                    return true;
        }

        return false;
    }

    public bool IsLfgGroup(ObjectGuid guid)
    {
        return guid is { IsEmpty: false, IsParty: true } && _groupsStore[guid].IsLfgGroup();
    }

    public bool IsOptionEnabled(LfgOptions option)
    {
        return _options.HasAnyFlag(option);
    }

    public bool IsVoteKickActive(ObjectGuid gguid)
    {
        var active = _groupsStore[gguid].IsVoteKickActive();
        Log.Logger.Information("Group: {0}, Active: {1}", gguid.ToString(), active);

        return active;
    }

    public void JoinLfg(Player player, LfgRoles roles, List<uint> dungeons)
    {
        if (!player || player.Session == null || dungeons.Empty())
            return;

        // Sanitize input roles
        roles &= LfgRoles.Any;
        roles = FilterClassRoles(player, roles);

        // At least 1 role must be selected
        if ((roles & (LfgRoles.Tank | LfgRoles.Healer | LfgRoles.Damage)) == 0)
            return;

        var grp = player.Group;
        var guid = player.GUID;
        var gguid = grp ? grp.GUID : guid;
        LfgJoinResultData joinData = new();
        List<ObjectGuid> players = new();
        uint rDungeonId = 0;
        var isContinue = grp && grp.IsLFGGroup && GetState(gguid) != LfgState.FinishedDungeon;

        // Do not allow to change dungeon in the middle of a current dungeon
        if (isContinue)
        {
            dungeons.Clear();
            dungeons.Add(GetDungeon(gguid));
        }

        // Already in queue?
        var state = GetState(gguid);

        if (state == LfgState.Queued)
        {
            var queue = GetQueue(gguid);
            queue.RemoveFromQueue(gguid);
        }

        // Check player or group member restrictions
        if (!player.Session.HasPermission(RBACPermissions.JoinDungeonFinder))
        {
            joinData.Result = LfgJoinResult.NoSlots;
        }
        else if (player.InBattleground || player.InArena || player.InBattlegroundQueue())
        {
            joinData.Result = LfgJoinResult.CantUseDungeons;
        }
        else if (player.HasAura(SharedConst.LFGSpellDungeonDeserter))
        {
            joinData.Result = LfgJoinResult.DeserterPlayer;
        }
        else if (!isContinue && player.HasAura(SharedConst.LFGSpellDungeonCooldown))
        {
            joinData.Result = LfgJoinResult.RandomCooldownPlayer;
        }
        else if (dungeons.Empty())
        {
            joinData.Result = LfgJoinResult.NoSlots;
        }
        else if (player.HasAura(9454)) // check Freeze debuff
        {
            joinData.Result = LfgJoinResult.NoSlots;
        }
        else if (grp)
        {
            if (grp.MembersCount > MapConst.MaxGroupSize)
            {
                joinData.Result = LfgJoinResult.TooManyMembers;
            }
            else
            {
                byte memberCount = 0;

                for (var refe = grp.FirstMember; refe != null && joinData.Result == LfgJoinResult.Ok; refe = refe.Next())
                {
                    var plrg = refe.Source;

                    if (plrg)
                    {
                        if (!plrg.Session.HasPermission(RBACPermissions.JoinDungeonFinder))
                            joinData.Result = LfgJoinResult.NoLfgObject;

                        if (plrg.HasAura(SharedConst.LFGSpellDungeonDeserter))
                        {
                            joinData.Result = LfgJoinResult.DeserterParty;
                        }
                        else if (!isContinue && plrg.HasAura(SharedConst.LFGSpellDungeonCooldown))
                        {
                            joinData.Result = LfgJoinResult.RandomCooldownParty;
                        }
                        else if (plrg.InBattleground || plrg.InArena || plrg.InBattlegroundQueue())
                        {
                            joinData.Result = LfgJoinResult.CantUseDungeons;
                        }
                        else if (plrg.HasAura(9454)) // check Freeze debuff
                        {
                            joinData.Result = LfgJoinResult.NoSlots;
                            joinData.PlayersMissingRequirement.Add(plrg.GetName());
                        }

                        ++memberCount;
                        players.Add(plrg.GUID);
                    }
                }

                if (joinData.Result == LfgJoinResult.Ok && memberCount != grp.MembersCount)
                    joinData.Result = LfgJoinResult.MembersNotPresent;
            }
        }
        else
        {
            players.Add(player.GUID);
        }

        // Check if all dungeons are valid
        var isRaid = false;

        if (joinData.Result == LfgJoinResult.Ok)
        {
            var isDungeon = false;

            foreach (var it in dungeons)
            {
                if (joinData.Result != LfgJoinResult.Ok)
                    break;

                var type = GetDungeonType(it);

                switch (type)
                {
                    case LfgType.Random:
                        if (dungeons.Count > 1) // Only allow 1 random dungeon
                            joinData.Result = LfgJoinResult.InvalidSlot;
                        else
                            rDungeonId = dungeons.First();

                        goto case LfgType.Dungeon;
                    case LfgType.Dungeon:
                        if (isRaid)
                            joinData.Result = LfgJoinResult.MismatchedSlots;

                        isDungeon = true;

                        break;
                    case LfgType.Raid:
                        if (isDungeon)
                            joinData.Result = LfgJoinResult.MismatchedSlots;

                        isRaid = true;

                        break;
                    default:
                        Log.Logger.Error("Wrong dungeon type {0} for dungeon {1}", type, it);
                        joinData.Result = LfgJoinResult.InvalidSlot;

                        break;
                }
            }

            // it could be changed
            if (joinData.Result == LfgJoinResult.Ok)
            {
                // Expand random dungeons and check restrictions
                if (rDungeonId != 0)
                    dungeons = GetDungeonsByRandom(rDungeonId);

                // if we have lockmap then there are no compatible dungeons
                GetCompatibleDungeons(dungeons, players, joinData.Lockmap, joinData.PlayersMissingRequirement, isContinue);

                if (dungeons.Empty())
                    joinData.Result = LfgJoinResult.NoSlots;
            }
        }

        // Can't join. Send result
        if (joinData.Result != LfgJoinResult.Ok)
        {
            Log.Logger.Debug("Join: [{0}] joining with {1} members. result: {2}", guid, grp ? grp.MembersCount : 1, joinData.Result);

            if (!dungeons.Empty()) // Only should show lockmap when have no dungeons available
                joinData.Lockmap.Clear();

            player.Session.SendLfgJoinResult(joinData);

            return;
        }

        if (isRaid)
        {
            Log.Logger.Debug("Join: [{0}] trying to join raid browser and it's disabled.", guid);

            return;
        }

        RideTicket ticket = new()
        {
            RequesterGuid = guid,
            Id = GetQueueId(gguid),
            Type = RideType.Lfg,
            Time = GameTime.CurrentTime
        };

        var debugNames = "";

        if (grp) // Begin rolecheck
        {
            // Create new rolecheck
            LfgRoleCheck roleCheck = new()
            {
                CancelTime = GameTime.CurrentTime + SharedConst.LFGTimeRolecheck,
                State = LfgRoleCheckState.Initialiting,
                Leader = guid,
                Dungeons = dungeons,
                RDungeonId = rDungeonId
            };

            _roleChecksStore[gguid] = roleCheck;

            if (rDungeonId != 0)
            {
                dungeons.Clear();
                dungeons.Add(rDungeonId);
            }

            SetState(gguid, LfgState.Rolecheck);
            // Send update to player
            LfgUpdateData updateData = new(LfgUpdateType.JoinQueue, dungeons);

            for (var refe = grp.FirstMember; refe != null; refe = refe.Next())
            {
                var plrg = refe.Source;

                if (plrg)
                {
                    var pguid = plrg.GUID;
                    plrg.Session.SendLfgUpdateStatus(updateData, true);
                    SetState(pguid, LfgState.Rolecheck);
                    SetTicket(pguid, ticket);

                    if (!isContinue)
                        SetSelectedDungeons(pguid, dungeons);

                    roleCheck.Roles[pguid] = 0;

                    if (!string.IsNullOrEmpty(debugNames))
                        debugNames += ", ";

                    debugNames += plrg.GetName();
                }
            }

            // Update leader role
            UpdateRoleCheck(gguid, guid, roles);
        }
        else // Add player to queue
        {
            Dictionary<ObjectGuid, LfgRoles> rolesMap = new()
            {
                [guid] = roles
            };

            var queue = GetQueue(guid);
            queue.AddQueueData(guid, GameTime.CurrentTime, dungeons, rolesMap);

            if (!isContinue)
            {
                if (rDungeonId != 0)
                {
                    dungeons.Clear();
                    dungeons.Add(rDungeonId);
                }

                SetSelectedDungeons(guid, dungeons);
            }

            // Send update to player
            SetTicket(guid, ticket);
            SetRoles(guid, roles);
            player.Session.SendLfgUpdateStatus(new LfgUpdateData(LfgUpdateType.JoinQueueInitial, dungeons), false);
            SetState(gguid, LfgState.Queued);
            player.Session.SendLfgUpdateStatus(new LfgUpdateData(LfgUpdateType.AddedToQueue, dungeons), false);
            player.Session.SendLfgJoinResult(joinData);
            debugNames += player.GetName();
        }

        StringBuilder o = new();
        o.AppendFormat("Join: [{0}] joined ({1}{2}) Members: {3}. Dungeons ({4}): ", guid, (grp ? "group" : "player"), debugNames, dungeons.Count, ConcatenateDungeons(dungeons));
        Log.Logger.Debug(o.ToString());
    }

    public void LeaveLfg(ObjectGuid guid, bool disconnected = false)
    {
        Log.Logger.Debug("LeaveLfg: [{0}]", guid);

        var gguid = guid.IsParty ? guid : GetGroup(guid);
        var state = GetState(guid);

        switch (state)
        {
            case LfgState.Queued:
                if (!gguid.IsEmpty)
                {
                    var newState = LfgState.None;
                    var oldState = GetOldState(gguid);

                    // Set the new state to LFG_STATE_DUNGEON/LFG_STATE_FINISHED_DUNGEON if the group is already in a dungeon
                    // This is required in case a LFG group vote-kicks a player in a dungeon, queues, then leaves the queue (maybe to queue later again)
                    var group = _groupManager.GetGroupByGuid(gguid);

                    if (group != null)
                        if (group.IsLFGGroup && GetDungeon(gguid) != 0 && oldState is LfgState.Dungeon or LfgState.FinishedDungeon)
                            newState = oldState;

                    var queue = GetQueue(gguid);
                    queue.RemoveFromQueue(gguid);
                    SetState(gguid, newState);
                    var players = GetPlayers(gguid);

                    foreach (var it in players)
                    {
                        SetState(it, newState);
                        SendLfgUpdateStatus(it, new LfgUpdateData(LfgUpdateType.RemovedFromQueue), true);
                    }
                }
                else
                {
                    SendLfgUpdateStatus(guid, new LfgUpdateData(LfgUpdateType.RemovedFromQueue), false);
                    var queue = GetQueue(guid);
                    queue.RemoveFromQueue(guid);
                    SetState(guid, LfgState.None);
                }

                break;
            case LfgState.Rolecheck:
                if (!gguid.IsEmpty)
                    UpdateRoleCheck(gguid); // No player to update role = LFG_ROLECHECK_ABORTED

                break;
            case LfgState.Proposal:
            {
                // Remove from Proposals
                KeyValuePair<uint, LfgProposal> it = new();
                var pguid = gguid == guid ? GetLeader(gguid) : guid;

                foreach (var test in _proposalsStore)
                {
                    it = test;
                    var itPlayer = it.Value.Players.LookupByKey(pguid);

                    if (itPlayer != null)
                    {
                        // Mark the player/leader of group who left as didn't accept the proposal
                        itPlayer.Accept = LfgAnswer.Deny;

                        break;
                    }
                }

                // Remove from queue - if proposal is found, RemoveProposal will call RemoveFromQueue
                if (it.Value != null)
                    RemoveProposal(it, LfgUpdateType.ProposalDeclined);

                break;
            }
            case LfgState.None:
            case LfgState.Raidbrowser:
                break;
            case LfgState.Dungeon:
            case LfgState.FinishedDungeon:
                if (guid != gguid && !disconnected) // Player
                    SetState(guid, LfgState.None);

                break;
        }
    }

    public void LoadLFGDungeons(bool reload = false)
    {
        var oldMSTime = Time.MSTime;

        _lfgDungeonStore.Clear();

        // Initialize Dungeon map with data from dbcs
        foreach (var dungeon in _cliDB.LFGDungeonsStorage.Values)
        {
            if (_db2Manager.GetMapDifficultyData((uint)dungeon.MapID, dungeon.DifficultyID) == null)
                continue;

            _lfgDungeonStore[dungeon.Id] = dungeon.TypeID switch
            {
                LfgType.Dungeon => new LFGDungeonData(dungeon),
                LfgType.Raid    => new LFGDungeonData(dungeon),
                LfgType.Random  => new LFGDungeonData(dungeon),
                LfgType.Zone    => new LFGDungeonData(dungeon),
                _               => _lfgDungeonStore[dungeon.Id]
            };
        }

        // Fill teleport locations from DB
        var result = _worldDatabase.Query("SELECT dungeonId, position_x, position_y, position_z, orientation, requiredItemLevel FROM lfg_dungeon_template");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 lfg dungeon templates. DB table `lfg_dungeon_template` is empty!");

            return;
        }

        uint count = 0;

        do
        {
            var dungeonId = result.Read<uint>(0);

            if (!_lfgDungeonStore.ContainsKey(dungeonId))
            {
                Log.Logger.Error("table `lfg_entrances` contains coordinates for wrong dungeon {0}", dungeonId);

                continue;
            }

            var data = _lfgDungeonStore[dungeonId];
            data.X = result.Read<float>(1);
            data.Y = result.Read<float>(2);
            data.Z = result.Read<float>(3);
            data.O = result.Read<float>(4);
            data.RequiredItemLevel = result.Read<ushort>(5);

            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} lfg dungeon templates in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));

        // Fill all other teleport coords from areatriggers
        foreach (var pair in _lfgDungeonStore)
        {
            var dungeon = pair.Value;

            // No teleport coords in database, load from areatriggers
            if (dungeon.Type != LfgType.Random && dungeon.X == 0.0f && dungeon.Y == 0.0f && dungeon.Z == 0.0f)
            {
                var at = _objectManager.GetMapEntranceTrigger(dungeon.Map);

                if (at == null)
                {
                    Log.Logger.Error("LoadLFGDungeons: Failed to load dungeon {0} (Id: {1}), cant find areatrigger for map {2}", dungeon.Name, dungeon.ID, dungeon.Map);

                    continue;
                }

                dungeon.Map = at.TargetMapId;
                dungeon.X = at.TargetX;
                dungeon.Y = at.TargetY;
                dungeon.Z = at.TargetZ;
                dungeon.O = at.TargetOrientation;
            }

            if (dungeon.Type != LfgType.Random)
                _cachedDungeonMapStore.Add((byte)dungeon.Group, dungeon.ID);

            _cachedDungeonMapStore.Add(0, dungeon.ID);
        }

        if (reload)
            _cachedDungeonMapStore.Clear();
    }

    public void LoadRewards()
    {
        var oldMSTime = Time.MSTime;

        _rewardMapStore.Clear();

        // ORDER BY is very important for GetRandomDungeonReward!
        var result = _worldDatabase.Query("SELECT dungeonId, maxLevel, firstQuestId, otherQuestId FROM lfg_dungeon_rewards ORDER BY dungeonId, maxLevel ASC");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 lfg dungeon rewards. DB table `lfg_dungeon_rewards` is empty!");

            return;
        }

        uint count = 0;

        do
        {
            var dungeonId = result.Read<uint>(0);
            uint maxLevel = result.Read<byte>(1);
            var firstQuestId = result.Read<uint>(2);
            var otherQuestId = result.Read<uint>(3);

            if (GetLFGDungeonEntry(dungeonId) == 0)
            {
                Log.Logger.Error("Dungeon {0} specified in table `lfg_dungeon_rewards` does not exist!", dungeonId);

                continue;
            }

            if (maxLevel == 0 || maxLevel > _configuration.GetDefaultValue("MaxPlayerLevel", SharedConst.DefaultMaxLevel))
            {
                Log.Logger.Error("Level {0} specified for dungeon {1} in table `lfg_dungeon_rewards` can never be reached!", maxLevel, dungeonId);
                maxLevel = (uint)_configuration.GetDefaultValue("MaxPlayerLevel", SharedConst.DefaultMaxLevel);
            }

            if (firstQuestId == 0 || _objectManager.GetQuestTemplate(firstQuestId) == null)
            {
                Log.Logger.Error("First quest {0} specified for dungeon {1} in table `lfg_dungeon_rewards` does not exist!", firstQuestId, dungeonId);

                continue;
            }

            if (otherQuestId != 0 && _objectManager.GetQuestTemplate(otherQuestId) == null)
            {
                Log.Logger.Error("Other quest {0} specified for dungeon {1} in table `lfg_dungeon_rewards` does not exist!", otherQuestId, dungeonId);
                otherQuestId = 0;
            }

            _rewardMapStore.Add(dungeonId, new LfgReward(maxLevel, firstQuestId, otherQuestId));
            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} lfg dungeon rewards in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void RemoveGroupData(ObjectGuid guid)
    {
        Log.Logger.Debug("RemoveGroupData: [{0}]", guid);
        var it = _groupsStore.LookupByKey(guid);

        if (it == null)
            return;

        var state = GetState(guid);
        // If group is being formed after proposal success do nothing more
        var players = it.GetPlayers();

        foreach (var guid in players)
        {
            SetGroup(guid, ObjectGuid.Empty);

            if (state != LfgState.Proposal)
            {
                SetState(guid, LfgState.None);
                SendLfgUpdateStatus(guid, new LfgUpdateData(LfgUpdateType.RemovedFromQueue), true);
            }
        }

        _groupsStore.Remove(guid);
    }

    public byte RemovePlayerFromGroup(ObjectGuid gguid, ObjectGuid guid)
    {
        return _groupsStore[gguid].RemovePlayer(guid);
    }

    public bool SelectedRandomLfgDungeon(ObjectGuid guid)
    {
        if (GetState(guid) != LfgState.None)
        {
            var dungeons = GetSelectedDungeons(guid);

            if (!dungeons.Empty())
            {
                var dungeon = GetLFGDungeon(dungeons.First());

                if (dungeon != null && (dungeon.Type == LfgType.Random || dungeon.Seasonal))
                    return true;
            }
        }

        return false;
    }

    public void SendLfgBootProposalUpdate(ObjectGuid guid, LfgPlayerBoot boot)
    {
        var player = _objectAccessor.FindPlayer(guid);

        if (player)
            player.Session.SendLfgBootProposalUpdate(boot);
    }

    public void SendLfgJoinResult(ObjectGuid guid, LfgJoinResultData data)
    {
        var player = _objectAccessor.FindPlayer(guid);

        if (player)
            player.Session.SendLfgJoinResult(data);
    }

    public void SendLfgQueueStatus(ObjectGuid guid, LfgQueueStatusData data)
    {
        var player = _objectAccessor.FindPlayer(guid);

        if (player)
            player.Session.SendLfgQueueStatus(data);
    }

    public void SendLfgRoleCheckUpdate(ObjectGuid guid, LfgRoleCheck roleCheck)
    {
        var player = _objectAccessor.FindPlayer(guid);

        if (player)
            player.Session.SendLfgRoleCheckUpdate(roleCheck);
    }

    public void SendLfgRoleChosen(ObjectGuid guid, ObjectGuid pguid, LfgRoles roles)
    {
        var player = _objectAccessor.FindPlayer(guid);

        if (player)
            player.Session.SendLfgRoleChosen(pguid, roles);
    }

    public void SendLfgUpdateProposal(ObjectGuid guid, LfgProposal proposal)
    {
        var player = _objectAccessor.FindPlayer(guid);

        if (player)
            player.Session.SendLfgProposalUpdate(proposal);
    }

    public void SendLfgUpdateStatus(ObjectGuid guid, LfgUpdateData data, bool party)
    {
        var player = _objectAccessor.FindPlayer(guid);

        if (player)
            player.Session.SendLfgUpdateStatus(data, party);
    }

    public void SetGroup(ObjectGuid guid, ObjectGuid group)
    {
        AddPlayerData(guid);
        _playersStore[guid].SetGroup(group);
    }

    public void SetLeader(ObjectGuid gguid, ObjectGuid leader)
    {
        if (!_groupsStore.ContainsKey(gguid))
            _groupsStore[gguid] = new LFGGroupData();

        _groupsStore[gguid].SetLeader(leader);
    }

    public void SetOptions(LfgOptions options)
    {
        _options = options;
    }

    public void SetSelectedDungeons(ObjectGuid guid, List<uint> dungeons)
    {
        AddPlayerData(guid);
        Log.Logger.Debug("SetSelectedDungeons: [{0}] Dungeons: {1}", guid, ConcatenateDungeons(dungeons));
        _playersStore[guid].SetSelectedDungeons(dungeons);
    }

    public void SetState(ObjectGuid guid, LfgState state)
    {
        if (guid.IsParty)
        {
            if (!_groupsStore.ContainsKey(guid))
                _groupsStore[guid] = new LFGGroupData();

            var data = _groupsStore[guid];
            data.SetState(state);
        }
        else
        {
            var data = _playersStore[guid];
            data.SetState(state);
        }
    }

    public void SetTeam(ObjectGuid guid, TeamFaction team)
    {
        if (GetDefaultValue("AllowTwoSide.Interaction.Group", false))
            team = 0;

        _playersStore[guid].SetTeam(team);
    }

    public void SetupGroupMember(ObjectGuid guid, ObjectGuid gguid)
    {
        List<uint> dungeons = new();
        dungeons.Add(GetDungeon(gguid));
        SetSelectedDungeons(guid, dungeons);
        SetState(guid, GetState(gguid));
        SetGroup(guid, gguid);
        AddPlayerToGroup(gguid, guid);
    }

    public void TeleportPlayer(Player player, bool outt, bool fromOpcode = false)
    {
        LFGDungeonData dungeon = null;
        var group = player.Group;

        if (group && group.IsLFGGroup)
            dungeon = GetLFGDungeon(GetDungeon(group.GUID));

        if (dungeon == null)
        {
            Log.Logger.Debug("TeleportPlayer: Player {0} not in group/lfggroup or dungeon not found!", player.GetName());
            player.Session.SendLfgTeleportError(LfgTeleportResult.NoReturnLocation);

            return;
        }

        if (outt)
        {
            Log.Logger.Debug("TeleportPlayer: Player {0} is being teleported out. Current Map {1} - Expected Map {2}", player.GetName(), player.Location.MapId, dungeon.Map);

            if (player.Location.MapId == dungeon.Map)
                player.TeleportToBGEntryPoint();

            return;
        }

        var error = LfgTeleportResult.None;

        if (!player.IsAlive)
        {
            error = LfgTeleportResult.Dead;
        }
        else if (player.IsFalling || player.HasUnitState(UnitState.Jumping))
        {
            error = LfgTeleportResult.Falling;
        }
        else if (player.IsMirrorTimerActive(MirrorTimerType.Fatigue))
        {
            error = LfgTeleportResult.Exhaustion;
        }
        else if (player.Vehicle)
        {
            error = LfgTeleportResult.OnTransport;
        }
        else if (!player.CharmedGUID.IsEmpty)
        {
            error = LfgTeleportResult.ImmuneToSummons;
        }
        else if (player.HasAura(9454)) // check Freeze debuff
        {
            error = LfgTeleportResult.NoReturnLocation;
        }
        else if (player.Location.MapId != dungeon.Map) // Do not teleport players in dungeon to the entrance
        {
            var mapid = dungeon.Map;
            var x = dungeon.X;
            var y = dungeon.Y;
            var z = dungeon.Z;
            var orientation = dungeon.O;

            if (!fromOpcode)
                // Select a player inside to be teleported to
                for (var refe = group.FirstMember; refe != null; refe = refe.Next())
                {
                    var plrg = refe.Source;

                    if (plrg && plrg != player && plrg.Location.MapId == dungeon.Map)
                    {
                        mapid = plrg.Location.MapId;
                        x = plrg.Location.X;
                        y = plrg.Location.Y;
                        z = plrg.Location.Z;
                        orientation = plrg.Location.Orientation;

                        break;
                    }
                }

            if (!player.Location.Map.IsDungeon)
                player.SetBattlegroundEntryPoint();

            player.FinishTaxiFlight();

            if (!player.TeleportTo(mapid, x, y, z, orientation))
                error = LfgTeleportResult.NoReturnLocation;
        }
        else
        {
            error = LfgTeleportResult.NoReturnLocation;
        }

        if (error != LfgTeleportResult.None)
            player.Session.SendLfgTeleportError(error);

        Log.Logger.Debug("TeleportPlayer: Player {0} is being teleported in to map {1} (x: {2}, y: {3}, z: {4}) Result: {5}", player.GetName(), dungeon.Map, dungeon.X, dungeon.Y, dungeon.Z, error);
    }

    public void Update(uint diff)
    {
        if (!IsOptionEnabled(LfgOptions.EnableDungeonFinder | LfgOptions.EnableRaidBrowser))
            return;

        var currTime = GameTime.CurrentTime;

        // Remove obsolete role checks
        foreach (var pairCheck in _roleChecksStore)
        {
            var roleCheck = pairCheck.Value;

            if (currTime < roleCheck.CancelTime)
                continue;

            roleCheck.State = LfgRoleCheckState.MissingRole;

            foreach (var pairRole in roleCheck.Roles)
            {
                var guid = pairRole.Key;
                RestoreState(guid, "Remove Obsolete RoleCheck");
                SendLfgRoleCheckUpdate(guid, roleCheck);

                if (guid == roleCheck.Leader)
                    SendLfgJoinResult(guid, new LfgJoinResultData(LfgJoinResult.RoleCheckFailed, LfgRoleCheckState.MissingRole));
            }

            RestoreState(pairCheck.Key, "Remove Obsolete RoleCheck");
            _roleChecksStore.Remove(pairCheck.Key);
        }

        // Remove obsolete proposals
        foreach (var removePair in _proposalsStore.ToList())
            if (removePair.Value.CancelTime < currTime)
                RemoveProposal(removePair, LfgUpdateType.ProposalFailed);

        // Remove obsolete kicks
        foreach (var itBoot in _bootsStore)
        {
            var boot = itBoot.Value;

            if (boot.CancelTime < currTime)
            {
                boot.InProgress = false;

                foreach (var itVotes in boot.Votes)
                {
                    var pguid = itVotes.Key;

                    if (pguid != boot.Victim)
                        SendLfgBootProposalUpdate(pguid, boot);
                }

                SetVoteKick(itBoot.Key, false);
                _bootsStore.Remove(itBoot.Key);
            }
        }

        var lastProposalId = _lfgProposalId;

        // Check if a proposal can be formed with the new groups being added
        foreach (var it in _queuesStore)
        {
            var newProposals = it.Value.FindGroups();

            if (newProposals != 0)
                Log.Logger.Debug("Update: Found {0} new groups in queue {1}", newProposals, it.Key);
        }

        if (lastProposalId != _lfgProposalId)
            // FIXME lastProposalId ? lastProposalId +1 ?
            foreach (var itProposal in _proposalsStore.SkipWhile(p => p.Key == _lfgProposalId))
            {
                var proposalId = itProposal.Key;
                var proposal = _proposalsStore[proposalId];

                var guid = ObjectGuid.Empty;

                foreach (var itPlayers in proposal.Players)
                {
                    guid = itPlayers.Key;
                    SetState(guid, LfgState.Proposal);
                    var gguid = GetGroup(guid);

                    if (!gguid.IsEmpty)
                    {
                        SetState(gguid, LfgState.Proposal);
                        SendLfgUpdateStatus(guid, new LfgUpdateData(LfgUpdateType.ProposalBegin, GetSelectedDungeons(guid)), true);
                    }
                    else
                    {
                        SendLfgUpdateStatus(guid, new LfgUpdateData(LfgUpdateType.ProposalBegin, GetSelectedDungeons(guid)), false);
                    }

                    SendLfgUpdateProposal(guid, proposal);
                }

                if (proposal.State == LfgProposalState.Success)
                    UpdateProposal(proposalId, guid, true);
            }

        // Update all players status queue info
        if (_queueTimer > SharedConst.LFGQueueUpdateInterval)
        {
            _queueTimer = 0;

            foreach (var it in _queuesStore)
                it.Value.UpdateQueueTimers(it.Key, currTime);
        }
        else
        {
            _queueTimer += diff;
        }
    }

    public void UpdateBoot(ObjectGuid guid, bool accept)
    {
        var gguid = GetGroup(guid);

        if (gguid.IsEmpty)
            return;

        var boot = _bootsStore.LookupByKey(gguid);

        if (boot == null)
            return;

        if (boot.Votes[guid] != LfgAnswer.Pending) // Cheat check: Player can't vote twice
            return;

        boot.Votes[guid] = (LfgAnswer)Convert.ToInt32(accept);

        byte agreeNum = 0;
        byte denyNum = 0;

        foreach (var (_, answer) in boot.Votes)
            switch (answer)
            {
                case LfgAnswer.Pending:
                    break;
                case LfgAnswer.Agree:
                    ++agreeNum;

                    break;
                case LfgAnswer.Deny:
                    ++denyNum;

                    break;
            }

        // if we don't have enough votes (agree or deny) do nothing
        if (agreeNum < SharedConst.LFGKickVotesNeeded && (boot.Votes.Count - denyNum) >= SharedConst.LFGKickVotesNeeded)
            return;

        // Send update info to all players
        boot.InProgress = false;

        foreach (var itVotes in boot.Votes)
        {
            var pguid = itVotes.Key;

            if (pguid != boot.Victim)
                SendLfgBootProposalUpdate(pguid, boot);
        }

        SetVoteKick(gguid, false);

        if (agreeNum == SharedConst.LFGKickVotesNeeded) // Vote passed - Kick player
        {
            var group = _groupManager.GetGroupByGuid(gguid);

            if (group)
                PlayerComputators.RemoveFromGroup(group, boot.Victim, RemoveMethod.KickLFG);

            DecreaseKicksLeft(gguid);
        }

        _bootsStore.Remove(gguid);
    }

    public void UpdateProposal(uint proposalId, ObjectGuid guid, bool accept)
    {
        // Check if the proposal exists
        var proposal = _proposalsStore.LookupByKey(proposalId);

        // Check if proposal have the current player
        var player = proposal?.Players.LookupByKey(guid);

        if (player == null)
            return;

        player.Accept = (LfgAnswer)Convert.ToInt32(accept);

        Log.Logger.Debug("UpdateProposal: Player [{0}] of proposal {1} selected: {2}", guid, proposalId, accept);

        if (!accept)
        {
            RemoveProposal(new KeyValuePair<uint, LfgProposal>(proposalId, proposal), LfgUpdateType.ProposalDeclined);

            return;
        }

        // check if all have answered and reorder players (leader first)
        var allAnswered = true;

        foreach (var itPlayers in proposal.Players)
            if (itPlayers.Value.Accept != LfgAnswer.Agree) // No answer (-1) or not accepted (0)
                allAnswered = false;

        if (!allAnswered)
        {
            foreach (var it in proposal.Players)
                SendLfgUpdateProposal(it.Key, proposal);

            return;
        }

        var sendUpdate = proposal.State != LfgProposalState.Success;
        proposal.State = LfgProposalState.Success;
        var joinTime = GameTime.CurrentTime;

        var queue = GetQueue(guid);
        LfgUpdateData updateData = new(LfgUpdateType.GroupFound);

        foreach (var it in proposal.Players)
        {
            var pguid = it.Key;
            var gguid = it.Value.Group;
            var dungeonId = GetSelectedDungeons(pguid).First();
            int waitTime;

            if (sendUpdate)
                SendLfgUpdateProposal(pguid, proposal);

            if (!gguid.IsEmpty)
            {
                waitTime = (int)((joinTime - queue.GetJoinTime(gguid)) / Time.IN_MILLISECONDS);
                SendLfgUpdateStatus(pguid, updateData, false);
            }
            else
            {
                waitTime = (int)((joinTime - queue.GetJoinTime(pguid)) / Time.IN_MILLISECONDS);
                SendLfgUpdateStatus(pguid, updateData, false);
            }

            updateData.UpdateType = LfgUpdateType.RemovedFromQueue;
            SendLfgUpdateStatus(pguid, updateData, true);
            SendLfgUpdateStatus(pguid, updateData, false);

            // Update timers
            var role = GetRoles(pguid);
            role &= ~LfgRoles.Leader;

            switch (role)
            {
                case LfgRoles.Damage:
                    queue.UpdateWaitTimeDps(waitTime, dungeonId);

                    break;
                case LfgRoles.Healer:
                    queue.UpdateWaitTimeHealer(waitTime, dungeonId);

                    break;
                case LfgRoles.Tank:
                    queue.UpdateWaitTimeTank(waitTime, dungeonId);

                    break;
                default:
                    queue.UpdateWaitTimeAvg(waitTime, dungeonId);

                    break;
            }

            // Store the number of players that were present in group when joining RFD, used for achievement purposes
            var player = _objectAccessor.FindConnectedPlayer(pguid);

            var group = player?.Group;

            if (group != null)
                _playersStore[pguid].SetNumberOfPartyMembersAtJoin((byte)group.MembersCount);

            SetState(pguid, LfgState.Dungeon);
        }

        // Remove players/groups from Queue
        foreach (var it in proposal.Queues)
            queue.RemoveFromQueue(it);

        MakeNewGroup(proposal);
        _proposalsStore.Remove(proposalId);
    }

    public void UpdateRoleCheck(ObjectGuid gguid, ObjectGuid guid = default, LfgRoles roles = LfgRoles.None)
    {
        if (gguid.IsEmpty)
            return;

        Dictionary<ObjectGuid, LfgRoles> checkRoles;
        var roleCheck = _roleChecksStore.LookupByKey(gguid);

        if (roleCheck == null)
            return;

        // Sanitize input roles
        roles &= LfgRoles.Any;

        if (!guid.IsEmpty)
        {
            var player = _objectAccessor.FindPlayer(guid);

            if (player != null)
                roles = FilterClassRoles(player, roles);
            else
                return;
        }

        var sendRoleChosen = roleCheck.State != LfgRoleCheckState.Default && !guid.IsEmpty;

        if (guid.IsEmpty)
        {
            roleCheck.State = LfgRoleCheckState.Aborted;
        }
        else if (roles < LfgRoles.Tank) // Player selected no role.
        {
            roleCheck.State = LfgRoleCheckState.NoRole;
        }
        else
        {
            roleCheck.Roles[guid] = roles;

            // Check if all players have selected a role
            var done = false;

            foreach (var rolePair in roleCheck.Roles)
            {
                if (rolePair.Value != LfgRoles.None)
                    continue;

                done = true;
            }

            if (done)
            {
                // use temporal var to check roles, CheckGroupRoles modifies the roles
                checkRoles = roleCheck.Roles;
                roleCheck.State = CheckGroupRoles(checkRoles) ? LfgRoleCheckState.Finished : LfgRoleCheckState.WrongRoles;
            }
        }

        List<uint> dungeons = new();

        if (roleCheck.RDungeonId != 0)
            dungeons.Add(roleCheck.RDungeonId);
        else
            dungeons = roleCheck.Dungeons;

        LfgJoinResultData joinData = new(LfgJoinResult.RoleCheckFailed, roleCheck.State);

        foreach (var it in roleCheck.Roles)
        {
            var pguid = it.Key;

            if (sendRoleChosen)
                SendLfgRoleChosen(pguid, guid, roles);

            SendLfgRoleCheckUpdate(pguid, roleCheck);

            switch (roleCheck.State)
            {
                case LfgRoleCheckState.Initialiting:
                    continue;
                case LfgRoleCheckState.Finished:
                    SetState(pguid, LfgState.Queued);
                    SetRoles(pguid, it.Value);
                    SendLfgUpdateStatus(pguid, new LfgUpdateData(LfgUpdateType.AddedToQueue, dungeons), true);

                    break;
                default:
                    if (roleCheck.Leader == pguid)
                        SendLfgJoinResult(pguid, joinData);

                    SendLfgUpdateStatus(pguid, new LfgUpdateData(LfgUpdateType.RolecheckFailed), true);
                    RestoreState(pguid, "Rolecheck Failed");

                    break;
            }
        }

        if (roleCheck.State == LfgRoleCheckState.Finished)
        {
            SetState(gguid, LfgState.Queued);
            var queue = GetQueue(gguid);
            queue.AddQueueData(gguid, GameTime.CurrentTime, roleCheck.Dungeons, roleCheck.Roles);
            _roleChecksStore.Remove(gguid);
        }
        else if (roleCheck.State != LfgRoleCheckState.Initialiting)
        {
            RestoreState(gguid, "Rolecheck Failed");
            _roleChecksStore.Remove(gguid);
        }
    }

    private void _SaveToDB(ObjectGuid guid, uint dbGuid)
    {
        if (!guid.IsParty)
            return;

        SQLTransaction trans = new();

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_LFG_DATA);
        stmt.AddValue(0, dbGuid);
        trans.Append(stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_LFG_DATA);
        stmt.AddValue(0, dbGuid);
        stmt.AddValue(1, GetDungeon(guid));
        stmt.AddValue(2, (uint)GetState(guid));
        trans.Append(stmt);

        _characterDatabase.CommitTransaction(trans);
    }

    private void AddPlayerData(ObjectGuid guid)
    {
        if (_playersStore.ContainsKey(guid))
            return;

        _playersStore[guid] = new LFGPlayerData();
    }

    private void DecreaseKicksLeft(ObjectGuid guid)
    {
        Log.Logger.Debug("DecreaseKicksLeft: [{0}]", guid);
        _groupsStore[guid].DecreaseKicksLeft();
    }

    private LfgRoles FilterClassRoles(Player player, LfgRoles roles)
    {
        var allowedRoles = (uint)LfgRoles.Leader;

        for (uint i = 0; i < PlayerConst.MaxSpecializations; ++i)
        {
            var specialization = _db2Manager.GetChrSpecializationByIndex(player.Class, i);

            if (specialization != null)
                allowedRoles |= (1u << (specialization.Role + 1));
        }

        return roles & (LfgRoles)allowedRoles;
    }

    private void GetCompatibleDungeons(List<uint> dungeons, List<ObjectGuid> players, Dictionary<ObjectGuid, Dictionary<uint, LfgLockInfoData>> lockMap, List<string> playersMissingRequirement, bool isContinue)
    {
        lockMap.Clear();
        Dictionary<uint, uint> lockedDungeons = new();
        List<uint> dungeonsToRemove = new();

        foreach (var guid in players)
        {
            if (dungeons.Empty())
                break;

            var cachedLockMap = GetLockedDungeons(guid);
            var player = _objectAccessor.FindConnectedPlayer(guid);

            foreach (var it2 in cachedLockMap)
            {
                if (dungeons.Empty())
                    break;

                var dungeonId = (it2.Key & 0x00FFFFFF); // Compare dungeon ids

                if (dungeons.Contains(dungeonId))
                {
                    var eraseDungeon = true;

                    // Don't remove the dungeon if team members are trying to continue a locked instance
                    if (it2.Value.LockStatus == LfgLockStatusType.RaidLocked && isContinue)
                    {
                        var dungeon = GetLFGDungeon(dungeonId);
                        MapDb2Entries entries = new(dungeon.Map, dungeon.Difficulty);
                        var playerBind = _instanceLockManager.FindActiveInstanceLock(guid, entries);

                        if (playerBind != null)
                        {
                            var dungeonInstanceId = playerBind.GetInstanceId();

                            if (!lockedDungeons.TryGetValue(dungeonId, out var lockedDungeon) || lockedDungeon == dungeonInstanceId)
                                eraseDungeon = false;

                            lockedDungeons[dungeonId] = dungeonInstanceId;
                        }
                    }

                    if (eraseDungeon)
                        dungeonsToRemove.Add(dungeonId);

                    if (!lockMap.ContainsKey(guid))
                        lockMap[guid] = new Dictionary<uint, LfgLockInfoData>();

                    lockMap[guid][it2.Key] = it2.Value;
                    playersMissingRequirement.Add(player.GetName());
                }
            }
        }

        foreach (var dungeonIdToRemove in dungeonsToRemove)
            dungeons.Remove(dungeonIdToRemove);

        if (!dungeons.Empty())
            lockMap.Clear();
    }

    private List<uint> GetDungeonsByRandom(uint randomdungeon)
    {
        var dungeon = GetLFGDungeon(randomdungeon);
        var group = (byte)(dungeon?.Group ?? 0);

        return _cachedDungeonMapStore.LookupByKey(group);
    }

    private LFGDungeonData GetLFGDungeon(uint id)
    {
        return _lfgDungeonStore.LookupByKey(id);
    }

    private List<ObjectGuid> GetPlayers(ObjectGuid guid)
    {
        return _groupsStore[guid].GetPlayers();
    }

    private TeamFaction GetTeam(ObjectGuid guid)
    {
        return _playersStore[guid].GetTeam();
    }

    private bool IsSeasonActive(uint dungeonId)
    {
        return dungeonId switch
        {
            285 => // The Headless Horseman
                _gameEventManager.IsHolidayActive(HolidayIds.HallowsEnd),
            286 => // The Frost Lord Ahune
                _gameEventManager.IsHolidayActive(HolidayIds.MidsummerFireFestival),
            287 => // Coren Direbrew
                _gameEventManager.IsHolidayActive(HolidayIds.Brewfest),
            288 => // The Crown Chemical Co.
                _gameEventManager.IsHolidayActive(HolidayIds.LoveIsInTheAir),
            744 => // Random Timewalking Dungeon (Burning Crusade)
                _gameEventManager.IsHolidayActive(HolidayIds.TimewalkingDungeonEventBcDefault),
            995 => // Random Timewalking Dungeon (Wrath of the Lich King)
                _gameEventManager.IsHolidayActive(HolidayIds.TimewalkingDungeonEventLkDefault),
            1146 => // Random Timewalking Dungeon (Cataclysm)
                _gameEventManager.IsHolidayActive(HolidayIds.TimewalkingDungeonEventCataDefault),
            1453 => // Timewalker MoP
                _gameEventManager.IsHolidayActive(HolidayIds.TimewalkingDungeonEventMopDefault),
            _ => false
        };
    }

    private void MakeNewGroup(LfgProposal proposal)
    {
        List<ObjectGuid> players = new();
        List<ObjectGuid> tankPlayers = new();
        List<ObjectGuid> healPlayers = new();
        List<ObjectGuid> dpsPlayers = new();
        List<ObjectGuid> playersToTeleport = new();

        foreach (var it in proposal.Players)
        {
            var guid = it.Key;

            if (guid == proposal.Leader)
                players.Add(guid);
            else
                switch (it.Value.Role & ~LfgRoles.Leader)
                {
                    case LfgRoles.Tank:
                        tankPlayers.Add(guid);

                        break;
                    case LfgRoles.Healer:
                        healPlayers.Add(guid);

                        break;
                    case LfgRoles.Damage:
                        dpsPlayers.Add(guid);

                        break;
                }

            if (proposal.IsNew || GetGroup(guid) != proposal.Group)
                playersToTeleport.Add(guid);
        }

        players.AddRange(tankPlayers);
        players.AddRange(healPlayers);
        players.AddRange(dpsPlayers);

        // Set the dungeon difficulty
        var dungeon = GetLFGDungeon(proposal.DungeonId);

        var grp = !proposal.Group.IsEmpty ? _groupManager.GetGroupByGUID(proposal.Group) : null;

        foreach (var pguid in players)
        {
            var player = _objectAccessor.FindConnectedPlayer(pguid);

            if (!player)
                continue;

            var group = player.Group;

            if (group && group != grp)
                group.RemoveMember(player.GUID);

            if (!grp)
            {
                grp = new PlayerGroup();
                grp.ConvertToLFG();
                grp.Create(player);
                var gguid = grp.GUID;
                SetState(gguid, LfgState.Proposal);
                _groupManager.AddGroup(grp);
            }
            else if (group != grp)
            {
                grp.AddMember(player);
            }

            grp.SetLfgRoles(pguid, proposal.Players.LookupByKey(pguid).Role);

            // Add the cooldown spell if queued for a random dungeon
            var dungeons = GetSelectedDungeons(player.GUID);

            if (!dungeons.Empty())
            {
                var rDungeonId = dungeons[0];
                var rDungeon = GetLFGDungeon(rDungeonId);

                if (rDungeon is { Type: LfgType.Random })
                    player.CastSpell(player, SharedConst.LFGSpellDungeonCooldown, false);
            }
        }

        grp.SetDungeonDifficultyID(dungeon.Difficulty);
        var guid = grp.GUID;
        SetDungeon(guid, dungeon.Entry());
        SetState(guid, LfgState.Dungeon);

        _SaveToDB(guid, grp.DbStoreId);

        // Teleport Player
        foreach (var it in playersToTeleport)
        {
            var player = _objectAccessor.FindPlayer(it);

            if (player)
                TeleportPlayer(player, false);
        }

        // Update group info
        grp.SendUpdate();
    }

    private void RemoveProposal(KeyValuePair<uint, LfgProposal> itProposal, LfgUpdateType type)
    {
        var proposal = itProposal.Value;
        proposal.State = LfgProposalState.Failed;

        Log.Logger.Debug("RemoveProposal: Proposal {0}, state FAILED, UpdateType {1}", itProposal.Key, type);

        // Mark all people that didn't answered as no accept
        if (type == LfgUpdateType.ProposalFailed)
            foreach (var it in proposal.Players)
                if (it.Value.Accept == LfgAnswer.Pending)
                    it.Value.Accept = LfgAnswer.Deny;

        // Mark players/groups to be removed
        List<ObjectGuid> toRemove = new();

        foreach (var it in proposal.Players)
        {
            if (it.Value.Accept == LfgAnswer.Agree)
                continue;

            var guid = !it.Value.Group.IsEmpty ? it.Value.Group : it.Key;

            // Player didn't accept or still pending when no secs left
            if (it.Value.Accept == LfgAnswer.Deny || type == LfgUpdateType.ProposalFailed)
            {
                it.Value.Accept = LfgAnswer.Deny;
                toRemove.Add(guid);
            }
        }

        // Notify players
        foreach (var it in proposal.Players)
        {
            var guid = it.Key;
            var gguid = !it.Value.Group.IsEmpty ? it.Value.Group : guid;

            SendLfgUpdateProposal(guid, proposal);

            if (toRemove.Contains(gguid)) // Didn't accept or in same group that someone that didn't accept
            {
                LfgUpdateData updateData = new();

                if (it.Value.Accept == LfgAnswer.Deny)
                {
                    updateData.UpdateType = type;
                    Log.Logger.Debug("RemoveProposal: [{0}] didn't accept. Removing from queue and compatible cache", guid);
                }
                else
                {
                    updateData.UpdateType = LfgUpdateType.RemovedFromQueue;
                    Log.Logger.Debug("RemoveProposal: [{0}] in same group that someone that didn't accept. Removing from queue and compatible cache", guid);
                }

                RestoreState(guid, "Proposal Fail (didn't accepted or in group with someone that didn't accept");

                if (gguid != guid)
                {
                    RestoreState(it.Value.Group, "Proposal Fail (someone in group didn't accepted)");
                    SendLfgUpdateStatus(guid, updateData, true);
                }
                else
                {
                    SendLfgUpdateStatus(guid, updateData, false);
                }
            }
            else
            {
                Log.Logger.Debug("RemoveProposal: Readding [{0}] to queue.", guid);
                SetState(guid, LfgState.Queued);

                if (gguid != guid)
                {
                    SetState(gguid, LfgState.Queued);
                    SendLfgUpdateStatus(guid, new LfgUpdateData(LfgUpdateType.AddedToQueue, GetSelectedDungeons(guid)), true);
                }
                else
                {
                    SendLfgUpdateStatus(guid, new LfgUpdateData(LfgUpdateType.AddedToQueue, GetSelectedDungeons(guid)), false);
                }
            }
        }

        var queue = GetQueue(proposal.Players.First().Key);

        // Remove players/groups from queue
        foreach (var guid in toRemove)
        {
            queue.RemoveFromQueue(guid);
            proposal.Queues.Remove(guid);
        }

        // Readd to queue
        foreach (var guid in proposal.Queues)
            queue.AddToQueue(guid, true);

        _proposalsStore.Remove(itProposal.Key);
    }

    private void RestoreState(ObjectGuid guid, string debugMsg)
    {
        if (guid.IsParty)
        {
            var data = _groupsStore[guid];
            data.RestoreState();
        }
        else
        {
            var data = _playersStore[guid];
            data.RestoreState();
        }
    }

    private void SetDungeon(ObjectGuid guid, uint dungeon)
    {
        AddPlayerData(guid);
        Log.Logger.Debug("SetDungeon: [{0}] dungeon {1}", guid, dungeon);
        _groupsStore[guid].SetDungeon(dungeon);
    }

    private void SetRoles(ObjectGuid guid, LfgRoles roles)
    {
        AddPlayerData(guid);
        Log.Logger.Debug("SetRoles: [{0}] roles: {1}", guid, roles);
        _playersStore[guid].SetRoles(roles);
    }

    private void SetTicket(ObjectGuid guid, RideTicket ticket)
    {
        _playersStore[guid].SetTicket(ticket);
    }

    private void SetVoteKick(ObjectGuid gguid, bool active)
    {
        var data = _groupsStore[gguid];
        Log.Logger.Information("Group: {0}, New state: {1}, Previous: {2}", gguid.ToString(), active, data.IsVoteKickActive());

        data.SetVoteKick(active);
    }
}