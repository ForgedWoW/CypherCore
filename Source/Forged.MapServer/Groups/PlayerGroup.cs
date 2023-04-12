// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.BattleFields;
using Forged.MapServer.BattleGrounds;
using Forged.MapServer.Cache;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.Structs.M;
using Forged.MapServer.DungeonFinding;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Objects.Update;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Party;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IGroup;
using Forged.MapServer.Server;
using Framework.Constants;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.Groups;

public class PlayerGroup
{
    private readonly BattlegroundManager _battlegroundManager;
    private readonly CharacterCache _characterCache;
    private readonly CharacterDatabase _characterDatabase;
    private readonly CliDB _cliDB;
    private readonly DB2Manager _db2Manager;
    private readonly GroupManager _groupManager;
    private readonly GroupInstanceRefManager _instanceRefManager = new();
    private readonly List<Player> _invitees = new();
    private readonly TimeTracker _leaderOfflineTimer = new();

    private readonly LFGManager _lfgManager;

    // Raid markers
    private readonly RaidMarker[] _markers = new RaidMarker[MapConst.RaidMarkersCount];

    private readonly GroupRefManager _memberMgr = new();
    private readonly ObjectAccessor _objectAccessor;
    private readonly GameObjectManager _objectManager;
    private readonly PlayerComputators _playerComputators;
    private readonly Dictionary<uint, Tuple<ObjectGuid, uint>> _recentInstances = new();
    private readonly ScriptManager _scriptManager;
    private readonly ObjectGuid[] _targetIcons = new ObjectGuid[MapConst.TargetIconsCount];
    private uint _activeMarkers;
    private BattleField _bfGroup;
    private Battleground _bgGroup;
    private ObjectGuid _guid;
    private bool _isLeaderOffline;
    private byte _leaderFactionGroup;
    private ObjectGuid _leaderGuid;
    private ObjectGuid _looterGuid;

    private ObjectGuid _masterLooterGuid;

    // Ready Check
    private TimeSpan _readyCheckTimer;

    private byte[] _subGroupsCounts;

    public PlayerGroup(ScriptManager scriptManager, PlayerComputators playerComputators, CharacterDatabase characterDatabase, ObjectAccessor objectAccessor, CliDB cliDB,
                       DB2Manager db2Manager, BattlegroundManager battlegroundManager, GroupManager groupManager, CharacterCache characterCache, LFGManager lfgManager, GameObjectManager objectManager)
    {
        _scriptManager = scriptManager;
        _playerComputators = playerComputators;
        _characterDatabase = characterDatabase;
        _objectAccessor = objectAccessor;
        _cliDB = cliDB;
        _db2Manager = db2Manager;
        _battlegroundManager = battlegroundManager;
        _groupManager = groupManager;
        _characterCache = characterCache;
        _lfgManager = lfgManager;
        _objectManager = objectManager;

        LeaderName = "";
        GroupFlags = GroupFlags.None;
        DungeonDifficultyID = Difficulty.Normal;
        RaidDifficultyID = Difficulty.NormalRaid;
        LegacyRaidDifficultyID = Difficulty.Raid10N;
        LootMethod = LootMethod.PersonalLoot;
        LootThreshold = ItemQuality.Uncommon;
    }

    public uint DbStoreId { get; private set; }
    public Difficulty DungeonDifficultyID { get; private set; }

    public GroupReference FirstMember => _memberMgr.GetFirst();
    public GroupCategory GroupCategory { get; private set; }
    public GroupFlags GroupFlags { get; private set; }
    public ObjectGuid GUID => _guid;
    public uint InviteeCount => (uint)_invitees.Count;
    public bool IsBfGroup => _bfGroup != null;
    public bool IsBGGroup => _bgGroup != null;
    public bool IsCreated => MembersCount > 0;
    public bool IsFull => IsRaidGroup ? (MemberSlots.Count >= MapConst.MaxRaidSize) : (MemberSlots.Count >= MapConst.MaxGroupSize);
    public bool IsLFGGroup => GroupFlags.HasAnyFlag(GroupFlags.Lfg);
    public bool IsRaidGroup => GroupFlags.HasAnyFlag(GroupFlags.Raid);
    public bool IsReadyCheckStarted { get; private set; }
    public ObjectGuid LeaderGUID => _leaderGuid;
    public string LeaderName { get; private set; }
    public Difficulty LegacyRaidDifficultyID { get; private set; }
    public ObjectGuid LooterGuid => LootMethod == LootMethod.FreeForAll ? ObjectGuid.Empty : _looterGuid;

    public LootMethod LootMethod { get; private set; }
    public ItemQuality LootThreshold { get; private set; }
    public ulong LowGUID => _guid.Counter;
    public ObjectGuid MasterLooterGuid => _masterLooterGuid;
    public uint MembersCount => (uint)MemberSlots.Count;
    public List<MemberSlot> MemberSlots { get; } = new();
    public Difficulty RaidDifficultyID { get; private set; }

    private bool IsReadyCheckCompleted
    {
        get
        {
            return MemberSlots.All(member => member.ReadyChecked);
        }
    }
    public bool AddInvite(Player player)
    {
        if (player is not { GroupInvite: null })
            return false;

        var group = player.Group;

        if (group != null && (group.IsBGGroup || group.IsBfGroup))
            group = player.OriginalGroup;

        if (group != null)
            return false;

        RemoveInvite(player);

        _invitees.Add(player);

        player.GroupInvite = this;

        _scriptManager.ForEach<IGroupOnInviteMember>(p => p.OnInviteMember(this, player.GUID));

        return true;
    }

    public bool AddLeaderInvite(Player player)
    {
        if (!AddInvite(player))
            return false;

        _leaderGuid = player.GUID;
        _leaderFactionGroup = _playerComputators.GetFactionGroupForRace(player.Race);
        LeaderName = player.GetName();

        return true;
    }

    public bool AddMember(Player player)
    {
        // Get first not-full group
        byte subGroup = 0;

        if (_subGroupsCounts != null)
        {
            var groupFound = false;

            for (; subGroup < MapConst.MaxRaidSubGroups; ++subGroup)
                if (_subGroupsCounts[subGroup] < MapConst.MaxGroupSize)
                {
                    groupFound = true;

                    break;
                }

            // We are raid group and no one slot is free
            if (!groupFound)
                return false;
        }

        MemberSlot member = new()
        {
            Guid = player.GUID,
            Name = player.GetName(),
            Race = player.Race,
            Class = (byte)player.Class,
            Group = subGroup,
            Flags = 0,
            Roles = 0,
            ReadyChecked = false
        };

        MemberSlots.Add(member);

        SubGroupCounterIncrease(subGroup);

        player.GroupInvite = null;

        if (player.Group != null)
        {
            if (IsBGGroup || IsBfGroup) // if player is in group and he is being added to BG raid group, then call SetBattlegroundRaid()
                player.SetBattlegroundOrBattlefieldRaid(this, subGroup);
            else //if player is in bg raid and we are adding him to normal group, then call SetOriginalGroup()
                player.SetOriginalGroup(this, subGroup);
        }
        else //if player is not in group, then call set group
        {
            player.SetGroup(this, subGroup);
        }

        player.SetPartyType(GroupCategory, GroupType.Normal);
        player.ResetGroupUpdateSequenceIfNeeded(this);

        // if the same group invites the player back, cancel the homebind timer
        player.InstanceValid = player.CheckInstanceValidity(false);

        if (!IsRaidGroup) // reset targetIcons for non-raid-groups
            for (byte i = 0; i < MapConst.TargetIconsCount; ++i)
                _targetIcons[i].Clear();

        // insert into the table if we're not a Battlegroundgroup
        if (!IsBGGroup && !IsBfGroup)
        {
            var stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_GROUP_MEMBER);

            stmt.AddValue(0, DbStoreId);
            stmt.AddValue(1, member.Guid.Counter);
            stmt.AddValue(2, (byte)member.Flags);
            stmt.AddValue(3, member.Group);
            stmt.AddValue(4, (byte)member.Roles);

            _characterDatabase.Execute(stmt);
        }

