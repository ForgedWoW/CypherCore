// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Arenas;
using Forged.MapServer.Cache;
using Forged.MapServer.Chrono;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Objects.Update;
using Forged.MapServer.Garrisons;
using Forged.MapServer.Globals;
using Forged.MapServer.Groups;
using Forged.MapServer.Guilds;
using Forged.MapServer.Mails;
using Forged.MapServer.Maps;
using Forged.MapServer.Phasing;
using Forged.MapServer.World;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
using Game.Common;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Entities.Players;

public class PlayerComputators
{
    private readonly ArenaTeamManager _arenaTeamManager;
    private readonly CharacterCache _characterCache;
    private readonly CharacterDatabase _characterDatabase;
    private readonly ClassFactory _classFactory;
    private readonly CliDB _cliDB;
    private readonly IConfiguration _configuration;
    private readonly GameObjectManager _gameObjectManager;
    private readonly GroupManager _groupManager;
    private readonly GuildManager _guildManager;
    private readonly LoginDatabase _loginDatabase;
    private readonly ObjectAccessor _objectAccessor;
    private readonly PetitionManager _petitionManager;
    private readonly PhasingHandler _phasingHandler;
    private readonly SocialManager _socialManager;
    private readonly TerrainManager _terrainManager;
    private readonly WorldManager _worldManager;

    public PlayerComputators(CliDB cliDB, IConfiguration configuration, CharacterCache characterCache, GuildManager guildManager,
                             CharacterDatabase characterDatabase, GroupManager groupManager, ObjectAccessor objectAccessor, SocialManager socialManager,
                             LoginDatabase loginDatabase, WorldManager worldManager, TerrainManager terrainManager, PetitionManager petitionManager,
                             GameObjectManager gameObjectManager, ArenaTeamManager arenaTeamManager, ClassFactory classFactory, PhasingHandler phasingHandler)
    {
        _cliDB = cliDB;
        _configuration = configuration;
        _characterCache = characterCache;
        _guildManager = guildManager;
        _characterDatabase = characterDatabase;
        _groupManager = groupManager;
        _objectAccessor = objectAccessor;
        _socialManager = socialManager;
        _loginDatabase = loginDatabase;
        _worldManager = worldManager;
        _terrainManager = terrainManager;
        _petitionManager = petitionManager;
        _gameObjectManager = gameObjectManager;
        _arenaTeamManager = arenaTeamManager;
        _classFactory = classFactory;
        _phasingHandler = phasingHandler;
    }

    public static void RemoveFromGroup(PlayerGroup group, ObjectGuid guid, RemoveMethod method = RemoveMethod.Default, ObjectGuid kicker = default, string reason = null)
    {
        group?.RemoveMember(guid, method, kicker, reason);
    }

    public Difficulty CheckLoadedDungeonDifficultyId(Difficulty difficulty)
    {
        var difficultyEntry = _cliDB.DifficultyStorage.LookupByKey(difficulty);

        if (difficultyEntry is not { InstanceType: MapTypes.Instance })
            return Difficulty.Normal;

        return !difficultyEntry.Flags.HasAnyFlag(DifficultyFlags.CanSelect) ? Difficulty.Normal : difficulty;
    }

    public Difficulty CheckLoadedLegacyRaidDifficultyId(Difficulty difficulty)
    {
        var difficultyEntry = _cliDB.DifficultyStorage.LookupByKey(difficulty);

        if (difficultyEntry is not { InstanceType: MapTypes.Raid })
            return Difficulty.Raid10N;

        if (!difficultyEntry.Flags.HasAnyFlag(DifficultyFlags.CanSelect) || !difficultyEntry.Flags.HasAnyFlag(DifficultyFlags.Legacy))
            return Difficulty.Raid10N;

        return difficulty;
    }

    public Difficulty CheckLoadedRaidDifficultyId(Difficulty difficulty)
    {
        var difficultyEntry = _cliDB.DifficultyStorage.LookupByKey(difficulty);

        if (difficultyEntry is not { InstanceType: MapTypes.Raid })
            return Difficulty.NormalRaid;

        if (!difficultyEntry.Flags.HasAnyFlag(DifficultyFlags.CanSelect) || difficultyEntry.Flags.HasAnyFlag(DifficultyFlags.Legacy))
            return Difficulty.NormalRaid;

        return difficulty;
    }

