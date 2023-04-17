// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Forged.MapServer.Chrono;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.DungeonFinding;

public class LFGQueue
{
    private readonly Dictionary<string, LfgCompatibilityData> _compatibleMapStore = new();

    private readonly List<ObjectGuid> _currentQueueStore = new();

    private readonly List<ObjectGuid> _newToQueueStore = new();

    // Queue
    private readonly Dictionary<ObjectGuid, LfgQueueData> _queueDataStore = new();
    private readonly Dictionary<uint, LfgWaitTime> _waitTimesAvgStore = new();
    private readonly Dictionary<uint, LfgWaitTime> _waitTimesDpsStore = new();
    private readonly Dictionary<uint, LfgWaitTime> _waitTimesHealerStore = new();
    private readonly Dictionary<uint, LfgWaitTime> _waitTimesTankStore = new();

    public static string ConcatenateDungeons(List<uint> dungeons)
    {
        var str = "";

        if (!dungeons.Empty())
            foreach (var it in dungeons)
            {
                if (!string.IsNullOrEmpty(str))
                    str += ", ";

                str += it;
            }

        return str;
    }

    public static string ConcatenateGuids(List<ObjectGuid> guids)
    {
        if (guids.Empty())
            return "";

        // need the guids in order to avoid duplicates
        StringBuilder val = new();
        guids.Sort();
        var it = guids.First();
        val.Append(it);

        foreach (var guid in guids)
        {
            if (guid == it)
                continue;

            val.Append($"|{guid}");
        }

        return val.ToString();
    }

    public static string GetRolesString(LfgRoles roles)
    {
        StringBuilder rolesstr = new();

        if (roles.HasAnyFlag(LfgRoles.Tank))
            rolesstr.Append("Tank");

        if (roles.HasAnyFlag(LfgRoles.Healer))
        {
            if (rolesstr.Capacity != 0)
                rolesstr.Append(", ");

            rolesstr.Append("Healer");
        }

        if (roles.HasAnyFlag(LfgRoles.Damage))
        {
            if (rolesstr.Capacity != 0)
                rolesstr.Append(", ");

            rolesstr.Append("Damage");
        }

        if (roles.HasAnyFlag(LfgRoles.Leader))
        {
            if (rolesstr.Capacity != 0)
                rolesstr.Append(", ");

            rolesstr.Append("Leader");
        }

        if (rolesstr.Capacity == 0)
            rolesstr.Append("None");

        return rolesstr.ToString();
    }

    public void AddQueueData(ObjectGuid guid, long joinTime, List<uint> dungeons, Dictionary<ObjectGuid, LfgRoles> rolesMap)
    {
        _queueDataStore[guid] = new LfgQueueData(joinTime, dungeons, rolesMap);
        AddToQueue(guid);
    }

    public void AddToCurrentQueue(ObjectGuid guid)
    {
        _currentQueueStore.Add(guid);
    }

    public void AddToNewQueue(ObjectGuid guid)
    {
        _newToQueueStore.Add(guid);
    }

    public void AddToQueue(ObjectGuid guid, bool reAdd = false)
    {
        if (!_queueDataStore.ContainsKey(guid))
        {
            Log.Logger.Error("AddToQueue: Queue data not found for [{0}]", guid);

            return;
        }

        if (reAdd)
            AddToFrontCurrentQueue(guid);
        else
            AddToNewQueue(guid);
    }

    public string DumpCompatibleInfo(bool full = false)
    {
        var str = "Compatible Map size: " + _compatibleMapStore.Count + "\n";

        if (full)
            foreach (var pair in _compatibleMapStore)
                str += "(" + pair.Key + "): " + GetCompatibleString(pair.Value.Compatibility) + "\n";

        return str;
    }

    public string DumpQueueInfo()
    {
        uint players = 0;
        uint groups = 0;
        uint playersInGroup = 0;

        for (byte i = 0; i < 2; ++i)
        {
            var queue = i != 0 ? _newToQueueStore : _currentQueueStore;

            foreach (var guid in queue)
                if (guid.IsParty)
                {
                    groups++;
                    playersInGroup += Global.LFGMgr.GetPlayerCount(guid);
                }
                else
                    players++;
        }

        return $"Queued Players: {players} (in group: {playersInGroup}) Groups: {groups}\n";
    }

