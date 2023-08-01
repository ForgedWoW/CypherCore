// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Threading;
using Forged.MapServer.Arenas;
using Forged.MapServer.Guilds;
using Framework.Constants;
using Framework.Database;

namespace Forged.MapServer.Globals.Caching;

public class IdGeneratorCache : IObjectCache
{
    private readonly ArenaTeamManager _arenaTeamManager;
    private readonly CharacterDatabase _characterDatabase;
    private readonly GuildManager _guildManager;
    private readonly ObjectGuidGeneratorFactory _objectGuidGeneratorFactory;
    private readonly WorldDatabase _worldDatabase;
    private uint _auctionId;
    private ulong _creatureSpawnId;
    private ulong _equipmentSetGuid;
    private ulong _gameObjectSpawnId;
    private ulong _mailId;
    private ulong _voidItemId;

    public IdGeneratorCache(CharacterDatabase characterDatabase, ObjectGuidGeneratorFactory objectGuidGeneratorFactory, WorldDatabase worldDatabase,
                            ArenaTeamManager arenaTeamManager, GuildManager guildManager)
    {
        _characterDatabase = characterDatabase;
        _objectGuidGeneratorFactory = objectGuidGeneratorFactory;
        _worldDatabase = worldDatabase;
        _arenaTeamManager = arenaTeamManager;
        _guildManager = guildManager;
    }

    public uint GenerateAuctionID()
    {
        return Interlocked.Increment(ref _auctionId);
    }

    public ulong GenerateCreatureSpawnId()
    {
        return Interlocked.Increment(ref _creatureSpawnId);
    }

    public ulong GenerateEquipmentSetGuid()
    {
        return Interlocked.Increment(ref _equipmentSetGuid);
    }

    public ulong GenerateGameObjectSpawnId()
    {
        return Interlocked.Increment(ref _gameObjectSpawnId);
    }

    public ulong GenerateMailID()
    {
        return Interlocked.Increment(ref _mailId);
    }

    public ulong GenerateVoidStorageItemId()
    {
        return Interlocked.Increment(ref _voidItemId);
    }

    public void Load()
    {
        var result = _characterDatabase.Query("SELECT MAX(guid) FROM characters");
        var playerGenerator = _objectGuidGeneratorFactory.GetGenerator(HighGuid.Player);
        var itemGenerator = _objectGuidGeneratorFactory.GetGenerator(HighGuid.Item);
        var transportGenerator = _objectGuidGeneratorFactory.GetGenerator(HighGuid.Transport);

        if (!result.IsEmpty())
            playerGenerator.Set(result.Read<ulong>(0) + 1);

        result = _characterDatabase.Query("SELECT MAX(guid) FROM item_instance");

        if (!result.IsEmpty())
            itemGenerator.Set(result.Read<ulong>(0) + 1);

        // Cleanup other tables from not existed guids ( >= hiItemGuid)
        _characterDatabase.Execute("DELETE FROM character_inventory WHERE item >= {0}", itemGenerator.GetNextAfterMaxUsed()); // One-time query
        _characterDatabase.Execute("DELETE FROM mail_items WHERE item_guid >= {0}", itemGenerator.GetNextAfterMaxUsed());     // One-time query

        _characterDatabase.Execute("DELETE a, ab, ai FROM auctionhouse a LEFT JOIN auction_bidders ab ON ab.auctionId = a.id LEFT JOIN auction_items ai ON ai.auctionId = a.id WHERE ai.itemGuid >= '{0}'",
                                   itemGenerator.GetNextAfterMaxUsed()); // One-time query

        _characterDatabase.Execute("DELETE FROM guild_bank_item WHERE item_guid >= {0}", itemGenerator.GetNextAfterMaxUsed()); // One-time query

        result = _worldDatabase.Query("SELECT MAX(guid) FROM transports");

        if (!result.IsEmpty())
            transportGenerator.Set(result.Read<ulong>(0) + 1);

        result = _characterDatabase.Query("SELECT MAX(id) FROM auctionhouse");

        if (!result.IsEmpty())
            _auctionId = result.Read<uint>(0) + 1;

        result = _characterDatabase.Query("SELECT MAX(id) FROM mail");

        if (!result.IsEmpty())
            _mailId = result.Read<ulong>(0) + 1;

        result = _characterDatabase.Query("SELECT MAX(arenateamid) FROM arena_team");

        if (!result.IsEmpty())
            _arenaTeamManager.SetNextArenaTeamId(result.Read<uint>(0) + 1);

        result = _characterDatabase.Query("SELECT MAX(maxguid) FROM ((SELECT MAX(setguid) AS maxguid FROM character_equipmentsets) UNION (SELECT MAX(setguid) AS maxguid FROM character_transmog_outfits)) allsets");

        if (!result.IsEmpty())
            _equipmentSetGuid = result.Read<ulong>(0) + 1;

        result = _characterDatabase.Query("SELECT MAX(guildId) FROM guild");

        if (!result.IsEmpty())
            _guildManager.SetNextGuildId(result.Read<uint>(0) + 1);

        result = _characterDatabase.Query("SELECT MAX(itemId) from character_void_storage");

        if (!result.IsEmpty())
            _voidItemId = result.Read<ulong>(0) + 1;

        result = _worldDatabase.Query("SELECT MAX(guid) FROM creature");

        if (!result.IsEmpty())
            _creatureSpawnId = result.Read<ulong>(0) + 1;

        result = _worldDatabase.Query("SELECT MAX(guid) FROM gameobject");

        if (!result.IsEmpty())
            _gameObjectSpawnId = result.Read<ulong>(0) + 1;
    }
}