    public void DeleteFromDB(ObjectGuid playerGuid, uint accountId, bool updateRealmChars = true, bool deleteFinally = false)
    {
        // Avoid realm-update for non-existing account
        if (accountId == 0)
            updateRealmChars = false;

        // Convert guid to low GUID for CharacterNameData, but also other methods on success
        var guid = playerGuid.Counter;
        var charDeleteMethod = (CharDeleteMethod)_configuration.GetDefaultValue("CharDelete:Method", 0);
        var characterInfo = _characterCache.GetCharacterCacheByGuid(playerGuid);
        var name = "<Unknown>";

        if (characterInfo != null)
            name = characterInfo.Name;

        if (deleteFinally)
            charDeleteMethod = CharDeleteMethod.Remove;
        else if (characterInfo != null) // To avoid a Select, we select loaded data. If it doesn't exist, return.
        {
            // Define the required variables

            var charDeleteMinLvl = characterInfo.ClassId switch
            {
                PlayerClass.Deathknight => _configuration.GetDefaultValue("CharDelete:DeathKnight:MinLevel", 0u),
                PlayerClass.DemonHunter => _configuration.GetDefaultValue("CharDelete:DemonHunter:MinLevel", 0u),
                _                       => _configuration.GetDefaultValue("CharDelete:MinLevel", 0u)
            };

            // if we want to finalize the character removal or the character does not meet the level requirement of either heroic or non-heroic settings,
            // we set it to mode CHAR_DELETE_REMOVE
            if (characterInfo.Level < charDeleteMinLvl)
                charDeleteMethod = CharDeleteMethod.Remove;
        }

        SQLTransaction trans = new();
        SQLTransaction loginTransaction = new();

        var guildId = _characterCache.GetCharacterGuildIdByGuid(playerGuid);

        if (guildId != 0)
            _guildManager.GetGuildById(guildId).DeleteMember(trans, playerGuid, false, false, true);

        // remove from arena teams
        LeaveAllArenaTeams(playerGuid);

        // the player was uninvited already on logout so just remove from group
        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_GROUP_MEMBER);
        stmt.AddValue(0, guid);
        var resultGroup = _characterDatabase.Query(stmt);

        if (!resultGroup.IsEmpty())
            RemoveFromGroup(_groupManager.GetGroupByDbStoreId(resultGroup.Read<uint>(0)), playerGuid);

        // Remove signs from petitions (also remove petitions if owner);
        RemovePetitionsAndSigns(playerGuid);