    public byte FindGroups()
    {
        byte proposals = 0;
        List<ObjectGuid> firstNew = new();

        while (!_newToQueueStore.Empty())
        {
            var frontguid = _newToQueueStore.First();
            Log.Logger.Debug("FindGroups: checking [{0}] newToQueue({1}), currentQueue({2})", frontguid, _newToQueueStore.Count, _currentQueueStore.Count);
            firstNew.Clear();
            firstNew.Add(frontguid);
            RemoveFromNewQueue(frontguid);

            List<ObjectGuid> temporalList = new(_currentQueueStore);
            var compatibles = FindNewGroups(firstNew, temporalList);

            if (compatibles == LfgCompatibility.Match)
                ++proposals;
            else
                AddToCurrentQueue(frontguid); // Lfg group not found, add this group to the queue.
        }

        return proposals;
    }

    public long GetJoinTime(ObjectGuid guid)
    {
        if (_queueDataStore.TryGetValue(guid, out var queueData))
            return queueData.JoinTime;

        return 0;
    }

    public void RemoveFromCurrentQueue(ObjectGuid guid)
    {
        _currentQueueStore.Remove(guid);
    }

    public void RemoveFromNewQueue(ObjectGuid guid)
    {
        _newToQueueStore.Remove(guid);
    }

    public void RemoveFromQueue(ObjectGuid guid)
    {
        RemoveFromNewQueue(guid);
        RemoveFromCurrentQueue(guid);
        RemoveFromCompatibles(guid);

        var sguid = guid.ToString();

        var itDelete = _queueDataStore.LastOrDefault().Key;

        foreach (var key in _queueDataStore.Keys.ToList())
        {
            var data = _queueDataStore[key];

            if (key != guid)
            {
                if (data.BestCompatible.Contains(sguid))
                {
                    data.BestCompatible = "";
                    FindBestCompatibleInQueue(key, data);
                }
            }
            else
                itDelete = key;
        }

        if (!itDelete.IsEmpty)
            _queueDataStore.Remove(itDelete);
    }

    public void RemoveQueueData(ObjectGuid guid)
    {
        _queueDataStore.Remove(guid);
    }

    public void UpdateBestCompatibleInQueue(ObjectGuid guid, LfgQueueData queueData, string key, Dictionary<ObjectGuid, LfgRoles> roles)
    {
        var storedSize = (byte)(string.IsNullOrEmpty(queueData.BestCompatible) ? 0 : queueData.BestCompatible.Count(p => p == '|') + 1);

        var size = (byte)(key.Count(p => p == '|') + 1);

        if (size <= storedSize)
            return;

        Log.Logger.Debug("UpdateBestCompatibleInQueue: Changed ({0}) to ({1}) as best compatible group for {2}",
                         queueData.BestCompatible,
                         key,
                         guid);

        queueData.BestCompatible = key;
        queueData.Tanks = SharedConst.LFGTanksNeeded;
        queueData.Healers = SharedConst.LFGHealersNeeded;
        queueData.Dps = SharedConst.LFGDPSNeeded;

        foreach (var it in roles)
        {
            var role = it.Value;

            if (role.HasAnyFlag(LfgRoles.Tank))
                --queueData.Tanks;
            else if (role.HasAnyFlag(LfgRoles.Healer))
                --queueData.Healers;
            else
                --queueData.Dps;
        }
    }

