// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Arenas;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Networking.Packets.Misc;
using Forged.MapServer.World;
using Framework.Constants;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.Cache;

public class CharacterCache
{
    private readonly Dictionary<string, CharacterCacheEntry> _characterCacheByNameStore = new();
    private readonly Dictionary<ObjectGuid, CharacterCacheEntry> _characterCacheStore = new();
    private readonly CharacterDatabase _characterDatabase;
    private readonly WorldManager _worldManager;
    private readonly CliDB _cliDB;

    public CharacterCache(CharacterDatabase characterDatabase, WorldManager worldManager, CliDB cliDB)
    {
        _characterDatabase = characterDatabase;
        _worldManager = worldManager;
        _cliDB = cliDB;
    }

    public void AddCharacterCacheEntry(ObjectGuid guid, uint accountId, string name, byte gender, byte race, byte playerClass, byte level, bool isDeleted)
    {
        var data = new CharacterCacheEntry
        {
            Guid = guid,
            Name = name,
            AccountId = accountId,
            RaceId = (Race)race,
            Sex = (Gender)gender,
            ClassId = (PlayerClass)playerClass,
            Level = level,
            GuildId = 0 // Will be set in guild loading or guild setting
        };

        for (byte i = 0; i < SharedConst.MaxArenaSlot; ++i)
            data.ArenaTeamId[i] = 0; // Will be set in arena teams loading

        data.IsDeleted = isDeleted;

        // Fill Name to Guid Store
        _characterCacheByNameStore[name] = data;
        _characterCacheStore[guid] = data;
    }

    public void DeleteCharacterCacheEntry(ObjectGuid guid, string name)
    {
        _characterCacheStore.Remove(guid);
        _characterCacheByNameStore.Remove(name);
    }

    public uint GetCharacterAccountIdByGuid(ObjectGuid guid)
    {
        var characterCacheEntry = _characterCacheStore.LookupByKey(guid);

        if (characterCacheEntry == null)
            return 0;

        return characterCacheEntry.AccountId;
    }

    public uint GetCharacterAccountIdByName(string name)
    {
        var characterCacheEntry = _characterCacheByNameStore.LookupByKey(name);

        if (characterCacheEntry != null)
            return characterCacheEntry.AccountId;

        return 0;
    }

    public uint GetCharacterArenaTeamIdByGuid(ObjectGuid guid, byte type)
    {
        var characterCacheEntry = _characterCacheStore.LookupByKey(guid);

        if (characterCacheEntry == null)
            return 0;

        return characterCacheEntry.ArenaTeamId[ArenaTeam.GetSlotByType(type)];
    }

    public CharacterCacheEntry GetCharacterCacheByGuid(ObjectGuid guid)
    {
        return _characterCacheStore.LookupByKey(guid);
    }

    public CharacterCacheEntry GetCharacterCacheByName(string name)
    {
        return _characterCacheByNameStore.LookupByKey(name);
    }

    public ObjectGuid GetCharacterGuidByName(string name)
    {
        var characterCacheEntry = _characterCacheByNameStore.LookupByKey(name);

        if (characterCacheEntry != null)
            return characterCacheEntry.Guid;

        return ObjectGuid.Empty;
    }

    public ulong GetCharacterGuildIdByGuid(ObjectGuid guid)
    {
        var characterCacheEntry = _characterCacheStore.LookupByKey(guid);

        if (characterCacheEntry == null)
            return 0;

        return characterCacheEntry.GuildId;
    }

    public byte GetCharacterLevelByGuid(ObjectGuid guid)
    {
        var characterCacheEntry = _characterCacheStore.LookupByKey(guid);

        if (characterCacheEntry == null)
            return 0;

        return characterCacheEntry.Level;
    }

    public bool GetCharacterNameAndClassByGUID(ObjectGuid guid, out string name, out byte _class)
    {
        name = "Unknown";
        _class = 0;

        var characterCacheEntry = _characterCacheStore.LookupByKey(guid);

        if (characterCacheEntry == null)
            return false;

        name = characterCacheEntry.Name;
        _class = (byte)characterCacheEntry.ClassId;

        return true;
    }

