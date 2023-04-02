// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.Groups;

public class GroupManager
{
    private readonly CharacterDatabase _characterDatabase;
    private readonly Dictionary<uint, PlayerGroup> _groupDbStore = new();
    private readonly Dictionary<ulong, PlayerGroup> _groupStore = new();
    private uint _nextGroupDbStoreId;
    private ulong _nextGroupId;
    public GroupManager(CharacterDatabase characterDatabase)
    {
        _characterDatabase = characterDatabase;
        _nextGroupDbStoreId = 1;
        _nextGroupId = 1;
    }

    public void AddGroup(PlayerGroup group)
    {
        _groupStore[group.GUID.Counter] = group;
    }

    public void FreeGroupDbStoreId(PlayerGroup group)
    {
        var storageId = group.DbStoreId;

        if (storageId < _nextGroupDbStoreId)
            _nextGroupDbStoreId = storageId;

        _groupDbStore[storageId - 1] = null;
    }

    public ulong GenerateGroupId()
    {
        return _nextGroupId++;
    }

    public uint GenerateNewGroupDbStoreId()
    {
        var newStorageId = _nextGroupDbStoreId;

        for (var i = ++_nextGroupDbStoreId; i < 0xFFFFFFFF; ++i)
            if ((i < _groupDbStore.Count && _groupDbStore[i] == null) || i >= _groupDbStore.Count)
            {
                _nextGroupDbStoreId = i;

                break;
            }

        return newStorageId;
    }

    public PlayerGroup GetGroupByDbStoreId(uint storageId)
    {
        return _groupDbStore.LookupByKey(storageId);
    }

    public PlayerGroup GetGroupByGuid(ObjectGuid groupId)
    {
        return _groupStore.LookupByKey(groupId.Counter);
    }

    public void LoadGroups()
    {
        {
            var oldMSTime = Time.MSTime;

            // Delete all members that does not exist
            _characterDatabase.DirectExecute("DELETE FROM group_member WHERE memberGuid NOT IN (SELECT guid FROM characters)");
            // Delete all groups whose leader does not exist
            _characterDatabase.DirectExecute("DELETE FROM `groups` WHERE leaderGuid NOT IN (SELECT guid FROM characters)");
            // Delete all groups with less than 2 members
            _characterDatabase.DirectExecute("DELETE FROM `groups` WHERE guid NOT IN (SELECT guid FROM group_member GROUP BY guid HAVING COUNT(guid) > 1)");
            // Delete all rows from group_member with no group
            _characterDatabase.DirectExecute("DELETE FROM group_member WHERE guid NOT IN (SELECT guid FROM `groups`)");

            //                                                    0              1           2             3                 4      5          6      7         8       9
            var result = _characterDatabase.Query("SELECT g.leaderGuid, g.lootMethod, g.looterGuid, g.lootThreshold, g.icon1, g.icon2, g.icon3, g.icon4, g.icon5, g.icon6" +
                                                  //  10         11          12         13              14                  15                     16             17          18         19
                                                  ", g.icon7, g.icon8, g.groupType, g.difficulty, g.raiddifficulty, g.legacyRaidDifficulty, g.masterLooterGuid, g.guid, lfg.dungeon, lfg.state FROM `groups` g LEFT JOIN lfg_data lfg ON lfg.guid = g.guid ORDER BY g.guid ASC");

            if (result.IsEmpty())
            {
                Log.Logger.Information("Loaded 0 group definitions. DB table `groups` is empty!");

                return;
            }

            uint count = 0;

            do
            {
                PlayerGroup group = new();
                group.LoadGroupFromDB(result.GetFields());
                AddGroup(group);

                // Get the ID used for storing the group in the database and register it in the pool.
                var storageId = group.DbStoreId;

                RegisterGroupDbStoreId(storageId, group);

                // Increase the next available storage ID
                if (storageId == _nextGroupDbStoreId)
                    _nextGroupDbStoreId++;

                ++count;
            } while (result.NextRow());

            Log.Logger.Information("Loaded {0} group definitions in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        Log.Logger.Information("Loading Group members...");

        {
            var oldMSTime = Time.MSTime;

            //                                                0        1           2            3       4
            var result = _characterDatabase.Query("SELECT guid, memberGuid, memberFlags, subgroup, roles FROM group_member ORDER BY guid");

            if (result.IsEmpty())
            {
                Log.Logger.Information("Loaded 0 group members. DB table `group_member` is empty!");

                return;
            }

            uint count = 0;

            do
            {
                var group = GetGroupByDbStoreId(result.Read<uint>(0));

                if (group)
                    group.LoadMemberFromDB(result.Read<uint>(1), result.Read<byte>(2), result.Read<byte>(3), (LfgRoles)result.Read<byte>(4));
                else
                    Log.Logger.Error("GroupMgr:LoadGroups: Consistency failed, can't find group (storage id: {0})", result.Read<uint>(0));

                ++count;
            } while (result.NextRow());

            Log.Logger.Information("Loaded {0} group members in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        }
    }

    public void RegisterGroupDbStoreId(uint storageId, PlayerGroup group)
    {
        _groupDbStore[storageId] = group;
    }
    public void RemoveGroup(PlayerGroup group)
    {
        _groupStore.Remove(group.GUID.Counter);
    }

    public void Update(uint diff)
    {
        foreach (var group in _groupStore.Values)
            group.Update(diff);
    }
}