        SendUpdate();
        _scriptManager.ForEach<IGroupOnAddMember>(p => p.OnAddMember(this, player.GUID));

        if (!IsLeader(player.GUID) && !IsBGGroup && !IsBfGroup)
        {
            if (player.DungeonDifficultyId != DungeonDifficultyID)
            {
                player.DungeonDifficultyId = DungeonDifficultyID;
                player.SendDungeonDifficulty();
            }

            if (player.RaidDifficultyId != RaidDifficultyID)
            {
                player.RaidDifficultyId = RaidDifficultyID;
                player.SendRaidDifficulty(false);
            }

            if (player.LegacyRaidDifficultyId != LegacyRaidDifficultyID)
            {
                player.LegacyRaidDifficultyId = LegacyRaidDifficultyID;
                player.SendRaidDifficulty(true);
            }
        }

        player.SetGroupUpdateFlag(GroupUpdateFlags.Full);
        var pet = player.CurrentPet;

        if (pet != null)
            pet.GroupUpdateFlag = GroupUpdatePetFlags.Full;

        UpdatePlayerOutOfRange(player);

        // quest related GO state dependent from raid membership
        if (IsRaidGroup)
            player.UpdateVisibleGameobjectsOrSpellClicks();

        {
            // Broadcast new player group member fields to rest of the group
            UpdateData groupData = new(player.Location.MapId);

            // Broadcast group members' fields to player
            for (var refe = FirstMember; refe != null; refe = refe.Next())
            {
                if (refe.Source == player)
                    continue;

                var existingMember = refe.Source;

                if (existingMember == null)
                    continue;

                if (player.HaveAtClient(existingMember))
                    existingMember.BuildValuesUpdateBlockForPlayerWithFlag(groupData, UpdateFieldFlag.PartyMember, player);

                if (!existingMember.HaveAtClient(player))
                    continue;

                UpdateData newData = new(player.Location.MapId);
                player.BuildValuesUpdateBlockForPlayerWithFlag(newData, UpdateFieldFlag.PartyMember, existingMember);

                if (!newData.HasData())
                    continue;

                newData.BuildPacket(out var newDataPacket);
                existingMember.SendPacket(newDataPacket);
            }

            if (!groupData.HasData())
                return true;

            groupData.BuildPacket(out var groupDataPacket);
            player.SendPacket(groupDataPacket);
        }

