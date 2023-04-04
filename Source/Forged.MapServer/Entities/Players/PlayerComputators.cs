// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Chrono;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Objects.Update;
using Forged.MapServer.Garrisons;
using Forged.MapServer.Mails;
using Forged.MapServer.Phasing;
using Framework.Constants;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.Entities.Players;

public class PlayerComputators
{
    private Player _player;
    public PlayerComputators(Player player1)
    {
        _player = player1;
    }

    public static WeaponAttackType GetAttackBySlot(byte slot, InventoryType inventoryType)
    {
        return slot switch
        {
            EquipmentSlot.MainHand => inventoryType != InventoryType.Ranged && inventoryType != InventoryType.RangedRight ? WeaponAttackType.BaseAttack : WeaponAttackType.RangedAttack,
            EquipmentSlot.OffHand  => WeaponAttackType.OffAttack,
            _                      => WeaponAttackType.Max,
        };
    }

    public static uint GetDefaultGossipMenuForSource(WorldObject source)
    {
        switch (source.TypeId)
        {
            case TypeId.Unit:
                return source.AsCreature.GossipMenuId;

            case TypeId.GameObject:
                return source.AsGameObject.GossipMenuId;
        }

        return 0;
    }

    public static DrunkenState GetDrunkenstateByValue(byte value)
    {
        if (value >= 90)
            return DrunkenState.Smashed;

        if (value >= 50)
            return DrunkenState.Drunk;

        if (value != 0)
            return DrunkenState.Tipsy;

        return DrunkenState.Sober;
    }

    public static byte GetFactionGroupForRace(Race race)
    {
        var rEntry = _player.CliDB.ChrRacesStorage.LookupByKey((uint)race);

        if (rEntry != null)
        {
            var faction = _player.CliDB.FactionTemplateStorage.LookupByKey(rEntry.FactionID);

            if (faction != null)
                return faction.FactionGroup;
        }

        return 1;
    }

    public static bool IsValidClass(PlayerClass @class)
    {
        return Convert.ToBoolean((1 << ((int)@class - 1)) & (int)PlayerClass.ClassMaskAllPlayable);
    }

    public static bool IsValidGender(Gender gender)
    {
        return gender <= Gender.Female;
    }

    public static bool IsValidRace(Race race)
    {
        return Convert.ToBoolean((ulong)SharedConst.GetMaskForRace(race) & SharedConst.RaceMaskAllPlayable);
    }