    public bool GetCharacterNameByGuid(ObjectGuid guid, out string name)
    {
        name = "Unknown";
        var characterCacheEntry = _characterCacheStore.LookupByKey(guid);

        if (characterCacheEntry == null)
            return false;

        name = characterCacheEntry.Name;

        return true;
    }

    public TeamFaction GetCharacterTeamByGuid(ObjectGuid guid)
    {
        var characterCacheEntry = _characterCacheStore.LookupByKey(guid);

        if (characterCacheEntry == null)
            return 0;

        return Player.TeamForRace(characterCacheEntry.RaceId, _cliDB);
    }

    public bool HasCharacterCacheEntry(ObjectGuid guid)
    {
        return _characterCacheStore.ContainsKey(guid);
    }

    public void LoadCharacterCacheStorage()
    {
        _characterCacheStore.Clear();
        var oldMSTime = Time.MSTime;

        var result = _characterDatabase.Query("SELECT guid, name, account, race, gender, class, level, deleteDate FROM characters");

        if (result.IsEmpty())
        {
            Log.Logger.Information("No character name data loaded, empty query");

            return;
        }

        do
        {
            AddCharacterCacheEntry(ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(0)), result.Read<uint>(2), result.Read<string>(1), result.Read<byte>(4), result.Read<byte>(3), result.Read<byte>(5), result.Read<byte>(6), result.Read<uint>(7) != 0);
        } while (result.NextRow());

        Log.Logger.Information($"Loaded character infos for {_characterCacheStore.Count} characters in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }
    public void UpdateCharacterAccountId(ObjectGuid guid, uint accountId)
    {
        if (!_characterCacheStore.TryGetValue(guid, out var p))
            return;

        p.AccountId = accountId;
    }

    public void UpdateCharacterArenaTeamId(ObjectGuid guid, byte slot, uint arenaTeamId)
    {
        if (!_characterCacheStore.TryGetValue(guid, out var p))
            return;

        p.ArenaTeamId[slot] = arenaTeamId;
    }

    public void UpdateCharacterData(ObjectGuid guid, string name, byte? gender = null, byte? race = null)
    {
        var characterCacheEntry = _characterCacheStore.LookupByKey(guid);

        if (characterCacheEntry == null)
            return;

        var oldName = characterCacheEntry.Name;
        characterCacheEntry.Name = name;

        if (gender.HasValue)
            characterCacheEntry.Sex = (Gender)gender.Value;

        if (race.HasValue)
            characterCacheEntry.RaceId = (Race)race.Value;

        InvalidatePlayer invalidatePlayer = new()
        {
            Guid = guid
        };

        _worldManager.SendGlobalMessage(invalidatePlayer);

        // Correct name -> pointer storage
        _characterCacheByNameStore.Remove(oldName);
        _characterCacheByNameStore[name] = characterCacheEntry;
    }

    public void UpdateCharacterGender(ObjectGuid guid, byte gender)
    {
        if (!_characterCacheStore.TryGetValue(guid, out var p))
            return;

        p.Sex = (Gender)gender;
    }

    public void UpdateCharacterGuildId(ObjectGuid guid, ulong guildId)
    {
        if (!_characterCacheStore.TryGetValue(guid, out var p))
            return;

        p.GuildId = guildId;
    }

    public void UpdateCharacterInfoDeleted(ObjectGuid guid, bool deleted, string name = null)
    {
        if (!_characterCacheStore.TryGetValue(guid, out var p))
            return;

        p.IsDeleted = deleted;

        if (!name.IsEmpty())
            p.Name = name;
    }

    public void UpdateCharacterLevel(ObjectGuid guid, byte level)
    {
        if (!_characterCacheStore.TryGetValue(guid, out var p))
            return;

        p.Level = level;
    }
}