        return true;
    }

    public void AddRaidMarker(byte markerId, uint mapId, float positionX, float positionY, float positionZ, ObjectGuid transportGuid = default)
    {
        if (markerId >= MapConst.RaidMarkersCount || _markers[markerId] != null)
            return;

        _activeMarkers |= (1u << markerId);
        _markers[markerId] = new RaidMarker(mapId, positionX, positionY, positionZ, transportGuid);
        SendRaidMarkersChanged();
    }

    public void BroadcastAddonMessagePacket(ServerPacket packet, string prefix, bool ignorePlayersInBGRaid, int group = -1, ObjectGuid ignore = default)
    {
        for (var refe = FirstMember; refe != null; refe = refe.Next())
        {
            var player = refe.Source;

            if (player == null || (!ignore.IsEmpty && player.GUID == ignore) || (ignorePlayersInBGRaid && player.Group != this))
                continue;

            if ((group != -1 && refe.SubGroup != group))
                continue;

            if (player.Session.IsAddonRegistered(prefix))
                player.SendPacket(packet);
        }
    }

    public void BroadcastGroupUpdate()
    {
        // FG: HACK: force flags update on group leave - for values update hack
        // -- not very efficient but safe
        foreach (var pp in MemberSlots.Select(member => _objectAccessor.FindPlayer(member.Guid)).Where(pp => pp != null && pp.Location.IsInWorld))
        {
            pp.Values.ModifyValue(pp.UnitData).ModifyValue(pp.UnitData.PvpFlags);
            pp.Values.ModifyValue(pp.UnitData).ModifyValue(pp.UnitData.FactionTemplate);
            pp.ForceUpdateFieldChange();
            Log.Logger.Debug("-- Forced group value update for '{0}'", pp.GetName());
        }
    }

    public void BroadcastPacket(ServerPacket packet, bool ignorePlayersInBGRaid, int group = -1, ObjectGuid ignore = default)
    {
        for (var refe = FirstMember; refe != null; refe = refe.Next())
        {
            var player = refe.Source;

            if (player == null || (!ignore.IsEmpty && player.GUID == ignore) || (ignorePlayersInBGRaid && player.Group != this))
                continue;

            if (player.Session != null && (group == -1 || refe.SubGroup == group))
                player.SendPacket(packet);
        }
    }

    public void BroadcastWorker(Action<Player> worker)
    {
        for (var refe = FirstMember; refe != null; refe = refe.Next())
            worker(refe.Source);
    }

    public GroupJoinBattlegroundResult CanJoinBattlegroundQueue(Battleground bgOrTemplate, BattlegroundQueueTypeId bgQueueTypeId, uint minPlayerCount, uint maxPlayerCount, bool isRated, uint arenaSlot, out ObjectGuid errorGuid)
    {
        errorGuid = new ObjectGuid();

        // check if this group is LFG group
        if (IsLFGGroup)
            return GroupJoinBattlegroundResult.LfgCantUseBattleground;

        if (!_cliDB.BattlemasterListStorage.TryGetValue((uint)bgOrTemplate.GetTypeID(), out var bgEntry))
            return GroupJoinBattlegroundResult.BattlegroundJoinFailed; // shouldn't happen

        // check for min / max count
        var memberscount = MembersCount;

        if (memberscount > bgEntry.MaxGroupSize)     // no MinPlayerCount for Battlegrounds
            return GroupJoinBattlegroundResult.None; // ERR_GROUP_JOIN_Battleground_TOO_MANY handled on client side

        // get a player as reference, to compare other players' stats to (arena team id, queue id based on level, etc.)
        var reference = FirstMember.Source;

        // no reference found, can't join this way
        if (reference == null)
            return GroupJoinBattlegroundResult.BattlegroundJoinFailed;

        var bracketEntry = _db2Manager.GetBattlegroundBracketByLevel(bgOrTemplate.GetMapId(), reference.Level);

        if (bracketEntry == null)
            return GroupJoinBattlegroundResult.BattlegroundJoinFailed;

        var arenaTeamId = reference.GetArenaTeamId((byte)arenaSlot);
        var team = reference.Team;
        var isMercenary = reference.HasAura(BattlegroundConst.SpellMercenaryContractHorde) || reference.HasAura(BattlegroundConst.SpellMercenaryContractAlliance);

        // check every member of the group to be able to join
        memberscount = 0;

        for (var refe = FirstMember; refe != null; refe = refe.Next(), ++memberscount)
        {
            // offline member? don't let join
            if (refe.Source == null)
                return GroupJoinBattlegroundResult.BattlegroundJoinFailed;

            // rbac permissions
            if (!refe.Source.CanJoinToBattleground(bgOrTemplate))
                return GroupJoinBattlegroundResult.JoinTimedOut;

            // don't allow cross-faction join as group
            if (refe.Source.Team != team)
            {
                errorGuid = refe.Source.GUID;

                return GroupJoinBattlegroundResult.JoinTimedOut;
            }

            // not in the same Battleground level braket, don't let join
            var memberBracketEntry = _db2Manager.GetBattlegroundBracketByLevel(bracketEntry.MapID, refe.Source.Level);

            if (memberBracketEntry != bracketEntry)
                return GroupJoinBattlegroundResult.JoinRangeIndex;

            // don't let join rated matches if the arena team id doesn't match
            if (isRated && refe.Source.GetArenaTeamId((byte)arenaSlot) != arenaTeamId)
                return GroupJoinBattlegroundResult.BattlegroundJoinFailed;

            // don't let join if someone from the group is already in that bg queue
            if (refe.Source.InBattlegroundQueueForBattlegroundQueueType(bgQueueTypeId))
                return GroupJoinBattlegroundResult.BattlegroundJoinFailed; // not blizz-like

            // don't let join if someone from the group is in bg queue random
            var isInRandomBgQueue = refe.Source.InBattlegroundQueueForBattlegroundQueueType(_battlegroundManager.BGQueueTypeId((ushort)BattlegroundTypeId.RB, BattlegroundQueueIdType.Battleground, false, 0)) || refe.Source.InBattlegroundQueueForBattlegroundQueueType(_battlegroundManager.BGQueueTypeId((ushort)BattlegroundTypeId.RandomEpic, BattlegroundQueueIdType.Battleground, false, 0));

            if (bgOrTemplate.GetTypeID() != BattlegroundTypeId.AA && isInRandomBgQueue)
                return GroupJoinBattlegroundResult.InRandomBg;

            // don't let join to bg queue random if someone from the group is already in bg queue
            if ((bgOrTemplate.GetTypeID() == BattlegroundTypeId.RB || bgOrTemplate.GetTypeID() == BattlegroundTypeId.RandomEpic) && refe.Source.InBattlegroundQueue(true) && !isInRandomBgQueue)
                return GroupJoinBattlegroundResult.InNonRandomBg;

            // check for deserter debuff in case not arena queue
            if (bgOrTemplate.GetTypeID() != BattlegroundTypeId.AA && refe.Source.IsDeserter())
                return GroupJoinBattlegroundResult.Deserters;

            // check if member can join any more Battleground queues
            if (!refe.Source.HasFreeBattlegroundQueueId)
                return GroupJoinBattlegroundResult.TooManyQueues; // not blizz-like

            // check if someone in party is using dungeon system
            if (refe.Source.IsUsingLfg)
                return GroupJoinBattlegroundResult.LfgCantUseBattleground;

            // check Freeze debuff
            if (refe.Source.HasAura(9454))
                return GroupJoinBattlegroundResult.BattlegroundJoinFailed;

            if (isMercenary != (refe.Source.HasAura(BattlegroundConst.SpellMercenaryContractHorde) || refe.Source.HasAura(BattlegroundConst.SpellMercenaryContractAlliance)))
                return GroupJoinBattlegroundResult.BattlegroundJoinMercenary;
        }

        // only check for MinPlayerCount since MinPlayerCount == MaxPlayerCount for arenas...
        if (bgOrTemplate.IsArena() && memberscount != minPlayerCount)
            return GroupJoinBattlegroundResult.ArenaTeamPartySize;

        return GroupJoinBattlegroundResult.None;
    }

    public void ChangeLeader(ObjectGuid newLeaderGuid, sbyte partyIndex = 0)
    {
        var slot = _getMemberSlot(newLeaderGuid);

        if (slot == null)
            return;

        var newLeader = _objectAccessor.FindPlayer(slot.Guid);

        // Don't allow switching leader to offline players
        if (newLeader == null)
            return;

        _scriptManager.ForEach<IGroupOnChangeLeader>(p => p.OnChangeLeader(this, newLeaderGuid, _leaderGuid));

        if (!IsBGGroup && !IsBfGroup)
        {
            SQLTransaction trans = new();

            // Update the group leader
            var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_GROUP_LEADER);

            stmt.AddValue(0, newLeader.GUID.Counter);
            stmt.AddValue(1, DbStoreId);

            trans.Append(stmt);

            _characterDatabase.CommitTransaction(trans);
        }

        var oldLeader = _objectAccessor.FindConnectedPlayer(_leaderGuid);

        oldLeader?.RemovePlayerFlag(PlayerFlags.GroupLeader);

        newLeader.SetPlayerFlag(PlayerFlags.GroupLeader);
        _leaderGuid = newLeader.GUID;
        _leaderFactionGroup = _playerComputators.GetFactionGroupForRace(newLeader.Race);
        LeaderName = newLeader.GetName();
        ToggleGroupMemberFlag(slot, GroupMemberFlags.Assistant, false);

        GroupNewLeader groupNewLeader = new()
        {
            Name = LeaderName,
            PartyIndex = partyIndex
        };

        BroadcastPacket(groupNewLeader, true);
    }

    public void ChangeMembersGroup(ObjectGuid guid, byte group)
    {
        // Only raid groups have sub groups
        if (!IsRaidGroup)
            return;

        // Check if player is really in the raid
        var slot = _getMemberSlot(guid);

        if (slot == null)
            return;

        var prevSubGroup = slot.Group;

        // Abort if the player is already in the target sub group
        if (prevSubGroup == group)
            return;

        // Update the player slot with the new sub group setting
        slot.Group = group;

        // Increase the counter of the new sub group..
        SubGroupCounterIncrease(group);

        // ..and decrease the counter of the previous one
        SubGroupCounterDecrease(prevSubGroup);

        // Preserve new sub group in database for non-raid groups
        if (!IsBGGroup && !IsBfGroup)
        {
            var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_GROUP_MEMBER_SUBGROUP);

            stmt.AddValue(0, group);
            stmt.AddValue(1, guid.Counter);

            _characterDatabase.Execute(stmt);
        }

        // In case the moved player is online, update the player object with the new sub group references
        var player = _objectAccessor.FindPlayer(guid);

        if (player != null)
        {
            if (player.Group == this)
                player.GroupRef.SubGroup = group;
            else
                // If player is in BG raid, it is possible that he is also in normal raid - and that normal raid is stored in m_originalGroup reference
                player.               // If player is in BG raid, it is possible that he is also in normal raid - and that normal raid is stored in m_originalGroup reference
                    OriginalGroupRef. // If player is in BG raid, it is possible that he is also in normal raid - and that normal raid is stored in m_originalGroup reference
                    SubGroup = group;
        }

        // Broadcast the changes to the group
        SendUpdate();
    }

    public void ConvertToGroup()
    {
        if (MemberSlots.Count > 5)
            return; // What message error should we send?

        GroupFlags = GroupFlags.None;

        _subGroupsCounts = null;

        if (!IsBGGroup && !IsBfGroup)
        {
            var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_GROUP_TYPE);

            stmt.AddValue(0, (byte)GroupFlags);
            stmt.AddValue(1, DbStoreId);

            _characterDatabase.Execute(stmt);
        }

        SendUpdate();

        // update quest related GO states (quest activity dependent from raid membership)
        foreach (var player in MemberSlots.Select(member => _objectAccessor.FindPlayer(member.Guid)))
        {
            player?.UpdateVisibleGameobjectsOrSpellClicks();
        }
    }

    public void ConvertToLFG()
    {
        GroupFlags = (GroupFlags | GroupFlags.Lfg | GroupFlags.LfgRestricted);
        GroupCategory = GroupCategory.Instance;
        LootMethod = LootMethod.PersonalLoot;

        if (!IsBGGroup && !IsBfGroup)
        {
            var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_GROUP_TYPE);

            stmt.AddValue(0, (byte)GroupFlags);
            stmt.AddValue(1, DbStoreId);

            _characterDatabase.Execute(stmt);
        }

        SendUpdate();
    }

    public void ConvertToRaid()
    {
        GroupFlags |= GroupFlags.Raid;

        _initRaidSubGroupsCounter();

        if (!IsBGGroup && !IsBfGroup)
        {
            var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_GROUP_TYPE);

            stmt.AddValue(0, (byte)GroupFlags);
            stmt.AddValue(1, DbStoreId);

            _characterDatabase.Execute(stmt);
        }

        SendUpdate();

        // update quest related GO states (quest activity dependent from raid membership)
        foreach (var member in MemberSlots)
        {
            _objectAccessor.FindPlayer(member.Guid)?.UpdateVisibleGameobjectsOrSpellClicks();
        }
    }

    public bool Create(Player leader)
    {
        var leaderGuid = leader.GUID;

        _guid = ObjectGuid.Create(HighGuid.Party, _groupManager.GenerateGroupId());
        _leaderGuid = leaderGuid;
        _leaderFactionGroup = _playerComputators.GetFactionGroupForRace(leader.Race);
        LeaderName = leader.GetName();
        leader.SetPlayerFlag(PlayerFlags.GroupLeader);

        if (IsBGGroup || IsBfGroup)
        {
            GroupFlags = GroupFlags.MaskBgRaid;
            GroupCategory = GroupCategory.Instance;
        }

        if (GroupFlags.HasAnyFlag(GroupFlags.Raid))
            _initRaidSubGroupsCounter();

        LootThreshold = ItemQuality.Uncommon;
        _looterGuid = leaderGuid;

        DungeonDifficultyID = Difficulty.Normal;
        RaidDifficultyID = Difficulty.NormalRaid;
        LegacyRaidDifficultyID = Difficulty.Raid10N;

        if (!IsBGGroup && !IsBfGroup)
        {
            DungeonDifficultyID = leader.DungeonDifficultyId;
            RaidDifficultyID = leader.RaidDifficultyId;
            LegacyRaidDifficultyID = leader.LegacyRaidDifficultyId;

            DbStoreId = _groupManager.GenerateNewGroupDbStoreId();

            _groupManager.RegisterGroupDbStoreId(DbStoreId, this);

            // Store group in database
            var stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_GROUP);

            byte index = 0;

            stmt.AddValue(index++, DbStoreId);
            stmt.AddValue(index++, _leaderGuid.Counter);
            stmt.AddValue(index++, (byte)LootMethod);
            stmt.AddValue(index++, _looterGuid.Counter);
            stmt.AddValue(index++, (byte)LootThreshold);
            stmt.AddValue(index++, _targetIcons[0].GetRawValue());
            stmt.AddValue(index++, _targetIcons[1].GetRawValue());
            stmt.AddValue(index++, _targetIcons[2].GetRawValue());
            stmt.AddValue(index++, _targetIcons[3].GetRawValue());
            stmt.AddValue(index++, _targetIcons[4].GetRawValue());
            stmt.AddValue(index++, _targetIcons[5].GetRawValue());
            stmt.AddValue(index++, _targetIcons[6].GetRawValue());
            stmt.AddValue(index++, _targetIcons[7].GetRawValue());
            stmt.AddValue(index++, (byte)GroupFlags);
            stmt.AddValue(index++, (byte)DungeonDifficultyID);
            stmt.AddValue(index++, (byte)RaidDifficultyID);
            stmt.AddValue(index++, (byte)LegacyRaidDifficultyID);
            stmt.AddValue(index++, _masterLooterGuid.Counter);

            _characterDatabase.Execute(stmt);

            var leaderInstance = leader.Location.Map.ToInstanceMap;

            leaderInstance?.TrySetOwningGroup(this);

            AddMember(leader); // If the leader can't be added to a new group because it appears full, something is clearly wrong.
        }
        else if (!AddMember(leader))
        {
            return false;
        }

        return true;
    }

    public void DeleteRaidMarker(byte markerId)
    {
        if (markerId > MapConst.RaidMarkersCount)
            return;

        for (byte i = 0; i < MapConst.RaidMarkersCount; i++)
            if (_markers[i] != null && (markerId == i || markerId == MapConst.RaidMarkersCount))
            {
                _markers[i] = null;
                _activeMarkers &= ~(1u << i);
            }

        SendRaidMarkersChanged();
    }

    public void Disband(bool hideDestroy = false)
    {
        _scriptManager.ForEach<IGroupOnDisband>(p => p.OnDisband(this));

        foreach (var member in MemberSlots)
        {
            var player = _objectAccessor.FindPlayer(member.Guid);

            if (player == null)
                continue;

            //we cannot call _removeMember because it would invalidate member iterator
            //if we are removing player from Battlegroundraid
            if (IsBGGroup || IsBfGroup)
            {
                player.RemoveFromBattlegroundOrBattlefieldRaid();
            }
            else
            {
                //we can remove player who is in Battlegroundfrom his original group
                if (player.OriginalGroup == this)
                    player.SetOriginalGroup(null);
                else
                    player.SetGroup(null);
            }

            player.SetPartyType(GroupCategory, GroupType.None);

            // quest related GO state dependent from raid membership
            if (IsRaidGroup)
                player.UpdateVisibleGameobjectsOrSpellClicks();

            if (!hideDestroy)
                player.SendPacket(new GroupDestroyed());

            SendUpdateDestroyGroupToPlayer(player);

            _homebindIfInstance(player);
        }

        MemberSlots.Clear();

        RemoveAllInvites();

        if (!IsBGGroup && !IsBfGroup)
        {
            SQLTransaction trans = new();

            var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_GROUP);
            stmt.AddValue(0, DbStoreId);
            trans.Append(stmt);

            stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_GROUP_MEMBER_ALL);
            stmt.AddValue(0, DbStoreId);
            trans.Append(stmt);

            stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_LFG_DATA);
            stmt.AddValue(0, DbStoreId);
            trans.Append(stmt);

            _characterDatabase.CommitTransaction(trans);

            _groupManager.FreeGroupDbStoreId(this);
        }

        _groupManager.RemoveGroup(this);
    }

    public Difficulty GetDifficultyID(MapRecord mapEntry)
    {
        if (!mapEntry.IsRaid())
            return DungeonDifficultyID;

        var defaultDifficulty = _db2Manager.GetDefaultMapDifficulty(mapEntry.Id);

        if (defaultDifficulty == null)
            return LegacyRaidDifficultyID;

        var difficulty = _cliDB.DifficultyStorage.LookupByKey(defaultDifficulty.DifficultyID);

        if (difficulty == null || difficulty.Flags.HasAnyFlag(DifficultyFlags.Legacy))
            return LegacyRaidDifficultyID;

        return RaidDifficultyID;
    }

    public Player GetInvited(ObjectGuid guid)
    {
        return _invitees.FirstOrDefault(pl => pl != null && pl.GUID == guid);
    }

    public Player GetInvited(string name)
    {
        return _invitees.FirstOrDefault(pl => pl != null && pl.GetName() == name);
    }

    public LfgRoles GetLfgRoles(ObjectGuid guid)
    {
        return _getMemberSlot(guid)?.Roles ?? 0;
    }

    public GroupMemberFlags GetMemberFlags(ObjectGuid guid)
    {
        return _getMemberSlot(guid)?.Flags ?? 0;
    }

    public byte GetMemberGroup(ObjectGuid guid)
    {
        var mslot = _getMemberSlot(guid);

        if (mslot == null)
            return (byte)(MapConst.MaxRaidSubGroups + 1);

        return mslot.Group;
    }

    public ObjectGuid GetMemberGUID(string name)
    {
        foreach (var member in MemberSlots.Where(member => member.Name == name))
            return member.Guid;

        return ObjectGuid.Empty;
    }

    public uint GetRecentInstanceId(uint mapId)
    {
        return _recentInstances.TryGetValue(mapId, out var value) ? value.Item2 : 0;
    }

    public ObjectGuid GetRecentInstanceOwner(uint mapId)
    {
        return _recentInstances.TryGetValue(mapId, out var value) ? value.Item1 : _leaderGuid;
    }

    public bool HasFreeSlotSubGroup(byte subgroup)
    {
        return (_subGroupsCounts != null && _subGroupsCounts[subgroup] < MapConst.MaxGroupSize);
    }

    public bool IsAssistant(ObjectGuid guid)
    {
        return GetMemberFlags(guid).HasAnyFlag(GroupMemberFlags.Assistant);
    }

    public bool IsLeader(ObjectGuid guid)
    {
        return LeaderGUID == guid;
    }

    public bool IsMember(ObjectGuid guid)
    {
        return _getMemberSlot(guid) != null;
    }

    public void LinkMember(GroupReference pRef)
    {
        _memberMgr.InsertFirst(pRef);
    }

    public void LinkOwnedInstance(GroupInstanceReference refe)
    {
        _instanceRefManager.InsertLast(refe);
    }

    public void LoadGroupFromDB(SQLFields field)
    {
        DbStoreId = field.Read<uint>(17);
        _guid = ObjectGuid.Create(HighGuid.Party, _groupManager.GenerateGroupId());
        _leaderGuid = ObjectGuid.Create(HighGuid.Player, field.Read<ulong>(0));

        // group leader not exist
        var leader = _characterCache.GetCharacterCacheByGuid(_leaderGuid);

        if (leader == null)
            return;

        _leaderFactionGroup = _playerComputators.GetFactionGroupForRace(leader.RaceId);
        LeaderName = leader.Name;
        LootMethod = (LootMethod)field.Read<byte>(1);
        _looterGuid = ObjectGuid.Create(HighGuid.Player, field.Read<ulong>(2));
        LootThreshold = (ItemQuality)field.Read<byte>(3);

        for (byte i = 0; i < MapConst.TargetIconsCount; ++i)
            _targetIcons[i].SetRawValue(field.Read<byte[]>(4 + i));

        GroupFlags = (GroupFlags)field.Read<byte>(12);

        if (GroupFlags.HasAnyFlag(GroupFlags.Raid))
            _initRaidSubGroupsCounter();

        DungeonDifficultyID = _playerComputators.CheckLoadedDungeonDifficultyId((Difficulty)field.Read<byte>(13));
        RaidDifficultyID = _playerComputators.CheckLoadedRaidDifficultyId((Difficulty)field.Read<byte>(14));
        LegacyRaidDifficultyID = _playerComputators.CheckLoadedLegacyRaidDifficultyId((Difficulty)field.Read<byte>(15));

        _masterLooterGuid = ObjectGuid.Create(HighGuid.Player, field.Read<ulong>(16));

        if (GroupFlags.HasAnyFlag(GroupFlags.Lfg))
            _lfgManager._LoadFromDB(field, GUID);
    }

    public void LoadMemberFromDB(ulong guidLow, byte memberFlags, byte subgroup, LfgRoles roles)
    {
        MemberSlot member = new()
        {
            Guid = ObjectGuid.Create(HighGuid.Player, guidLow)
        };

        // skip non-existed member
        var character = _characterCache.GetCharacterCacheByGuid(member.Guid);

        if (character == null)
        {
            var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_GROUP_MEMBER);
            stmt.AddValue(0, guidLow);
            _characterDatabase.Execute(stmt);

            return;
        }

        member.Name = character.Name;
        member.Race = character.RaceId;
        member.Class = (byte)character.ClassId;
        member.Group = subgroup;
        member.Flags = (GroupMemberFlags)memberFlags;
        member.Roles = roles;
        member.ReadyChecked = false;

        MemberSlots.Add(member);

        SubGroupCounterIncrease(subgroup);

        _lfgManager.SetupGroupMember(member.Guid, GUID);
    }

    public void RemoveAllInvites()
    {
        foreach (var pl in _invitees.Where(pl => pl != null))
            pl.GroupInvite = null;

        _invitees.Clear();
    }

    public void RemoveInvite(Player player)
    {
        if (player == null)
            return;

        _invitees.Remove(player);
        player.GroupInvite = null;
    }

    public bool RemoveMember(ObjectGuid guid, RemoveMethod method = RemoveMethod.Default, ObjectGuid kicker = default, string reason = null)
    {
        BroadcastGroupUpdate();

        _scriptManager.ForEach<IGroupOnRemoveMember>(p => p.OnRemoveMember(this, guid, method, kicker, reason));

        var player = _objectAccessor.FindConnectedPlayer(guid);

        if (player != null)
            for (var refe = FirstMember; refe != null; refe = refe.Next())
            {
                var groupMember = refe.Source;

                if (groupMember == null)
                    continue;

                if (groupMember.GUID == guid)
                    continue;

                groupMember.RemoveAllGroupBuffsFromCaster(guid);
                player.RemoveAllGroupBuffsFromCaster(groupMember.GUID);
            }

        // LFG group vote kick handled in scripts
        if (IsLFGGroup && method == RemoveMethod.Kick)
            return MemberSlots.Count != 0;

        // remove member and change leader (if need) only if strong more 2 members _before_ member remove (BG/BF allow 1 member group)
        if (MembersCount > ((IsBGGroup || IsLFGGroup || IsBfGroup) ? 1 : 2))
        {
            if (player != null)
            {
                // Battlegroundgroup handling
                if (IsBGGroup || IsBfGroup)
                {
                    player.RemoveFromBattlegroundOrBattlefieldRaid();
                }
                else
                    // Regular group
                {
                    if (player.OriginalGroup == this)
                        player.SetOriginalGroup(null);
                    else
                        player.SetGroup(null);

                    // quest related GO state dependent from raid membership
                    player.UpdateVisibleGameobjectsOrSpellClicks();
                }

                player.SetPartyType(GroupCategory, GroupType.None);

                if (method is RemoveMethod.Kick or RemoveMethod.KickLFG)
                    player.SendPacket(new GroupUninvite());

                _homebindIfInstance(player);
            }

            // Remove player from group in DB
            if (!IsBGGroup && !IsBfGroup)
            {
                var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_GROUP_MEMBER);
                stmt.AddValue(0, guid.Counter);
                _characterDatabase.Execute(stmt);
                DelinkMember(guid);
            }

            // Update subgroups
            var slot = _getMemberSlot(guid);

            if (slot != null)
            {
                SubGroupCounterDecrease(slot.Group);
                MemberSlots.Remove(slot);
            }

            // Pick new leader if necessary
            if (_leaderGuid == guid)
                foreach (var member in MemberSlots.Where(member => _objectAccessor.FindPlayer(member.Guid) != null))
                {
                    ChangeLeader(member.Guid);

                    break;
                }

            SendUpdate();

            if (IsLFGGroup && MembersCount == 1)
            {
                var leader = _objectAccessor.FindPlayer(LeaderGUID);
                var mapId = _lfgManager.GetDungeonMapId(GUID);

                if (mapId == 0 || leader == null || (leader.IsAlive && leader.Location.MapId != mapId))
                {
                    Disband();

                    return false;
                }
            }

            if (_memberMgr.GetSize() < ((IsLFGGroup || IsBGGroup) ? 1 : 2))
                Disband();
            else if (player != null)
                // send update to removed player too so party frames are destroyed clientside
                SendUpdateDestroyGroupToPlayer(player);

            return true;
        }
        // If group size before player removal <= 2 then disband it
        else
        {
            Disband();

            return false;
        }
    }

    public void RemoveUniqueGroupMemberFlag(GroupMemberFlags flag)
    {
        foreach (var member in MemberSlots.Where(member => member.Flags.HasAnyFlag(flag)))
            member.Flags &= ~flag;
    }

    public void ResetInstances(InstanceResetMethod method, Player notifyPlayer)
    {
        for (var refe = _instanceRefManager.GetFirst(); refe != null; refe = refe.Next())
        {
            var map = refe.Source;

            switch (map.Reset(method))
            {
                case InstanceResetResult.Success:
                    notifyPlayer.SendResetInstanceSuccess(map.Id);
                    _recentInstances.Remove(map.Id);

                    break;
                case InstanceResetResult.NotEmpty:
                    if (method == InstanceResetMethod.Manual)
                        notifyPlayer.SendResetInstanceFailed(ResetFailedReason.Failed, map.Id);
                    else if (method == InstanceResetMethod.OnChangeDifficulty)
                        _recentInstances.Remove(map.Id); // map might not have been reset on difficulty change but we still don't want to zone in there again

                    break;
                case InstanceResetResult.CannotReset:
                    _recentInstances.Remove(map.Id); // forget the instance, allows retrying different lockout with a new leader

                    break;
            }
        }
    }

    public bool SameSubGroup(Player member1, Player member2)
    {
        if (member1 == null || member2 == null)
            return false;

        if (member1.Group != this || member2.Group != this)
            return false;

        return member1.SubGroup == member2.SubGroup;
    }

    public bool SameSubGroup(ObjectGuid guid1, ObjectGuid guid2)
    {
        var mslot2 = _getMemberSlot(guid2);

        return mslot2 != null && SameSubGroup(guid1, mslot2);
    }

    public bool SameSubGroup(ObjectGuid guid1, MemberSlot slot2)
    {
        var mslot1 = _getMemberSlot(guid1);

        if (mslot1 == null || slot2 == null)
            return false;

        return (mslot1.Group == slot2.Group);
    }

    public void SendRaidMarkersChanged(WorldSession session = null, sbyte partyIndex = 0)
    {
        RaidMarkersChanged packet = new()
        {
            PartyIndex = partyIndex,
            ActiveMarkers = _activeMarkers
        };

        for (byte i = 0; i < MapConst.RaidMarkersCount; i++)
            if (_markers[i] != null)
                packet.RaidMarkers.Add(_markers[i]);

        if (session != null)
            session.SendPacket(packet);
        else
            BroadcastPacket(packet, false);
    }

    public void SendTargetIconList(WorldSession session, sbyte partyIndex)
    {
        if (session == null)
            return;

        SendRaidTargetUpdateAll updateAll = new()
        {
            PartyIndex = partyIndex
        };

        for (byte i = 0; i < MapConst.TargetIconsCount; i++)
            updateAll.TargetIcons.Add(i, _targetIcons[i]);

        session.SendPacket(updateAll);
    }

    public void SendUpdate()
    {
        foreach (var member in MemberSlots)
            SendUpdateToPlayer(member.Guid, member);
    }

    public void SendUpdateToPlayer(ObjectGuid playerGUID, MemberSlot memberSlot = null)
    {
        var player = _objectAccessor.FindPlayer(playerGUID);

        if (player?.Session == null || player.Group != this)
            return;

        // if MemberSlot wasn't provided
        if (memberSlot == null)
        {
            var slot = _getMemberSlot(playerGUID);

            if (slot == null) // if there is no MemberSlot for such a player
                return;

            memberSlot = slot;
        }

        PartyUpdate partyUpdate = new()
        {
            PartyFlags = GroupFlags,
            PartyIndex = (byte)GroupCategory,
            PartyType = IsCreated ? GroupType.Normal : GroupType.None,
            PartyGUID = _guid,
            LeaderGUID = _leaderGuid,
            LeaderFactionGroup = _leaderFactionGroup,
            SequenceNum = player.NextGroupUpdateSequenceNumber(GroupCategory),
            MyIndex = -1
        };

        byte index = 0;

        for (var i = 0; i < MemberSlots.Count; ++i, ++index)
        {
            var member = MemberSlots[i];

            if (memberSlot.Guid == member.Guid)
                partyUpdate.MyIndex = index;

            var memberPlayer = _objectAccessor.FindConnectedPlayer(member.Guid);

            PartyPlayerInfo playerInfos = new()
            {
                GUID = member.Guid,
                Name = member.Name,
                Class = member.Class,
                FactionGroup = _playerComputators.GetFactionGroupForRace(member.Race),
                Connected = memberPlayer?.Session != null && !memberPlayer.Session.PlayerLogout,
                Subgroup = member.Group,           // groupid
                Flags = (byte)member.Flags,        // See enum GroupMemberFlags
                RolesAssigned = (byte)member.Roles // Lfg Roles
            };

            partyUpdate.PlayerList.Add(playerInfos);
        }

        if (MembersCount > 1)
        {
            // LootSettings
            PartyLootSettings lootSettings = new()
            {
                Method = (byte)LootMethod,
                Threshold = (byte)LootThreshold,
                LootMaster = LootMethod == LootMethod.MasterLoot ? _masterLooterGuid : ObjectGuid.Empty
            };

            partyUpdate.LootSettings = lootSettings;

            // Difficulty Settings
            PartyDifficultySettings difficultySettings = new()
            {
                DungeonDifficultyID = (uint)DungeonDifficultyID,
                RaidDifficultyID = (uint)RaidDifficultyID,
                LegacyRaidDifficultyID = (uint)LegacyRaidDifficultyID
            };

            partyUpdate.DifficultySettings = difficultySettings;
        }

        // LfgInfos
        if (IsLFGGroup)
        {
            PartyLFGInfo lfgInfos = new()
            {
                Slot = _lfgManager.GetLFGDungeonEntry(_lfgManager.GetDungeon(_guid)),
                BootCount = 0,
                Aborted = false,
                MyFlags = (byte)(_lfgManager.GetState(_guid) == LfgState.FinishedDungeon ? 2 : 0),
                MyRandomSlot = _lfgManager.GetSelectedRandomDungeon(player.GUID),
                MyPartialClear = 0,
                MyGearDiff = 0.0f,
                MyFirstReward = false
            };

            if (partyUpdate.LfgInfos != null)
            {
                var reward = _lfgManager.GetRandomDungeonReward(partyUpdate.LfgInfos.Value.MyRandomSlot, player.Level);

                if (reward != null)
                {
                    var quest = _objectManager.GetQuestTemplate(reward.FirstQuest);

                    if (quest != null)
                        lfgInfos.MyFirstReward = player.CanRewardQuest(quest, false);
                }
            }

            lfgInfos.MyStrangerCount = 0;
            lfgInfos.MyKickVoteCount = 0;

            partyUpdate.LfgInfos = lfgInfos;
        }

        player.SendPacket(partyUpdate);
    }

    public void SetBattlefieldGroup(BattleField bg)
    {
        _bfGroup = bg;
    }

    public void SetBattlegroundGroup(Battleground bg)
    {
        _bgGroup = bg;
    }

    public void SetDungeonDifficultyID(Difficulty difficulty)
    {
        DungeonDifficultyID = difficulty;

        if (!IsBGGroup && !IsBfGroup)
        {
            var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_GROUP_DIFFICULTY);

            stmt.AddValue(0, (byte)DungeonDifficultyID);
            stmt.AddValue(1, DbStoreId);

            _characterDatabase.Execute(stmt);
        }

        for (var refe = FirstMember; refe != null; refe = refe.Next())
        {
            var player = refe.Source;

            if (player.Session == null)
                continue;

            player.DungeonDifficultyId = difficulty;
            player.SendDungeonDifficulty();
        }
    }

    public void SetEveryoneIsAssistant(bool apply)
    {
        if (apply)
            GroupFlags |= GroupFlags.EveryoneAssistant;
        else
            GroupFlags &= ~GroupFlags.EveryoneAssistant;

        foreach (var member in MemberSlots)
            ToggleGroupMemberFlag(member, GroupMemberFlags.Assistant, apply);

        SendUpdate();
    }

    public void SetGroupMemberFlag(ObjectGuid guid, bool apply, GroupMemberFlags flag)
    {
        // Assistants, main assistants and main tanks are only available in raid groups
        if (!IsRaidGroup)
            return;

        // Check if player is really in the raid
        var slot = _getMemberSlot(guid);

        if (slot == null)
            return;

        // Do Id specific actions, e.g ensure uniqueness
        switch (flag)
        {
            case GroupMemberFlags.MainAssist:
                RemoveUniqueGroupMemberFlag(GroupMemberFlags.MainAssist); // Remove main assist Id from current if any.

                break;
            case GroupMemberFlags.MainTank:
                RemoveUniqueGroupMemberFlag(GroupMemberFlags.MainTank); // Remove main tank Id from current if any.

                break;
            case GroupMemberFlags.Assistant:
                break;
            default:
                return; // This should never happen
        }

        // Switch the actual Id
        ToggleGroupMemberFlag(slot, flag, apply);

        // Preserve the new setting in the db
        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_GROUP_MEMBER_FLAG);

        stmt.AddValue(0, (byte)slot.Flags);
        stmt.AddValue(1, guid.Counter);

        _characterDatabase.Execute(stmt);

        // Broadcast the changes to the group
        SendUpdate();
    }

    public void SetLegacyRaidDifficultyID(Difficulty difficulty)
    {
        LegacyRaidDifficultyID = difficulty;

        if (!IsBGGroup && !IsBfGroup)
        {
            var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_GROUP_LEGACY_RAID_DIFFICULTY);

            stmt.AddValue(0, (byte)LegacyRaidDifficultyID);
            stmt.AddValue(1, DbStoreId);

            _characterDatabase.Execute(stmt);
        }

        for (var refe = FirstMember; refe != null; refe = refe.Next())
        {
            var player = refe.Source;

            if (player.Session == null)
                continue;

            player.LegacyRaidDifficultyId = difficulty;
            player.SendRaidDifficulty(true);
        }
    }

    public void SetLfgRoles(ObjectGuid guid, LfgRoles roles)
    {
        var slot = _getMemberSlot(guid);

        if (slot == null)
            return;

        slot.Roles = roles;
        SendUpdate();
    }

    public void SetLooterGuid(ObjectGuid guid)
    {
        _looterGuid = guid;
    }

    public void SetLootMethod(LootMethod method)
    {
        LootMethod = method;
    }

    public void SetLootThreshold(ItemQuality threshold)
    {
        LootThreshold = threshold;
    }

    public void SetMasterLooterGuid(ObjectGuid guid)
    {
        _masterLooterGuid = guid;
    }

    public void SetMemberReadyCheck(ObjectGuid guid, bool ready)
    {
        if (!IsReadyCheckStarted)
            return;

        var slot = _getMemberSlot(guid);

        if (slot != null)
            SetMemberReadyCheck(slot, ready);
    }

    public void SetRaidDifficultyID(Difficulty difficulty)
    {
        RaidDifficultyID = difficulty;

        if (!IsBGGroup && !IsBfGroup)
        {
            var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_GROUP_RAID_DIFFICULTY);

            stmt.AddValue(0, (byte)RaidDifficultyID);
            stmt.AddValue(1, DbStoreId);

            _characterDatabase.Execute(stmt);
        }

        for (var refe = FirstMember; refe != null; refe = refe.Next())
        {
            var player = refe.Source;

            if (player.Session == null)
                continue;

            player.RaidDifficultyId = difficulty;
            player.SendRaidDifficulty(false);
        }
    }

    public void SetRecentInstance(uint mapId, ObjectGuid instanceOwner, uint instanceId)
    {
        _recentInstances[mapId] = Tuple.Create(instanceOwner, instanceId);
    }

    public void SetTargetIcon(byte symbol, ObjectGuid target, ObjectGuid changedBy, sbyte partyIndex)
    {
        if (symbol >= MapConst.TargetIconsCount)
            return;

        // clean other icons
        if (!target.IsEmpty)
            for (byte i = 0; i < MapConst.TargetIconsCount; ++i)
                if (_targetIcons[i] == target)
                    SetTargetIcon(i, ObjectGuid.Empty, changedBy, partyIndex);

        _targetIcons[symbol] = target;

        SendRaidTargetUpdateSingle updateSingle = new()
        {
            PartyIndex = partyIndex,
            Target = target,
            ChangedBy = changedBy,
            Symbol = (sbyte)symbol
        };

        BroadcastPacket(updateSingle, true);
    }

    public void StartLeaderOfflineTimer()
    {
        _isLeaderOffline = true;
        _leaderOfflineTimer.Reset(2 * Time.MINUTE * Time.IN_MILLISECONDS);
    }

    public void StartReadyCheck(ObjectGuid starterGuid, sbyte partyIndex, TimeSpan duration)
    {
        if (IsReadyCheckStarted)
            return;

        var slot = _getMemberSlot(starterGuid);

        if (slot == null)
            return;

        IsReadyCheckStarted = true;
        _readyCheckTimer = duration;

        SetOfflineMembersReadyChecked();

        SetMemberReadyChecked(slot);

        ReadyCheckStarted readyCheckStarted = new()
        {
            PartyGUID = _guid,
            PartyIndex = partyIndex,
            InitiatorGUID = starterGuid,
            Duration = (uint)duration.TotalMilliseconds
        };

        BroadcastPacket(readyCheckStarted, false);
    }

    public void StopLeaderOfflineTimer()
    {
        _isLeaderOffline = false;
    }

    public void SwapMembersGroups(ObjectGuid firstGuid, ObjectGuid secondGuid)
    {
        if (!IsRaidGroup)
            return;

        var slots = new MemberSlot[2];
        slots[0] = _getMemberSlot(firstGuid);
        slots[1] = _getMemberSlot(secondGuid);

        if (slots[0] == null || slots[1] == null)
            return;

        if (slots[0].Group == slots[1].Group)
            return;

        (slots[0].Group, slots[1].Group) = (slots[1].Group, slots[0].Group);

        SQLTransaction trans = new();

        for (byte i = 0; i < 2; i++)
        {
            // Preserve new sub group in database for non-raid groups
            if (!IsBGGroup && !IsBfGroup)
            {
                var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_GROUP_MEMBER_SUBGROUP);
                stmt.AddValue(0, slots[i].Group);
                stmt.AddValue(1, slots[i].Guid.Counter);

                trans.Append(stmt);
            }

            var player = _objectAccessor.FindConnectedPlayer(slots[i].Guid);

            if (player == null)
                continue;

            if (player.Group == this)
                player.GroupRef.SubGroup = slots[i].Group;
            else
                player.OriginalGroupRef.SubGroup = slots[i].Group;
        }

        _characterDatabase.CommitTransaction(trans);

        SendUpdate();
    }

    public void Update(uint diff)
    {
        if (_isLeaderOffline)
        {
            _leaderOfflineTimer.Update(diff);

            if (_leaderOfflineTimer.Passed)
            {
                SelectNewPartyOrRaidLeader();
                _isLeaderOffline = false;
            }
        }

        UpdateReadyCheck(diff);
    }

    public void UpdateLooterGuid(WorldObject pLootedObject, bool ifneed = false)
    {
        switch (LootMethod)
        {
            case LootMethod.MasterLoot:
            case LootMethod.FreeForAll:
                return;
        }

        var oldLooterGUID = LooterGuid;
        var memberSlot = _getMemberSlot(oldLooterGUID);

        if (memberSlot != null)
            if (ifneed)
            {
                // not update if only update if need and ok
                var looter = _objectAccessor.FindPlayer(memberSlot.Guid);

                if (looter != null && looter.IsAtGroupRewardDistance(pLootedObject))
                    return;
            }

        // search next after current
        Player pNewLooter = null;

        foreach (var member in MemberSlots)
        {
            if (member == memberSlot)
                continue;

            var player = _objectAccessor.FindPlayer(member.Guid);

            if (player == null)
                continue;

            if (!player.IsAtGroupRewardDistance(pLootedObject))
                continue;

            pNewLooter = player;

            break;
        }

        if (pNewLooter == null)
            // search from start
            foreach (var player in from member in MemberSlots select _objectAccessor.FindPlayer(member.Guid) into player where player != null where player.IsAtGroupRewardDistance(pLootedObject) select player)
            {
                pNewLooter = player;

                break;
            }

        if (pNewLooter != null)
        {
            if (oldLooterGUID == pNewLooter.GUID)
                return;

            SetLooterGuid(pNewLooter.GUID);
            SendUpdate();
        }
        else
        {
            SetLooterGuid(ObjectGuid.Empty);
            SendUpdate();
        }
    }

    public void UpdatePlayerOutOfRange(Player player)
    {
        if (player == null || !player.Location.IsInWorld)
            return;

        PartyMemberFullState packet = new();
        packet.Initialize(player);

        for (var refe = FirstMember; refe != null; refe = refe.Next())
        {
            var member = refe.Source;

            if (member != null && member != player && (!member.Location.IsInMap(player) || !member.Location.IsWithinDist(player, member.Visibility.GetSightRange(), false)))
                member.SendPacket(packet);
        }
    }

    private MemberSlot _getMemberSlot(ObjectGuid guid)
    {
        return MemberSlots.FirstOrDefault(member => member.Guid == guid);
    }

    private void _homebindIfInstance(Player player)
    {
        if (player is { IsGameMaster: false } && _cliDB.MapStorage.LookupByKey(player.Location.MapId).IsDungeon())
            player.InstanceValid = false;
    }

    private void _initRaidSubGroupsCounter()
    {
        // Sub group counters initialization
        _subGroupsCounts ??= new byte[MapConst.MaxRaidSubGroups];

        foreach (var memberSlot in MemberSlots)
            ++_subGroupsCounts[memberSlot.Group];
    }

    private void DelinkMember(ObjectGuid guid)
    {
        var refe = _memberMgr.GetFirst();

        while (refe != null)
        {
            var nextRef = refe.Next();

            if (refe.Source.GUID == guid)
            {
                refe.Unlink();

                break;
            }

            refe = nextRef;
        }
    }

    private void EndReadyCheck()
    {
        if (!IsReadyCheckStarted)
            return;

        IsReadyCheckStarted = false;
        _readyCheckTimer = TimeSpan.Zero;

        ResetMemberReadyChecked();

        ReadyCheckCompleted readyCheckCompleted = new()
        {
            PartyIndex = 0,
            PartyGUID = _guid
        };

        BroadcastPacket(readyCheckCompleted, false);
    }

    private void ResetMemberReadyChecked()
    {
        foreach (var member in MemberSlots)
            member.ReadyChecked = false;
    }

    private void SelectNewPartyOrRaidLeader()
    {
        Player newLeader = null;

        // Attempt to give leadership to main assistant first
        if (IsRaidGroup)
            foreach (var memberSlot in MemberSlots)
                if (memberSlot.Flags.HasFlag(GroupMemberFlags.Assistant))
                {
                    var player = _objectAccessor.FindPlayer(memberSlot.Guid);

                    if (player != null)
                    {
                        newLeader = player;

                        break;
                    }
                }

        // If there aren't assistants in raid, or if the group is not a raid, pick the first available member
        if (newLeader == null)
            foreach (var player in MemberSlots.Select(memberSlot => _objectAccessor.FindPlayer(memberSlot.Guid)).Where(player => player != null))
            {
                newLeader = player;

                break;
            }

        if (newLeader == null)
            return;

        ChangeLeader(newLeader.GUID);
        SendUpdate();
    }

    private void SendUpdateDestroyGroupToPlayer(Player player)
    {
        PartyUpdate partyUpdate = new()
        {
            PartyFlags = GroupFlags.Destroyed,
            PartyIndex = (byte)GroupCategory,
            PartyType = GroupType.None,
            PartyGUID = _guid,
            MyIndex = -1,
            SequenceNum = player.NextGroupUpdateSequenceNumber(GroupCategory)
        };

        player.SendPacket(partyUpdate);
    }

    private void SetMemberReadyCheck(MemberSlot slot, bool ready)
    {
        ReadyCheckResponse response = new()
        {
            PartyGUID = _guid,
            Player = slot.Guid,
            IsReady = ready
        };

        BroadcastPacket(response, false);

        SetMemberReadyChecked(slot);
    }

    private void SetMemberReadyChecked(MemberSlot slot)
    {
        slot.ReadyChecked = true;

        if (IsReadyCheckCompleted)
            EndReadyCheck();
    }

    private void SetOfflineMembersReadyChecked()
    {
        foreach (var member in MemberSlots)
        {
            var player = _objectAccessor.FindConnectedPlayer(member.Guid);

            if (player?.Session == null)
                SetMemberReadyCheck(member, false);
        }
    }

    private void SubGroupCounterDecrease(byte subgroup)
    {
        if (_subGroupsCounts != null)
            --_subGroupsCounts[subgroup];
    }

    private void SubGroupCounterIncrease(byte subgroup)
    {
        if (_subGroupsCounts != null)
            ++_subGroupsCounts[subgroup];
    }

    private void ToggleGroupMemberFlag(MemberSlot slot, GroupMemberFlags flag, bool apply)
    {
        if (apply)
            slot.Flags |= flag;
        else
            slot.Flags &= ~flag;
    }

    private void UpdateReadyCheck(uint diff)
    {
        if (!IsReadyCheckStarted)
            return;

        _readyCheckTimer -= TimeSpan.FromMilliseconds(diff);

        if (_readyCheckTimer <= TimeSpan.Zero)
            EndReadyCheck();
    }
}