    public void UpdateQueueTimers(byte queueId, long currTime)
    {
        Log.Logger.Debug("Updating queue timers...");

        foreach (var itQueue in _queueDataStore)
        {
            var queueinfo = itQueue.Value;
            var dungeonId = queueinfo.Dungeons.FirstOrDefault();
            var queuedTime = (uint)(currTime - queueinfo.JoinTime);
            var role = LfgRoles.None;
            var waitTime = -1;

            if (!_waitTimesTankStore.ContainsKey(dungeonId))
                _waitTimesTankStore[dungeonId] = new LfgWaitTime();

            if (!_waitTimesHealerStore.ContainsKey(dungeonId))
                _waitTimesHealerStore[dungeonId] = new LfgWaitTime();

            if (!_waitTimesDpsStore.ContainsKey(dungeonId))
                _waitTimesDpsStore[dungeonId] = new LfgWaitTime();

            if (!_waitTimesAvgStore.ContainsKey(dungeonId))
                _waitTimesAvgStore[dungeonId] = new LfgWaitTime();

            var wtTank = _waitTimesTankStore[dungeonId].Time;
            var wtHealer = _waitTimesHealerStore[dungeonId].Time;
            var wtDps = _waitTimesDpsStore[dungeonId].Time;
            var wtAvg = _waitTimesAvgStore[dungeonId].Time;

            foreach (var itPlayer in queueinfo.Roles)
                role |= itPlayer.Value;

            role &= ~LfgRoles.Leader;

            waitTime = role switch
            {
                LfgRoles.None => // Should not happen - just in case
                    -1,
                LfgRoles.Tank   => wtTank,
                LfgRoles.Healer => wtHealer,
                LfgRoles.Damage => wtDps,
                _               => wtAvg
            };

            if (string.IsNullOrEmpty(queueinfo.BestCompatible))
                FindBestCompatibleInQueue(itQueue.Key, itQueue.Value);

            LfgQueueStatusData queueData = new(queueId, dungeonId, waitTime, wtAvg, wtTank, wtHealer, wtDps, queuedTime, queueinfo.Tanks, queueinfo.Healers, queueinfo.Dps);

            foreach (var itPlayer in queueinfo.Roles)
            {
                var pguid = itPlayer.Key;
                Global.LFGMgr.SendLfgQueueStatus(pguid, queueData);
            }
        }
    }

    public void UpdateWaitTimeAvg(int waitTime, uint dungeonId)
    {
        var wt = _waitTimesAvgStore[dungeonId];
        var oldNumber = wt.Number++;
        wt.Time = (int)((wt.Time * oldNumber + waitTime) / wt.Number);
    }

    public void UpdateWaitTimeDps(int waitTime, uint dungeonId)
    {
        var wt = _waitTimesDpsStore[dungeonId];
        var oldNumber = wt.Number++;
        wt.Time = (int)((wt.Time * oldNumber + waitTime) / wt.Number);
    }

    public void UpdateWaitTimeHealer(int waitTime, uint dungeonId)
    {
        var wt = _waitTimesHealerStore[dungeonId];
        var oldNumber = wt.Number++;
        wt.Time = (int)((wt.Time * oldNumber + waitTime) / wt.Number);
    }

    public void UpdateWaitTimeTank(int waitTime, uint dungeonId)
    {
        var wt = _waitTimesTankStore[dungeonId];
        var oldNumber = wt.Number++;
        wt.Time = (int)((wt.Time * oldNumber + waitTime) / wt.Number);
    }

    private void AddToFrontCurrentQueue(ObjectGuid guid)
    {
        _currentQueueStore.Insert(0, guid);
    }