    public static void OfflineResurrect(ObjectGuid guid, SQLTransaction trans)
    {
        Corpse.DeleteFromDB(guid, trans);
        var stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_ADD_AT_LOGIN_FLAG);
        stmt.AddValue(0, (ushort)AtLoginFlags.Resurrect);
        stmt.AddValue(1, guid.Counter);
        DB.Characters.ExecuteOrAppend(trans, stmt);
    }

    public static void DeleteFromDB(ObjectGuid playerGuid, uint accountId, bool updateRealmChars = true, bool deleteFinally = false)
    {
        // Avoid realm-update for non-existing account
        if (accountId == 0)
            updateRealmChars = false;

        // Convert guid to low GUID for CharacterNameData, but also other methods on success
        var guid = playerGuid.Counter;
        var charDeleteMethod = (CharDeleteMethod)GetDefaultValue("CharDelete.Method", 0);
        var characterInfo = Global.CharacterCacheStorage.GetCharacterCacheByGuid(playerGuid);
        var name = "<Unknown>";

        if (characterInfo != null)
            name = characterInfo.Name;

        if (deleteFinally)
        {
            charDeleteMethod = CharDeleteMethod.Remove;
        }
        else if (characterInfo != null) // To avoid a Select, we select loaded data. If it doesn't exist, return.
        {
            // Define the required variables
            uint charDeleteMinLvl;

            if (characterInfo.ClassId == PlayerClass.Deathknight)
                charDeleteMinLvl = GetDefaultValue("CharDelete.DeathKnight.MinLevel", 0);
            else if (characterInfo.ClassId == PlayerClass.DemonHunter)
                charDeleteMinLvl = GetDefaultValue("CharDelete.DemonHunter.MinLevel", 0);
            else
                charDeleteMinLvl = GetDefaultValue("CharDelete.MinLevel", 0);

            // if we want to finalize the character removal or the character does not meet the level requirement of either heroic or non-heroic settings,
            // we set it to mode CHAR_DELETE_REMOVE
            if (characterInfo.Level < charDeleteMinLvl)
                charDeleteMethod = CharDeleteMethod.Remove;
        }

        SQLTransaction trans = new();
        SQLTransaction loginTransaction = new();

        var guildId = Global.CharacterCacheStorage.GetCharacterGuildIdByGuid(playerGuid);

        if (guildId != 0)
        {
            var guild = Global.GuildMgr.GetGuildById(guildId);

            if (guild)
                guild.DeleteMember(trans, playerGuid, false, false, true);
        }

        // remove from arena teams
        Player.LeaveAllArenaTeams(playerGuid);

        // the player was uninvited already on logout so just remove from group
        var stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_GROUP_MEMBER);
        stmt.AddValue(0, guid);
        var resultGroup = DB.Characters.Query(stmt);

        if (!resultGroup.IsEmpty())
        {
            var group = Global.GroupMgr.GetGroupByDbStoreId(resultGroup.Read<uint>(0));

            if (group)
                Player.RemoveFromGroup(group, playerGuid);
        }

        // Remove signs from petitions (also remove petitions if owner);
        RemovePetitionsAndSigns(playerGuid);

        switch (charDeleteMethod)
        {
            // Completely remove from the database
            case CharDeleteMethod.Remove:
            {
                stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_COD_ITEM_MAIL);
                stmt.AddValue(0, guid);
                var resultMail = DB.Characters.Query(stmt);

                if (!resultMail.IsEmpty())
                {
                    MultiMap<ulong, Item> itemsByMail = new();

                    stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAILITEMS);
                    stmt.AddValue(0, guid);
                    var resultItems = DB.Characters.Query(stmt);

                    if (!resultItems.IsEmpty())
                    {
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAILITEMS_ARTIFACT);
                        stmt.AddValue(0, guid);
                        var artifactResult = DB.Characters.Query(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAILITEMS_AZERITE);
                        stmt.AddValue(0, guid);
                        var azeriteResult = DB.Characters.Query(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAILITEMS_AZERITE_MILESTONE_POWER);
                        stmt.AddValue(0, guid);
                        var azeriteItemMilestonePowersResult = DB.Characters.Query(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAILITEMS_AZERITE_UNLOCKED_ESSENCE);
                        stmt.AddValue(0, guid);
                        var azeriteItemUnlockedEssencesResult = DB.Characters.Query(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAILITEMS_AZERITE_EMPOWERED);
                        stmt.AddValue(0, guid);
                        var azeriteEmpoweredItemResult = DB.Characters.Query(stmt);

                        Dictionary<ulong, ItemAdditionalLoadInfo> additionalData = new();
                        ItemAdditionalLoadInfo.Init(additionalData, artifactResult, azeriteResult, azeriteItemMilestonePowersResult, azeriteItemUnlockedEssencesResult, azeriteEmpoweredItemResult);

                        do
                        {
                            var mailId = resultItems.Read<ulong>(52);
                            var mailItem = _LoadMailedItem(playerGuid, null, mailId, null, resultItems.GetFields(), additionalData.LookupByKey(resultItems.Read<ulong>(0)));

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
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_MAIL_BY_ID);
                        stmt.AddValue(0, mailID);
                        trans.Append(stmt);

                        // Mail is not from player
                        if (mailType != MailMessageType.Normal)
                        {
                            if (hasItems)
                            {
                                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_MAIL_ITEM_BY_ID);
                                stmt.AddValue(0, mailID);
                                trans.Append(stmt);
                            }

                            continue;
                        }

                        MailDraft draft = new(subject, body);

                        if (mailTemplateId != 0)
                            draft = new MailDraft(mailTemplateId, false); // items are already included

                        var itemsList = itemsByMail.LookupByKey(mailID);

                        if (itemsList != null)
                        {
                            foreach (var item in itemsList)
                                draft.AddItem(item);

                            itemsByMail.Remove(mailID);
                        }

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_MAIL_ITEM_BY_ID);
                        stmt.AddValue(0, mailID);
                        trans.Append(stmt);

                        var plAccount = Global.CharacterCacheStorage.GetCharacterAccountIdByGuid(ObjectGuid.Create(HighGuid.Player, guid));

                        draft.AddMoney(money).SendReturnToSender(plAccount, guid, sender, trans);
                    } while (resultMail.NextRow());

                    // Free remaining items
                    foreach (var pair in itemsByMail.KeyValueList)
                        pair.Value.Dispose();
                }

                // Unsummon and delete for pets in world is not required: player deleted from CLI or character list with not loaded pet.
                // NOW we can finally clear other DB data related to character
                stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_PET_IDS);
                stmt.AddValue(0, guid);
                var resultPets = DB.Characters.Query(stmt);

                if (!resultPets.IsEmpty())
                    do
                    {
                        var petguidlow = resultPets.Read<uint>(0);
                        Pet.DeleteFromDB(petguidlow);
                    } while (resultPets.NextRow());

                // Delete char from social list of online chars
                stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_SOCIAL);
                stmt.AddValue(0, guid);
                var resultFriends = DB.Characters.Query(stmt);

                if (!resultFriends.IsEmpty())
                    do
                    {
                        var playerFriend = Global.ObjAccessor.FindPlayer(ObjectGuid.Create(HighGuid.Player, resultFriends.Read<ulong>(0)));

                        if (playerFriend)
                        {
                            playerFriend.Social.RemoveFromSocialList(playerGuid, SocialFlag.All);
                            Global.SocialMgr.SendFriendStatus(playerFriend, FriendsResult.Removed, playerGuid);
                        }
                    } while (resultFriends.NextRow());

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_CUSTOMIZATIONS);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PLAYER_ACCOUNT_DATA);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_DECLINED_NAME);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_ACTION);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_ARENA_STATS);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_AURA_EFFECT);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_AURA);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PLAYER_BGDATA);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_BATTLEGROUND_RANDOM);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_CUF_PROFILES);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PLAYER_CURRENCY);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_GIFT);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PLAYER_HOMEBIND);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_INSTANCE_LOCK_BY_GUID);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_INVENTORY);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_QUESTSTATUS);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_QUESTSTATUS_OBJECTIVES);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_QUESTSTATUS_REWARDED);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_REPUTATION);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_SPELL);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_SPELL_COOLDOWNS);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_GEMS_BY_OWNER);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_TRANSMOG_BY_OWNER);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_ARTIFACT_BY_OWNER);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_ARTIFACT_POWERS_BY_OWNER);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_MODIFIERS_BY_OWNER);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_AZERITE_BY_OWNER);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_AZERITE_MILESTONE_POWER_BY_OWNER);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_AZERITE_UNLOCKED_ESSENCE_BY_OWNER);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_AZERITE_EMPOWERED_BY_OWNER);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_BY_OWNER);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_SOCIAL_BY_FRIEND);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_SOCIAL_BY_GUID);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_MAIL);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_MAIL_ITEMS);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_PET_BY_OWNER);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_PET_DECLINEDNAME_BY_OWNER);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_ACHIEVEMENTS);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_ACHIEVEMENT_PROGRESS);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_EQUIPMENTSETS);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_TRANSMOG_OUTFITS);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GUILD_EVENTLOG_BY_PLAYER);
                stmt.AddValue(0, guid);
                stmt.AddValue(1, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GUILD_BANK_EVENTLOG_BY_PLAYER);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_GLYPHS);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_QUESTSTATUS_DAILY);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_QUESTSTATUS_WEEKLY);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_QUESTSTATUS_MONTHLY);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_QUESTSTATUS_SEASONAL);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_TALENT);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_SKILLS);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_STATS);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_VOID_STORAGE_ITEM_BY_CHAR_GUID);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_FISHINGSTEPS);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_FAVORITE_AUCTIONS_BY_CHAR);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_AURA_STORED_LOCATIONS_BY_GUID);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Login.GetPreparedStatement(LoginStatements.DEL_BATTLE_PET_DECLINED_NAME_BY_OWNER);
                stmt.AddValue(0, guid);
                stmt.AddValue(1, Global.WorldMgr.RealmId.Index);
                loginTransaction.Append(stmt);

                stmt = DB.Login.GetPreparedStatement(LoginStatements.DEL_BATTLE_PETS_BY_OWNER);
                stmt.AddValue(0, guid);
                stmt.AddValue(1, Global.WorldMgr.RealmId.Index);
                loginTransaction.Append(stmt);

                Corpse.DeleteFromDB(playerGuid, trans);

                Garrison.DeleteFromDB(guid, trans);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_TRAIT_ENTRIES_BY_CHAR);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_TRAIT_CONFIGS_BY_CHAR);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                Global.CharacterCacheStorage.DeleteCharacterCacheEntry(playerGuid, name);

                break;
            }
            // The character gets unlinked from the account, the name gets freed up and appears as deleted ingame
            case CharDeleteMethod.Unlink:
            {
                stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_DELETE_INFO);
                stmt.AddValue(0, guid);
                trans.Append(stmt);

                Global.CharacterCacheStorage.UpdateCharacterInfoDeleted(playerGuid, true);

                break;
            }
            default:
                Log.Logger.Error("Player:DeleteFromDB: Unsupported delete method: {0}.", charDeleteMethod);

                if (trans.commands.Count > 0)
                    DB.Characters.CommitTransaction(trans);

                return;
        }

        DB.Login.CommitTransaction(loginTransaction);
        DB.Characters.CommitTransaction(trans);

        if (updateRealmChars)
            Global.WorldMgr.UpdateRealmCharCount(accountId);
    }

    public static void DeleteOldCharacters()
    {
        var keepDays = GetDefaultValue("CharDelete.KeepDays", 30);

        if (keepDays == 0)
            return;

        DeleteOldCharacters(keepDays);
    }

    public static void DeleteOldCharacters(int keepDays)
    {
        Log.Logger.Information("Player:DeleteOldChars: Deleting all characters which have been deleted {0} days before...", keepDays);

        var stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_OLD_CHARS);
        stmt.AddValue(0, (uint)(GameTime.CurrentTime - keepDays * Time.DAY));
        var result = DB.Characters.Query(stmt);

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

    public static uint GetZoneIdFromDB(ObjectGuid guid)
    {
        var guidLow = guid.Counter;
        var stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_ZONE);
        stmt.AddValue(0, guidLow);
        var result = DB.Characters.Query(stmt);

        if (result.IsEmpty())
            return 0;

        uint zone = result.Read<ushort>(0);

        if (zone == 0)
        {
            // stored zone is zero, use generic and slow zone detection
            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_POSITION_XYZ);
            stmt.AddValue(0, guidLow);
            result = DB.Characters.Query(stmt);

            if (result.IsEmpty())
                return 0;

            uint map = result.Read<ushort>(0);
            var posx = result.Read<float>(1);
            var posy = result.Read<float>(2);
            var posz = result.Read<float>(3);

            if (!_player.CliDB.MapStorage.ContainsKey(map))
                return 0;

            zone = Global.TerrainMgr.GetZoneId(PhasingHandler.EmptyPhaseShift, map, posx, posy, posz);

            if (zone > 0)
            {
                stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_ZONE);

                stmt.AddValue(0, zone);
                stmt.AddValue(1, guidLow);

                DB.Characters.Execute(stmt);
            }
        }

        return zone;
    }

    public static bool LoadPositionFromDB(out WorldLocation loc, out bool inFlight, ObjectGuid guid)
    {
        var stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_POSITION);
        stmt.AddValue(0, guid.Counter);
        var result = DB.Characters.Query(stmt);

        loc = new WorldLocation();
        inFlight = false;

        if (result.IsEmpty())
            return false;

        loc.X = result.Read<float>(0);
        loc.Y = result.Read<float>(1);
        loc.Z = result.Read<float>(2);
        loc.Orientation = result.Read<float>(3);
        loc.MapId = result.Read<ushort>(4);
        inFlight = !string.IsNullOrEmpty(result.Read<string>(5));

        return true;
    }

    public static void RemovePetitionsAndSigns(ObjectGuid guid)
    {
        Global.PetitionMgr.RemoveSignaturesBySigner(guid);
        Global.PetitionMgr.RemovePetitionsByOwner(guid);
    }

    public static void SaveCustomizations(SQLTransaction trans, ulong guid, List<ChrCustomizationChoice> customizations)
    {
        SavePlayerCustomizations(trans, guid, customizations);
    }

    public static void SavePlayerCustomizations(SQLTransaction trans, ulong guid, List<ChrCustomizationChoice> customizations)
    {
        var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_CUSTOMIZATIONS);
        stmt.AddValue(0, guid);
        trans.Append(stmt);

        foreach (var customization in customizations)
        {
            stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHARACTER_CUSTOMIZATION);
            stmt.AddValue(0, guid);
            stmt.AddValue(1, customization.ChrCustomizationOptionID);
            stmt.AddValue(2, customization.ChrCustomizationChoiceID);
            trans.Append(stmt);
        }
    }

    public static void SavePositionInDB(WorldLocation loc, uint zoneId, ObjectGuid guid, SQLTransaction trans = null)
    {
        var stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHARACTER_POSITION);
        stmt.AddValue(0, loc.X);
        stmt.AddValue(1, loc.Y);
        stmt.AddValue(2, loc.Z);
        stmt.AddValue(3, loc.Orientation);
        stmt.AddValue(4, (ushort)loc.MapId);
        stmt.AddValue(5, zoneId);
        stmt.AddValue(6, guid.Counter);

        DB.Characters.ExecuteOrAppend(trans, stmt);
    }

    private static Item _LoadMailedItem(ObjectGuid playerGuid, Player player, ulong mailId, Mail mail, SQLFields fields, ItemAdditionalLoadInfo addionalData)
    {
        var itemGuid = fields.Read<ulong>(0);
        var itemEntry = fields.Read<uint>(1);

        var proto = Global.ObjectMgr.GetItemTemplate(itemEntry);

        if (proto == null)
        {
            Log.Logger.Error($"Player {(player != null ? player.GetName() : "<unknown>")} ({playerGuid}) has unknown item in mailed items (GUID: {itemGuid} template: {itemEntry}) in mail ({mailId}), deleted.");

            SQLTransaction trans = new();

            var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_INVALID_MAIL_ITEM);
            stmt.AddValue(0, itemGuid);
            trans.Append(stmt);

            Item.DeleteFromDB(trans, itemGuid);
            AzeriteItem.DeleteFromDB(trans, itemGuid);
            AzeriteEmpoweredItem.DeleteFromDB(trans, itemGuid);

            DB.Characters.CommitTransaction(trans);

            return null;
        }

        var item = Item.NewItemOrBag(proto);
        var ownerGuid = fields.Read<ulong>(51) != 0 ? ObjectGuid.Create(HighGuid.Player, fields.Read<ulong>(51)) : ObjectGuid.Empty;

        if (!item.LoadFromDB(itemGuid, ownerGuid, fields, itemEntry))
        {
            Log.Logger.Error($"Player._LoadMailedItems: Item (GUID: {itemGuid}) in mail ({mailId}) doesn't exist, deleted from mail.");

            var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_MAIL_ITEM);
            stmt.AddValue(0, itemGuid);
            DB.Characters.Execute(stmt);

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

    public static bool IsBagPos(ushort pos)
    {
        var bag = (byte)(pos >> 8);
        var slot = (byte)(pos & 255);

        if (bag == InventorySlots.Bag0 && (slot >= InventorySlots.BagStart && slot < InventorySlots.BagEnd))
            return true;

        if (bag == InventorySlots.Bag0 && (slot >= InventorySlots.BankBagStart && slot < InventorySlots.BankBagEnd))
            return true;

        if (bag == InventorySlots.Bag0 && (slot >= InventorySlots.ReagentBagStart && slot < InventorySlots.ReagentBagEnd))
            return true;

        return false;
    }

    public static bool IsBankPos(ushort pos)
    {
        return IsBankPos((byte)(pos >> 8), (byte)(pos & 255));
    }

    public static bool IsBankPos(byte bag, byte slot)
    {
        if (bag == InventorySlots.Bag0 && (slot >= InventorySlots.BankItemStart && slot < InventorySlots.BankItemEnd))
            return true;

        if (bag == InventorySlots.Bag0 && (slot >= InventorySlots.BankBagStart && slot < InventorySlots.BankBagEnd))
            return true;

        if (bag >= InventorySlots.BankBagStart && bag < InventorySlots.BankBagEnd)
            return true;

        if (bag == InventorySlots.Bag0 && (slot >= InventorySlots.ReagentStart && slot < InventorySlots.ReagentEnd))
            return true;

        return false;
    }

    public static bool IsChildEquipmentPos(byte bag, byte slot)
    {
        return bag == InventorySlots.Bag0 && (slot >= InventorySlots.ChildEquipmentStart && slot < InventorySlots.ChildEquipmentEnd);
    }

    public static bool IsChildEquipmentPos(ushort pos)
    {
        return IsChildEquipmentPos((byte)(pos >> 8), (byte)(pos & 255));
    }

    public static bool IsEquipmentPos(ushort pos)
    {
        return IsEquipmentPos((byte)(pos >> 8), (byte)(pos & 255));
    }

    public static bool IsEquipmentPos(byte bag, byte slot)
    {
        if (bag == InventorySlots.Bag0 && (slot < EquipmentSlot.End))
            return true;

        if (bag == InventorySlots.Bag0 && (slot >= ProfessionSlots.Start && slot < ProfessionSlots.End))
            return true;

        if (bag == InventorySlots.Bag0 && (slot >= InventorySlots.BagStart && slot < InventorySlots.BagEnd))
            return true;

        if (bag == InventorySlots.Bag0 && (slot >= InventorySlots.ReagentBagStart && slot < InventorySlots.ReagentBagEnd))
            return true;

        return false;
    }

    public static bool IsInventoryPos(byte bag, byte slot)
    {
        if (bag == InventorySlots.Bag0 && slot == ItemConst.NullSlot)
            return true;

        if (bag == InventorySlots.Bag0 && (slot >= InventorySlots.ItemStart && slot < InventorySlots.ItemEnd))
            return true;

        if (bag >= InventorySlots.BagStart && bag < InventorySlots.BagEnd)
            return true;

        if (bag == InventorySlots.Bag0 && (slot >= InventorySlots.ReagentStart && slot < InventorySlots.ReagentEnd))
            return true;

        if (bag == InventorySlots.Bag0 && (slot >= InventorySlots.ChildEquipmentStart && slot < InventorySlots.ChildEquipmentEnd))
            return true;

        return false;
    }

    public static bool IsReagentBankPos(ushort pos)
    {
        return IsReagentBankPos((byte)(pos >> 8), (byte)(pos & 255));
    }

    public static bool IsReagentBankPos(byte bag, byte slot)
    {
        if (bag == InventorySlots.Bag0 && (slot >= InventorySlots.ReagentStart && slot < InventorySlots.ReagentEnd))
            return true;

        return false;
    }
}