        switch (charDeleteMethod)
        {
            // Completely remove from the database
            case CharDeleteMethod.Remove:
            {
                stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHAR_COD_ITEM_MAIL);
                stmt.AddValue(0, guid);
                var resultMail = _characterDatabase.Query(stmt);

                if (!resultMail.IsEmpty())
                {
                    MultiMap<ulong, Item> itemsByMail = new();

                    stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_MAILITEMS);
                    stmt.AddValue(0, guid);
                    var resultItems = _characterDatabase.Query(stmt);

                    if (!resultItems.IsEmpty())
                    {
                        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_MAILITEMS_ARTIFACT);
                        stmt.AddValue(0, guid);
                        var artifactResult = _characterDatabase.Query(stmt);

                        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_MAILITEMS_AZERITE);
                        stmt.AddValue(0, guid);
                        var azeriteResult = _characterDatabase.Query(stmt);

                        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_MAILITEMS_AZERITE_MILESTONE_POWER);
                        stmt.AddValue(0, guid);
                        var azeriteItemMilestonePowersResult = _characterDatabase.Query(stmt);

                        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_MAILITEMS_AZERITE_UNLOCKED_ESSENCE);
                        stmt.AddValue(0, guid);
                        var azeriteItemUnlockedEssencesResult = _characterDatabase.Query(stmt);

                        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_MAILITEMS_AZERITE_EMPOWERED);
                        stmt.AddValue(0, guid);
                        var azeriteEmpoweredItemResult = _characterDatabase.Query(stmt);

                        Dictionary<ulong, ItemAdditionalLoadInfo> additionalData = new();
                        ItemAdditionalLoadInfo.Init(additionalData, artifactResult, azeriteResult, azeriteItemMilestonePowersResult, azeriteItemUnlockedEssencesResult, azeriteEmpoweredItemResult);

                        do
                        {
                            var mailId = resultItems.Read<ulong>(52);
                            var mailItem = LoadMailedItem(playerGuid, null, mailId, null, resultItems.GetFields(), additionalData.LookupByKey(resultItems.Read<ulong>(0)));

                            if (mailItem != null)
                                itemsByMail.Add(mailId, mailItem);
                        } while (resultItems.NextRow());
                    }

                    do
                    {
                        var mailID = resultMail.Read<ulong>(0);
                        var mailType = (MailMessageType)resultMail.Read<byte>(1);
                        var mailTemplateId = resultMail.Read<ushort>(2);
                        var sender = resultMail.Read<uint>(3);
                        var subject = resultMail.Read<string>(4);
                        var body = resultMail.Read<string>(5);
                        var money = resultMail.Read<ulong>(6);
                        var hasItems = resultMail.Read<bool>(7);

                        // We can return mail now
                        // So firstly delete the old one
                        stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_MAIL_BY_ID);
                        stmt.AddValue(0, mailID);
                        trans.Append(stmt);

                        // Mail is not from player
                        if (mailType != MailMessageType.Normal)
                        {
                            if (hasItems)
                            {
                                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_MAIL_ITEM_BY_ID);
                                stmt.AddValue(0, mailID);
                                trans.Append(stmt);
                            }

                            continue;
                        }

                        var draft = _classFactory.ResolveWithPositionalParameters<MailDraft>(subject, body);

                        if (mailTemplateId != 0)
                            draft = _classFactory.ResolveWithPositionalParameters<MailDraft>(mailTemplateId, false); // items are already included

                        if (itemsByMail.TryGetValue(mailID, out var itemsList))
                        {
                            foreach (var item in itemsList)
                                draft.AddItem(item);

                            itemsByMail.Remove(mailID);
                        }

                        stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_MAIL_ITEM_BY_ID);
                        stmt.AddValue(0, mailID);
                        trans.Append(stmt);

                        var plAccount = _characterCache.GetCharacterAccountIdByGuid(ObjectGuid.Create(HighGuid.Player, guid));

                        draft.AddMoney(money).SendReturnToSender(plAccount, guid, sender, trans);
                    } while (resultMail.NextRow());

                    // Free remaining items
                    foreach (var pair in itemsByMail.KeyValueList)
                        pair.Value.Dispose();
                }

                // Unsummon and delete for pets in world is not required: player deleted from CLI or character list with not loaded pet.
                // NOW we can finally clear other DB data related to character
                stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHAR_PET_IDS);
                stmt.AddValue(0, guid);
                var resultPets = _characterDatabase.Query(stmt);

                if (!resultPets.IsEmpty())
                    do
                    {
                        var petguidlow = resultPets.Read<uint>(0);
                        Pet.DeleteFromDB(petguidlow, _characterDatabase);
                    } while (resultPets.NextRow());

                // Delete char from social list of online chars
                stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHAR_SOCIAL);
                stmt.AddValue(0, guid);
                var resultFriends = _characterDatabase.Query(stmt);

                if (!resultFriends.IsEmpty())
                    do
                    {
                        var playerFriend = _objectAccessor.FindPlayer(ObjectGuid.Create(HighGuid.Player, resultFriends.Read<ulong>(0)));

                        if (playerFriend == null)
                            continue;

                        playerFriend.Social.RemoveFromSocialList(playerGuid, SocialFlag.All);
                        _socialManager.SendFriendStatus(playerFriend, FriendsResult.Removed, playerGuid);
                    } while (resultFriends.NextRow());

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHARACTER);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHARACTER_CUSTOMIZATIONS);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_PLAYER_ACCOUNT_DATA);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_DECLINED_NAME);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_ACTION);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHARACTER_ARENA_STATS);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_AURA_EFFECT);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_AURA);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_PLAYER_BGDATA);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_BATTLEGROUND_RANDOM);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_CUF_PROFILES);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_PLAYER_CURRENCY);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_GIFT);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_PLAYER_HOMEBIND);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHARACTER_INSTANCE_LOCK_BY_GUID);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_INVENTORY);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_QUESTSTATUS);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_QUESTSTATUS_OBJECTIVES);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_QUESTSTATUS_REWARDED);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_REPUTATION);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_SPELL);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_SPELL_COOLDOWNS);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_GEMS_BY_OWNER);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_TRANSMOG_BY_OWNER);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_ARTIFACT_BY_OWNER);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_ARTIFACT_POWERS_BY_OWNER);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_MODIFIERS_BY_OWNER);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_AZERITE_BY_OWNER);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_AZERITE_MILESTONE_POWER_BY_OWNER);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_AZERITE_UNLOCKED_ESSENCE_BY_OWNER);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_AZERITE_EMPOWERED_BY_OWNER);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_BY_OWNER);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_SOCIAL_BY_FRIEND);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_SOCIAL_BY_GUID);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_MAIL);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_MAIL_ITEMS);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_PET_BY_OWNER);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_PET_DECLINEDNAME_BY_OWNER);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_ACHIEVEMENTS);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_ACHIEVEMENT_PROGRESS);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_EQUIPMENTSETS);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_TRANSMOG_OUTFITS);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_GUILD_EVENTLOG_BY_PLAYER);
                stmt.AddValue(0, guid);
                stmt.AddValue(1, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_GUILD_BANK_EVENTLOG_BY_PLAYER);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_GLYPHS);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHARACTER_QUESTSTATUS_DAILY);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHARACTER_QUESTSTATUS_WEEKLY);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHARACTER_QUESTSTATUS_MONTHLY);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHARACTER_QUESTSTATUS_SEASONAL);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_TALENT);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_SKILLS);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_STATS);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_VOID_STORAGE_ITEM_BY_CHAR_GUID);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_FISHINGSTEPS);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHARACTER_FAVORITE_AUCTIONS_BY_CHAR);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHARACTER_AURA_STORED_LOCATIONS_BY_GUID);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _loginDatabase.GetPreparedStatement(LoginStatements.DEL_BATTLE_PET_DECLINED_NAME_BY_OWNER);
                stmt.AddValue(0, guid);
                stmt.AddValue(1, WorldManager.Realm.Id.Index);
                loginTransaction.Append(stmt);

                stmt = _loginDatabase.GetPreparedStatement(LoginStatements.DEL_BATTLE_PETS_BY_OWNER);
                stmt.AddValue(0, guid);
                stmt.AddValue(1, WorldManager.Realm.Id.Index);
                loginTransaction.Append(stmt);

                Corpse.DeleteFromDB(playerGuid, trans, _characterDatabase);

                Garrison.DeleteFromDB(guid, trans, _characterDatabase);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_TRAIT_ENTRIES_BY_CHAR);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_TRAIT_CONFIGS_BY_CHAR);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                _characterCache.DeleteCharacterCacheEntry(playerGuid, name);

                break;
            }
            // The character gets unlinked from the account, the name gets freed up and appears as deleted ingame
            case CharDeleteMethod.Unlink:
            {
                stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_DELETE_INFO);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                _characterCache.UpdateCharacterInfoDeleted(playerGuid, true);

                break;
            }
            default:
                Log.Logger.Error("Player:DeleteFromDB: Unsupported delete method: {0}.", charDeleteMethod);

                if (trans.commands.Count > 0)
                    _characterDatabase.CommitTransaction(trans);

                return;
        }

        _loginDatabase.CommitTransaction(loginTransaction);
        _characterDatabase.CommitTransaction(trans);

        if (updateRealmChars)
            _worldManager.UpdateRealmCharCount(accountId);
    }

    public void DeleteOldCharacters()
    {
        var keepDays = _configuration.GetDefaultValue("CharDelete:KeepDays", 30);

        if (keepDays == 0)
            return;

        DeleteOldCharacters(keepDays);
    }

    public void DeleteOldCharacters(int keepDays)
    {
        Log.Logger.Information("Player:DeleteOldChars: Deleting all characters which have been deleted {0} days before...", keepDays);

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHAR_OLD_CHARS);
        stmt.AddValue(0, (uint)(GameTime.CurrentTime - keepDays * Time.DAY));
        var result = _characterDatabase.Query(stmt);

        if (!result.IsEmpty())
        {
            var count = 0;

            do
            {
                DeleteFromDB(ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(0)), result.Read<uint>(1), true, true);
                count++;
            } while (result.NextRow());

            Log.Logger.Debug("Player:DeleteOldChars: Deleted {0} character(s)", count);
        }
    }

    public WeaponAttackType GetAttackBySlot(byte slot, InventoryType inventoryType)
    {
        return slot switch
        {
            EquipmentSlot.MainHand => inventoryType != InventoryType.Ranged && inventoryType != InventoryType.RangedRight ? WeaponAttackType.BaseAttack : WeaponAttackType.RangedAttack,
            EquipmentSlot.OffHand  => WeaponAttackType.OffAttack,
            _                      => WeaponAttackType.Max,
        };
    }

    public uint GetDefaultGossipMenuForSource(WorldObject source)
    {
        return source.TypeId switch
        {
            TypeId.Unit       => source.AsCreature.GossipMenuId,
            TypeId.GameObject => source.AsGameObject.GossipMenuId,
            _                 => 0
        };
    }

    public DrunkenState GetDrunkenstateByValue(byte value)
    {
        if (value >= 90)
            return DrunkenState.Smashed;

        if (value >= 50)
            return DrunkenState.Drunk;

        if (value != 0)
            return DrunkenState.Tipsy;

        return DrunkenState.Sober;
    }

    public byte GetFactionGroupForRace(Race race)
    {
        if (_cliDB.ChrRacesStorage.TryGetValue((uint)race, out var rEntry))
            if (_cliDB.FactionTemplateStorage.TryGetValue((uint)rEntry.FactionID, out var faction))
                return faction.FactionGroup;

        return 1;
    }

    public uint GetZoneIdFromDB(ObjectGuid guid)
    {
        var guidLow = guid.Counter;
        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHAR_ZONE);
        stmt.AddValue(0, guidLow);
        var result = _characterDatabase.Query(stmt);

        if (result.IsEmpty())
            return 0;

        uint zone = result.Read<ushort>(0);

        if (zone == 0)
        {
            // stored zone is zero, use generic and slow zone detection
            stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHAR_POSITION_XYZ);
            stmt.AddValue(0, guidLow);
            result = _characterDatabase.Query(stmt);

            if (result.IsEmpty())
                return 0;

            uint map = result.Read<ushort>(0);
            var posx = result.Read<float>(1);
            var posy = result.Read<float>(2);
            var posz = result.Read<float>(3);

            if (!_cliDB.MapStorage.ContainsKey(map))
                return 0;

            zone = _terrainManager.GetZoneId(_phasingHandler.EmptyPhaseShift, map, posx, posy, posz);

            if (zone <= 0)
                return zone;

            stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_ZONE);

            stmt.AddValue(0, zone);
            stmt.AddValue(1, guidLow);

            _characterDatabase.Execute(stmt);
        }

        return zone;
    }

    public bool IsBagPos(ushort pos)
    {
        var bag = (byte)(pos >> 8);
        var slot = (byte)(pos & 255);

        if (bag == InventorySlots.Bag0 && slot is >= InventorySlots.BagStart and < InventorySlots.BagEnd)
            return true;

        if (bag == InventorySlots.Bag0 && slot is >= InventorySlots.BankBagStart and < InventorySlots.BankBagEnd)
            return true;

        if (bag == InventorySlots.Bag0 && slot is >= InventorySlots.ReagentBagStart and < InventorySlots.ReagentBagEnd)
            return true;

        return false;
    }

    public bool IsBankPos(ushort pos)
    {
        return IsBankPos((byte)(pos >> 8), (byte)(pos & 255));
    }

    public bool IsBankPos(byte bag, byte slot)
    {
        if (bag == InventorySlots.Bag0 && slot is >= InventorySlots.BankItemStart and < InventorySlots.BankItemEnd)
            return true;

        if (bag == InventorySlots.Bag0 && slot is >= InventorySlots.BankBagStart and < InventorySlots.BankBagEnd)
            return true;

        if (bag is >= InventorySlots.BankBagStart and < InventorySlots.BankBagEnd)
            return true;

        if (bag == InventorySlots.Bag0 && slot is >= InventorySlots.ReagentStart and < InventorySlots.ReagentEnd)
            return true;

        return false;
    }

    public bool IsChildEquipmentPos(byte bag, byte slot)
    {
        return bag == InventorySlots.Bag0 && slot is >= InventorySlots.ChildEquipmentStart and < InventorySlots.ChildEquipmentEnd;
    }

    public bool IsChildEquipmentPos(ushort pos)
    {
        return IsChildEquipmentPos((byte)(pos >> 8), (byte)(pos & 255));
    }

    public bool IsEquipmentPos(ushort pos)
    {
        return IsEquipmentPos((byte)(pos >> 8), (byte)(pos & 255));
    }

    public bool IsEquipmentPos(byte bag, byte slot)
    {
        if (bag == InventorySlots.Bag0 && slot < EquipmentSlot.End)
            return true;

        if (bag == InventorySlots.Bag0 && slot is >= ProfessionSlots.Start and < ProfessionSlots.End)
            return true;

        if (bag == InventorySlots.Bag0 && slot is >= InventorySlots.BagStart and < InventorySlots.BagEnd)
            return true;

        if (bag == InventorySlots.Bag0 && slot is >= InventorySlots.ReagentBagStart and < InventorySlots.ReagentBagEnd)
            return true;

        return false;
    }

    public bool IsInventoryPos(byte bag, byte slot)
    {
        if (bag == InventorySlots.Bag0 && slot == ItemConst.NullSlot)
            return true;

        if (bag == InventorySlots.Bag0 && slot is >= InventorySlots.ItemStart and < InventorySlots.ItemEnd)
            return true;

        if (bag is >= InventorySlots.BagStart and < InventorySlots.BagEnd)
            return true;

        if (bag == InventorySlots.Bag0 && slot is >= InventorySlots.ReagentStart and < InventorySlots.ReagentEnd)
            return true;

        if (bag == InventorySlots.Bag0 && slot is >= InventorySlots.ChildEquipmentStart and < InventorySlots.ChildEquipmentEnd)
            return true;

        return false;
    }

    public bool IsReagentBankPos(ushort pos)
    {
        return IsReagentBankPos((byte)(pos >> 8), (byte)(pos & 255));
    }

    public bool IsReagentBankPos(byte bag, byte slot)
    {
        if (bag == InventorySlots.Bag0 && slot is >= InventorySlots.ReagentStart and < InventorySlots.ReagentEnd)
            return true;

        return false;
    }

    public bool IsValidClass(PlayerClass @class)
    {
        return Convert.ToBoolean((1 << ((int)@class - 1)) & (int)PlayerClass.ClassMaskAllPlayable);
    }

    public bool IsValidGender(Gender gender)
    {
        return gender <= Gender.Female;
    }

    public bool IsValidRace(Race race)
    {
        return Convert.ToBoolean((ulong)SharedConst.GetMaskForRace(race) & SharedConst.RaceMaskAllPlayable);
    }
    public void LeaveAllArenaTeams(ObjectGuid guid)
    {
        var characterInfo = _characterCache.GetCharacterCacheByGuid(guid);

        if (characterInfo == null)
            return;

        for (byte i = 0; i < SharedConst.MaxArenaSlot; ++i)
        {
            var arenaTeamId = characterInfo.ArenaTeamId[i];

            if (arenaTeamId == 0)
                continue;

            var arenaTeam = _arenaTeamManager.GetArenaTeamById(arenaTeamId);

            arenaTeam?.DelMember(guid, true);
        }
    }

    public Item LoadMailedItem(ObjectGuid playerGuid, Player player, ulong mailId, Mail mail, SQLFields fields, ItemAdditionalLoadInfo addionalData)
    {
        var itemGuid = fields.Read<ulong>(0);
        var itemEntry = fields.Read<uint>(1);

        var proto = _gameObjectManager.GetItemTemplate(itemEntry);

        if (proto == null)
        {
            Log.Logger.Error($"Player {(player != null ? player.GetName() : "<unknown>")} ({playerGuid}) has unknown item in mailed items (GUID: {itemGuid} template: {itemEntry}) in mail ({mailId}), deleted.");

            SQLTransaction trans = new();

            var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_INVALID_MAIL_ITEM);
            stmt.AddValue(0, itemGuid);
            trans.Append(stmt);

            ItemFactory.DeleteFromDB(trans, itemGuid);
            AzeriteItemFactory.DeleteFromDB(trans, itemGuid);
            AzeriteEmpoweredItemFactory.DeleteFromDB(trans, itemGuid);

            _characterDatabase.CommitTransaction(trans);

            return null;
        }

        var item = ItemFactory.NewItemOrBag(proto);
        var ownerGuid = fields.Read<ulong>(51) != 0 ? ObjectGuid.Create(HighGuid.Player, fields.Read<ulong>(51)) : ObjectGuid.Empty;

        if (!item.LoadFromDB(itemGuid, ownerGuid, fields, itemEntry))
        {
            Log.Logger.Error($"Player._LoadMailedItems: Item (GUID: {itemGuid}) in mail ({mailId}) doesn't exist, deleted from mail.");

            var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_MAIL_ITEM);
            stmt.AddValue(0, itemGuid);
            _characterDatabase.Execute(stmt);

            item.FSetState(ItemUpdateState.Removed);

            item.SaveToDB(null); // it also deletes item object !

            return null;
        }

        if (addionalData != null)
        {
            if (item.Template.ArtifactID != 0 && addionalData.Artifact != null)
                item.LoadArtifactData(player,
                                      addionalData.Artifact.Xp,
                                      addionalData.Artifact.ArtifactAppearanceId,
                                      addionalData.Artifact.ArtifactTierId,
                                      addionalData.Artifact.ArtifactPowers);

            if (addionalData.AzeriteItem != null)
            {
                var azeriteItem = item.AsAzeriteItem;

                azeriteItem?.LoadAzeriteItemData(player, addionalData.AzeriteItem);
            }

            if (addionalData.AzeriteEmpoweredItem != null)
            {
                var azeriteEmpoweredItem = item.AsAzeriteEmpoweredItem;

                azeriteEmpoweredItem?.LoadAzeriteEmpoweredItemData(player, addionalData.AzeriteEmpoweredItem);
            }
        }

        mail?.AddItem(itemGuid, itemEntry);

        player?.AddMItem(item);

        return item;
    }

    public bool LoadPositionFromDB(out WorldLocation loc, out bool inFlight, ObjectGuid guid)
    {
        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHAR_POSITION);
        stmt.AddValue(0, guid.Counter);
        var result = _characterDatabase.Query(stmt);
        inFlight = false;

        if (result.IsEmpty())
        {
            loc = new WorldLocation();

            return false;
        }

        loc = new WorldLocation(result.Read<ushort>(4), result.Read<float>(0), result.Read<float>(1), result.Read<float>(2), result.Read<float>(3));
        inFlight = !string.IsNullOrEmpty(result.Read<string>(5));

        return true;
    }

    public void OfflineResurrect(ObjectGuid guid, SQLTransaction trans)
    {
        Corpse.DeleteFromDB(guid, trans, _characterDatabase);
        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_ADD_AT_LOGIN_FLAG);
        stmt.AddValue(0, (ushort)AtLoginFlags.Resurrect);
        stmt.AddValue(1, guid.Counter);
        _characterDatabase.ExecuteOrAppend(trans, stmt);
    }

    public void RemovePetitionsAndSigns(ObjectGuid guid)
    {
        _petitionManager.RemoveSignaturesBySigner(guid);
        _petitionManager.RemovePetitionsByOwner(guid);
    }

    public void SaveCustomizations(SQLTransaction trans, ulong guid, List<ChrCustomizationChoice> customizations)
    {
        SavePlayerCustomizations(trans, guid, customizations);
    }

    public void SavePlayerCustomizations(SQLTransaction trans, ulong guid, List<ChrCustomizationChoice> customizations)
    {
        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHARACTER_CUSTOMIZATIONS);
        stmt.AddValue(0, guid);
        trans.Append(stmt);

        foreach (var customization in customizations)
        {
            stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_CHARACTER_CUSTOMIZATION);
            stmt.AddValue(0, guid);
            stmt.AddValue(1, customization.ChrCustomizationOptionID);
            stmt.AddValue(2, customization.ChrCustomizationChoiceID);
            trans.Append(stmt);
        }
    }

    public void SavePositionInDB(WorldLocation loc, uint zoneId, ObjectGuid guid, SQLTransaction trans = null)
    {
        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_CHARACTER_POSITION);
        stmt.AddValue(0, loc.X);
        stmt.AddValue(1, loc.Y);
        stmt.AddValue(2, loc.Z);
        stmt.AddValue(3, loc.Orientation);
        stmt.AddValue(4, (ushort)loc.MapId);
        stmt.AddValue(5, zoneId);
        stmt.AddValue(6, guid.Counter);

        _characterDatabase.ExecuteOrAppend(trans, stmt);
    }
}