    private LfgCompatibility CheckCompatibility(List<ObjectGuid> check)
    {
        var strGuids = ConcatenateGuids(check);
        LfgProposal proposal = new();
        List<uint> proposalDungeons;
        Dictionary<ObjectGuid, ObjectGuid> proposalGroups = new();
        Dictionary<ObjectGuid, LfgRoles> proposalRoles = new();

        // Check for correct size
        if (check.Count > MapConst.MaxGroupSize || check.Empty())
        {
            Log.Logger.Debug("CheckCompatibility: ({0}): Size wrong - Not compatibles", strGuids);

            return LfgCompatibility.WrongGroupSize;
        }

        // Check all-but-new compatiblitity
        if (check.Count > 2)
        {
            var frontGuid = check.First();
            check.RemoveAt(0);

            // Check all-but-new compatibilities (New, A, B, C, D) -. check(A, B, C, D)
            var childCompatibles = CheckCompatibility(check);

            if (childCompatibles < LfgCompatibility.WithLessPlayers) // Group not compatible
            {
                Log.Logger.Debug("CheckCompatibility: ({0}) child {1} not compatibles", strGuids, ConcatenateGuids(check));
                SetCompatibles(strGuids, childCompatibles);

                return childCompatibles;
            }

            check.Insert(0, frontGuid);
        }

        // Check if more than one LFG group and number of players joining
        byte numPlayers = 0;
        byte numLfgGroups = 0;

        foreach (var playerGuid in check)
        {
            if (!(numLfgGroups < 2) && !(numPlayers <= MapConst.MaxGroupSize))
                break;

            if (!_queueDataStore.TryGetValue(playerGuid, out var itQueue))
            {
                Log.Logger.Error("CheckCompatibility: [{0}] is not queued but listed as queued!", playerGuid);
                RemoveFromQueue(playerGuid);

                return LfgCompatibility.Pending;
            }

            // Store group so we don't need to call Mgr to get it later (if it's player group will be 0 otherwise would have joined as group)
            foreach (var it2 in itQueue.Roles)
                proposalGroups[it2.Key] = playerGuid.IsPlayer ? playerGuid : ObjectGuid.Empty;

            numPlayers += (byte)itQueue.Roles.Count;

            if (Global.LFGMgr.IsLfgGroup(playerGuid))
            {
                if (numLfgGroups == 0)
                    proposal.Group = playerGuid;

                ++numLfgGroups;
            }
        }

        // Group with less that MAXGROUPSIZE members always compatible
        if (check.Count == 1 && numPlayers != MapConst.MaxGroupSize)
        {
            Log.Logger.Debug("CheckCompatibility: ({0}) sigle group. Compatibles", strGuids);
            var fistPlayer = check.First();
            var itQueue = _queueDataStore.LookupByKey(fistPlayer);

            LfgCompatibilityData data = new(LfgCompatibility.WithLessPlayers)
            {
                Roles = itQueue.Roles
            };

            Global.LFGMgr.CheckGroupRoles(data.Roles);

            UpdateBestCompatibleInQueue(fistPlayer, itQueue, strGuids, data.Roles);
            SetCompatibilityData(strGuids, data);

            return LfgCompatibility.WithLessPlayers;
        }

        if (numLfgGroups > 1)
        {
            Log.Logger.Debug("CheckCompatibility: ({0}) More than one Lfggroup ({1})", strGuids, numLfgGroups);
            SetCompatibles(strGuids, LfgCompatibility.MultipleLfgGroups);

            return LfgCompatibility.MultipleLfgGroups;
        }

        if (numPlayers > MapConst.MaxGroupSize)
        {
            Log.Logger.Debug("CheckCompatibility: ({0}) Too much players ({1})", strGuids, numPlayers);
            SetCompatibles(strGuids, LfgCompatibility.TooMuchPlayers);

            return LfgCompatibility.TooMuchPlayers;
        }

        // If it's single group no need to check for duplicate players, ignores, bad roles or bad dungeons as it's been checked before joining
        if (check.Count > 1)
        {
            foreach (var it in check)
            {
                var roles = _queueDataStore[it].Roles;

                foreach (var rolePair in roles)
                {
                    KeyValuePair<ObjectGuid, LfgRoles> itPlayer = new();

                    foreach (var player in proposalRoles)
                    {
                        itPlayer = player;

                        if (rolePair.Key == itPlayer.Key)
                            Log.Logger.Error("CheckCompatibility: ERROR! Player multiple times in queue! [{0}]", rolePair.Key);
                        else if (Global.LFGMgr.HasIgnore(rolePair.Key, itPlayer.Key))
                            break;
                    }

                    if (itPlayer.Key == proposalRoles.LastOrDefault().Key)
                        proposalRoles[rolePair.Key] = rolePair.Value;
                }
            }

            var playersize = (byte)(numPlayers - proposalRoles.Count);

            if (playersize != 0)
            {
                Log.Logger.Debug("CheckCompatibility: ({0}) not compatible, {1} players are ignoring each other", strGuids, playersize);
                SetCompatibles(strGuids, LfgCompatibility.HasIgnores);

                return LfgCompatibility.HasIgnores;
            }

            StringBuilder o;
            var debugRoles = proposalRoles;

            if (!Global.LFGMgr.CheckGroupRoles(proposalRoles))
            {
                o = new StringBuilder();

                foreach (var it in debugRoles)
                    o.Append($", {it.Key}: {GetRolesString(it.Value)}");

                Log.Logger.Debug("CheckCompatibility: ({0}) Roles not compatible{1}", strGuids, o.ToString());
                SetCompatibles(strGuids, LfgCompatibility.NoRoles);

                return LfgCompatibility.NoRoles;
            }

            var itguid = check.First();
            proposalDungeons = _queueDataStore[itguid].Dungeons;
            o = new StringBuilder();
            o.AppendFormat(", {0}: ({1})", itguid, Global.LFGMgr.ConcatenateDungeons(proposalDungeons));

            foreach (var playerGuid in check)
            {
                if (playerGuid == itguid)
                    continue;

                var dungeons = _queueDataStore[itguid].Dungeons;
                o.AppendFormat(", {0}: ({1})", playerGuid, Global.LFGMgr.ConcatenateDungeons(dungeons));
                var temporal = proposalDungeons.Intersect(dungeons).ToList();
                proposalDungeons = temporal;
            }

            if (proposalDungeons.Empty())
            {
                Log.Logger.Debug("CheckCompatibility: ({0}) No compatible dungeons{1}", strGuids, o.ToString());
                SetCompatibles(strGuids, LfgCompatibility.NoDungeons);

                return LfgCompatibility.NoDungeons;
            }
        }
        else
        {
            var gguid = check.First();
            var queue = _queueDataStore[gguid];
            proposalDungeons = queue.Dungeons;
            proposalRoles = queue.Roles;
            Global.LFGMgr.CheckGroupRoles(proposalRoles); // assing new roles
        }

        // Enough players?
        if (numPlayers != MapConst.MaxGroupSize)
        {
            Log.Logger.Debug("CheckCompatibility: ({0}) Compatibles but not enough players({1})", strGuids, numPlayers);

            LfgCompatibilityData data = new(LfgCompatibility.WithLessPlayers)
            {
                Roles = proposalRoles
            };

            foreach (var playerGuid in check)
            {
                var queueData = _queueDataStore.LookupByKey(playerGuid);
                UpdateBestCompatibleInQueue(playerGuid, queueData, strGuids, data.Roles);
            }

            SetCompatibilityData(strGuids, data);

            return LfgCompatibility.WithLessPlayers;
        }

        var guid = check.First();
        proposal.Queues = check;
        proposal.IsNew = numLfgGroups != 1 || Global.LFGMgr.GetOldState(guid) != LfgState.Dungeon;

        if (!Global.LFGMgr.AllQueued(check))
        {
            Log.Logger.Debug("CheckCompatibility: ({0}) Group MATCH but can't create proposal!", strGuids);
            SetCompatibles(strGuids, LfgCompatibility.BadStates);

            return LfgCompatibility.BadStates;
        }

        // Create a new proposal
        proposal.CancelTime = GameTime.CurrentTime + SharedConst.LFGTimeProposal;
        proposal.State = LfgProposalState.Initiating;
        proposal.Leader = ObjectGuid.Empty;
        proposal.DungeonId = proposalDungeons.SelectRandom();

        var leader = false;

        foreach (var rolePair in proposalRoles)
        {
            // Assing new leader
            if (rolePair.Value.HasAnyFlag(LfgRoles.Leader))
            {
                if (!leader || proposal.Leader.IsEmpty || Convert.ToBoolean(RandomHelper.IRand(0, 1)))
                    proposal.Leader = rolePair.Key;

                leader = true;
            }
            else if (!leader && (proposal.Leader.IsEmpty || Convert.ToBoolean(RandomHelper.IRand(0, 1))))
                proposal.Leader = rolePair.Key;

            // Assing player data and roles
            LfgProposalPlayer data = new()
            {
                Role = rolePair.Value,
                Group = proposalGroups.LookupByKey(rolePair.Key)
            };

            if (!proposal.IsNew && !data.Group.IsEmpty && data.Group == proposal.Group) // Player from existing group, autoaccept
                data.Accept = LfgAnswer.Agree;

            proposal.Players[rolePair.Key] = data;
        }

        // Mark proposal members as not queued (but not remove queue data)
        foreach (var playerGuid in proposal.Queues)
        {
            RemoveFromNewQueue(playerGuid);
            RemoveFromCurrentQueue(playerGuid);
        }

        Global.LFGMgr.AddProposal(proposal);

        Log.Logger.Debug("CheckCompatibility: ({0}) MATCH! Group formed", strGuids);
        SetCompatibles(strGuids, LfgCompatibility.Match);

        return LfgCompatibility.Match;
    }

    private void FindBestCompatibleInQueue(ObjectGuid guid, LfgQueueData data)
    {
        Log.Logger.Debug("FindBestCompatibleInQueue: {0}", guid);

        foreach (var pair in _compatibleMapStore)
            if (pair.Value.Compatibility == LfgCompatibility.WithLessPlayers && pair.Key.Contains(guid.ToString()))
                UpdateBestCompatibleInQueue(guid, data, pair.Key, pair.Value.Roles);
    }

    private LfgCompatibility FindNewGroups(List<ObjectGuid> check, List<ObjectGuid> all)
    {
        var strGuids = ConcatenateGuids(check);
        var compatibles = GetCompatibles(strGuids);

        Log.Logger.Debug("FindNewGroup: ({0}): {1} - all({2})", strGuids, GetCompatibleString(compatibles), ConcatenateGuids(all));

        if (compatibles == LfgCompatibility.Pending) // Not previously cached, calculate
            compatibles = CheckCompatibility(check);

        if (compatibles == LfgCompatibility.BadStates && Global.LFGMgr.AllQueued(check))
        {
            Log.Logger.Debug("FindNewGroup: ({0}) compatibles (cached) changed from bad states to match", strGuids);
            SetCompatibles(strGuids, LfgCompatibility.Match);

            return LfgCompatibility.Match;
        }

        if (compatibles != LfgCompatibility.WithLessPlayers)
            return compatibles;

        // Try to match with queued groups
        while (!all.Empty())
        {
            check.Add(all.First());
            all.RemoveAt(0);
            var subcompatibility = FindNewGroups(check, all);

            if (subcompatibility == LfgCompatibility.Match)
                return LfgCompatibility.Match;

            check.RemoveAt(check.Count - 1);
        }

        return compatibles;
    }

    private LfgCompatibilityData GetCompatibilityData(string key)
    {
        var compatibilityData = _compatibleMapStore.LookupByKey(key);

        return compatibilityData;
    }

    private LfgCompatibility GetCompatibles(string key)
    {
        if (_compatibleMapStore.TryGetValue(key, out var compatibilityData))
            return compatibilityData.Compatibility;

        return LfgCompatibility.Pending;
    }

    private string GetCompatibleString(LfgCompatibility compatibles)
    {
        return compatibles switch
        {
            LfgCompatibility.Pending           => "Pending",
            LfgCompatibility.BadStates         => "Compatibles (Bad States)",
            LfgCompatibility.Match             => "Match",
            LfgCompatibility.WithLessPlayers   => "Compatibles (Not enough players)",
            LfgCompatibility.HasIgnores        => "Has ignores",
            LfgCompatibility.MultipleLfgGroups => "Multiple Lfg Groups",
            LfgCompatibility.NoDungeons        => "Incompatible dungeons",
            LfgCompatibility.NoRoles           => "Incompatible roles",
            LfgCompatibility.TooMuchPlayers    => "Too much players",
            LfgCompatibility.WrongGroupSize    => "Wrong group size",
            _                                  => "Unknown"
        };
    }

    private void RemoveFromCompatibles(ObjectGuid guid)
    {
        var strGuid = guid.ToString();

        Log.Logger.Debug("RemoveFromCompatibles: Removing [{0}]", guid);

        foreach (var itNext in _compatibleMapStore.ToList())
            if (itNext.Key.Contains(strGuid))
                _compatibleMapStore.Remove(itNext.Key);
    }

    private void SetCompatibilityData(string key, LfgCompatibilityData data)
    {
        _compatibleMapStore[key] = data;
    }

    private void SetCompatibles(string key, LfgCompatibility compatibles)
    {
        if (!_compatibleMapStore.ContainsKey(key))
            _compatibleMapStore[key] = new LfgCompatibilityData();

        _compatibleMapStore[key].Compatibility = compatibles;
    }
}
// Stores player or group queue info