// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Arenas;
using Forged.MapServer.Cache;
using Forged.MapServer.Calendar;
using Forged.MapServer.Chrono;
using Forged.MapServer.Conditions;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.Structs.C;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Objects.Update;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Forged.MapServer.Globals.Caching;
using Forged.MapServer.Guilds;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Character;
using Forged.MapServer.Networking.Packets.Equipment;
using Forged.MapServer.Networking.Packets.Misc;
using Forged.MapServer.Networking.Packets.Reputation;
using Forged.MapServer.Reputation;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IPlayer;
using Forged.MapServer.Server;
using Forged.MapServer.World;
using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
using Game.Common;
using Game.Common.Handlers;
using Microsoft.Extensions.Configuration;
using Serilog;

// ReSharper disable AccessToDisposedClosure
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMember.Local

namespace Forged.MapServer.OpCodeHandlers;

public class CharacterHandler : IWorldSessionHandler
{
    private readonly ArenaTeamManager _arenaTeamManager;
    private readonly CalendarManager _calendarManager;
    private readonly CharacterCache _characterCache;
    private readonly CharacterDatabase _characterDatabase;
    private readonly ClassFactory _classFactory;
    private readonly CliDB _cliDb;
    private readonly CollectionMgr _collectionMgr;
    private readonly ConditionManager _conditionManager;
    private readonly IConfiguration _configuration;
    private readonly DB2Manager _dB2Manager;
    private readonly GuildManager _guildManager;
    private readonly List<ObjectGuid> _legitCharacters = new();
    private readonly LoginDatabase _loginDatabase;
    private readonly ObjectAccessor _objectAccessor;
    private readonly GameObjectManager _objectManager;
    private readonly PlayerComputators _playerComputators;
    private readonly ObjectGuidGeneratorFactory _objectGuidGeneratorFactory;
    private readonly FactionChangeTitleCache _factionChangeTitleCache;
    private readonly ClassAndRaceExpansionRequirementsCache _classAndRaceExpansionRequirementsCache;
    private readonly GraveyardCache _graveyardCache;
    private readonly ScriptManager _scriptManager;
    private readonly WorldSession _session;
    private readonly WorldManager _worldManager;

    public CharacterHandler(WorldSession session, CliDB cliDb, CollectionMgr collectionMgr, IConfiguration configuration,
        CharacterDatabase characterDatabase, ScriptManager scriptManager, LoginDatabase loginDatabase, DB2Manager dB2Manager,
        WorldManager worldManager, GuildManager guildManager, GameObjectManager objectManager, ObjectAccessor objectAccessor, CharacterCache characterCache,
        ArenaTeamManager arenaTeamManager, ClassFactory classFactory, CalendarManager calendarManager, ConditionManager conditionManager, PlayerComputators playerComputators,
        ObjectGuidGeneratorFactory objectGuidGeneratorFactory, FactionChangeTitleCache factionChangeTitleCache, ClassAndRaceExpansionRequirementsCache classAndRaceExpansionRequirementsCache,
        GraveyardCache graveyardCache)
    {
        _session = session;
        _cliDb = cliDb;
        _collectionMgr = collectionMgr;
        _configuration = configuration;
        _characterDatabase = characterDatabase;
        _scriptManager = scriptManager;
        _loginDatabase = loginDatabase;
        _dB2Manager = dB2Manager;
        _worldManager = worldManager;
        _guildManager = guildManager;
        _objectManager = objectManager;
        _objectAccessor = objectAccessor;
        _characterCache = characterCache;
        _arenaTeamManager = arenaTeamManager;
        _classFactory = classFactory;
        _calendarManager = calendarManager;
        _conditionManager = conditionManager;
        _playerComputators = playerComputators;
        _objectGuidGeneratorFactory = objectGuidGeneratorFactory;
        _factionChangeTitleCache = factionChangeTitleCache;
        _classAndRaceExpansionRequirementsCache = classAndRaceExpansionRequirementsCache;
        _graveyardCache = graveyardCache;
    }

    public bool MeetsChrCustomizationReq(ChrCustomizationReqRecord req, Race race, PlayerClass playerClass, bool checkRequiredDependentChoices, List<ChrCustomizationChoice> selectedChoices)
    {
        if (!req.GetFlags().HasFlag(ChrCustomizationReqFlag.HasRequirements))
            return true;

        if (req.ClassMask != 0 && (req.ClassMask & (1 << ((int)playerClass - 1))) == 0)
            return false;

        if (race != Race.None && req.RaceMask != 0 && req.RaceMask != -1 && (req.RaceMask & (int)race) == 0)
            return false;

        if (req.AchievementID != 0 /*&& !HasAchieved(req->AchievementID)*/)
            return false;

        if (req.ItemModifiedAppearanceID != 0 && !_collectionMgr.HasItemAppearance(req.ItemModifiedAppearanceID).PermAppearance)
            return false;

        if (req.QuestID != 0)
        {
            if (_session.Player == null)
                return false;

            if (!_session.Player.IsQuestRewarded((uint)req.QuestID))
                return false;
        }

        if (!checkRequiredDependentChoices)
            return true;

        var requiredChoices = _dB2Manager.GetRequiredCustomizationChoices(req.Id);

        if (requiredChoices == null)
            return true;

        foreach (var key in requiredChoices.Keys)
        {
            var hasRequiredChoiceForOption = false;

            foreach (var requiredChoice in requiredChoices[key])
                if (selectedChoices.Any(choice => choice.ChrCustomizationChoiceID == requiredChoice))
                {
                    hasRequiredChoiceForOption = true;

                    break;
                }

            if (!hasRequiredChoiceForOption)
                return false;
        }

        return true;
    }

    public bool ValidateAppearance(Race race, PlayerClass playerClass, Gender gender, List<ChrCustomizationChoice> customizations)
    {
        var options = _dB2Manager.GetCustomiztionOptions(race, gender);

        if (options.Empty())
            return false;

        uint previousOption = 0;

        foreach (var playerChoice in customizations)
        {
            // check uniqueness of options
            if (playerChoice.ChrCustomizationOptionID == previousOption)
                return false;

            previousOption = playerChoice.ChrCustomizationOptionID;

            // check if we can use this option
            var customizationOptionData = options.Find(option => option.Id == playerChoice.ChrCustomizationOptionID);

            // option not found for race/gender combination
            if (customizationOptionData == null)
                return false;

            var req = _cliDb.ChrCustomizationReqStorage.LookupByKey((uint)customizationOptionData.ChrCustomizationReqID);

            if (req != null)
                if (!MeetsChrCustomizationReq(req, race, playerClass, false, customizations))
                    return false;

            var choicesForOption = _dB2Manager.GetCustomiztionChoices(playerChoice.ChrCustomizationOptionID);

            if (choicesForOption.Empty())
                return false;

            var customizationChoiceData = choicesForOption.Find(choice => choice.Id == playerChoice.ChrCustomizationChoiceID);

            // choice not found for option
            if (customizationChoiceData == null)
                return false;

            var reqEntry = _cliDb.ChrCustomizationReqStorage.LookupByKey(customizationChoiceData.ChrCustomizationReqID);

            if (reqEntry != null && !MeetsChrCustomizationReq(reqEntry, race, playerClass, true, customizations))
                return false;
        }

        return true;
    }

    [WorldPacketHandler(ClientOpcodes.AlterAppearance)]
    private void HandleAlterAppearance(AlterApperance packet)
    {
        if (packet.CustomizedChrModelID != 0)
        {
            if (!_cliDb.ConditionalChrModelStorage.TryGetValue((uint)packet.CustomizedChrModelID, out var conditionalChrModel))
            {
                return;
            }
            
            if (_cliDb.ChrCustomizationReqStorage.TryGetValue((uint)conditionalChrModel.ChrCustomizationReqID, out var req)
                && !MeetsChrCustomizationReq(req, _session.Player.Race, _session.Player.Class, false, packet.Customizations))
            {
                return;
            }
            
            if (_cliDb.PlayerConditionStorage.TryGetValue((uint)conditionalChrModel.PlayerConditionID, out var condition)
                && !_conditionManager.IsPlayerMeetingCondition(_session.Player, condition))
            {
                return;
            }
        }


        if (!ValidateAppearance(_session.Player.Race, _session.Player.Class, (Gender)packet.NewSex, packet.Customizations))
            return;

        var go = _session.Player.Location.FindNearestGameObjectOfType(GameObjectTypes.BarberChair, 5.0f);

        if (go == null)
        {
            _session.SendPacket(new BarberShopResult(BarberShopResult.ResultEnum.NotOnChair));

            return;
        }

        if (_session.Player.StandState != (UnitStandStateType)((int)UnitStandStateType.SitLowChair + go.Template.BarberChair.chairheight))
        {
            _session.SendPacket(new BarberShopResult(BarberShopResult.ResultEnum.NotOnChair));

            return;
        }

        var cost = _session.Player.GetBarberShopCost(packet.Customizations);

        if (!_session.Player.HasEnoughMoney(cost))
        {
            _session.SendPacket(new BarberShopResult(BarberShopResult.ResultEnum.NoMoney));

            return;
        }

        _session.SendPacket(new BarberShopResult(BarberShopResult.ResultEnum.Success));

        _session.Player.ModifyMoney(-cost);
        _session.Player.UpdateCriteria(CriteriaType.MoneySpentAtBarberShop, (ulong)cost);

        if (_session.Player.NativeGender != (Gender)packet.NewSex)
        {
            _session.Player.NativeGender = (Gender)packet.NewSex;
            _session.Player.InitDisplayIds();
            _session.Player.RestoreDisplayId();
        }

        _session.Player.SetCustomizations(packet.Customizations);

        _session.Player.UpdateCriteria(CriteriaType.GotHaircut, 1);

        _session.Player.SetStandState(UnitStandStateType.Stand);

        _characterCache.UpdateCharacterGender(_session.Player.GUID, packet.NewSex);
    }

    [WorldPacketHandler(ClientOpcodes.CreateCharacter, Status = SessionStatus.Authed)]
    private void HandleCharCreate(CreateCharacter charCreate)
    {
        if (!_session.HasPermission(RBACPermissions.SkipCheckCharacterCreationTeammask))
        {
            var mask = _configuration.GetDefaultValue("CharacterCreating:Disabled:FactionMask", 0u);

            if (mask != 0)
            {
                var disabled = false;

                var team = Player.TeamIdForRace(charCreate.CreateInfo.RaceId, _cliDb);

                switch (team)
                {
                    case TeamIds.Alliance:
                        disabled = Convert.ToBoolean(mask & (1 << 0));

                        break;

                    case TeamIds.Horde:
                        disabled = Convert.ToBoolean(mask & (1 << 1));

                        break;

                    case TeamIds.Neutral:
                        disabled = Convert.ToBoolean(mask & (1 << 2));

                        break;
                }

                if (disabled)
                {
                    SendCharCreate(ResponseCodes.CharCreateDisabled);

                    return;
                }
            }
        }

        var classEntry = _cliDb.ChrClassesStorage.LookupByKey((uint)charCreate.CreateInfo.ClassId);

        if (classEntry == null)
        {
            Log.Logger.Error("Class ({0}) not found in DBC while creating new char for account (ID: {1}): wrong DBC files or cheater?", charCreate.CreateInfo.ClassId, _session.AccountId);
            SendCharCreate(ResponseCodes.CharCreateFailed);

            return;
        }

        var raceEntry = _cliDb.ChrRacesStorage.LookupByKey((uint)charCreate.CreateInfo.RaceId);

        if (raceEntry == null)
        {
            Log.Logger.Error("Race ({0}) not found in DBC while creating new char for account (ID: {1}): wrong DBC files or cheater?", charCreate.CreateInfo.RaceId, _session.AccountId);
            SendCharCreate(ResponseCodes.CharCreateFailed);

            return;
        }

        if (_configuration.GetDefaultValue("character.EnforceRaceAndClassExpansions", true))
        {
            // prevent character creating Expansion race without Expansion account
            var raceExpansionRequirement = _classAndRaceExpansionRequirementsCache.GetRaceUnlockRequirement(charCreate.CreateInfo.RaceId);

            if (raceExpansionRequirement == null)
            {
                Log.Logger.Error($"Account {_session.AccountId} tried to create character with unavailable race {charCreate.CreateInfo.RaceId}");
                SendCharCreate(ResponseCodes.CharCreateFailed);

                return;
            }

            if (raceExpansionRequirement.Expansion > (byte)_session.AccountExpansion)
            {
                Log.Logger.Error($"Expansion {_session.AccountExpansion} account:[{_session.AccountId}] tried to Create character with expansion {raceExpansionRequirement.Expansion} race ({charCreate.CreateInfo.RaceId})");
                SendCharCreate(ResponseCodes.CharCreateExpansion);

                return;
            }

            //if (raceExpansionRequirement.AchievementId && !)
            //{
            //    TC_LOG_ERROR("entities.player.cheat", "Expansion %u account:[%d] tried to Create character without achievement %u race (%u)",
            //        GetAccountExpansion(), Get_session.AccountId(), raceExpansionRequirement.AchievementId, charCreate.CreateInfo.Race);
            //    SendCharCreate(CHAR_CREATE_ALLIED_RACE_ACHIEVEMENT);
            //    return;
            //}

            // prevent character creating Expansion race without Expansion account
            var raceClassExpansionRequirement = _objectManager.ClassAndRaceExpansionRequirementsCache.GetClassExpansionRequirement(charCreate.CreateInfo.RaceId, charCreate.CreateInfo.ClassId);

            if (raceClassExpansionRequirement != null)
            {
                if (raceClassExpansionRequirement.ActiveExpansionLevel > (byte)_session.Expansion || raceClassExpansionRequirement.AccountExpansionLevel > (byte)_session.AccountExpansion)
                {
                    Log.Logger.Error(
                                $"Account:[{_session.AccountId}] tried to create character with race/class {charCreate.CreateInfo.RaceId}/{charCreate.CreateInfo.ClassId} without required expansion " +
                                $"(had {_session.Expansion}/{_session.AccountExpansion}, required {raceClassExpansionRequirement.ActiveExpansionLevel}/{raceClassExpansionRequirement.AccountExpansionLevel})");

                    SendCharCreate(ResponseCodes.CharCreateExpansionClass);

                    return;
                }
            }
            else
            {
                var classExpansionRequirement = _objectManager.ClassAndRaceExpansionRequirementsCache.GetClassExpansionRequirementFallback((byte)charCreate.CreateInfo.ClassId);

                if (classExpansionRequirement != null)
                {
                    if (classExpansionRequirement.MinActiveExpansionLevel > (byte)_session.Expansion || classExpansionRequirement.AccountExpansionLevel > (byte)_session.AccountExpansion)
                    {
                        Log.Logger.Error(
                                    $"Account:[{_session.AccountId}] tried to create character with race/class {charCreate.CreateInfo.RaceId}/{charCreate.CreateInfo.ClassId} without required expansion " +
                                    $"(had {_session.Expansion}/{_session.AccountExpansion}, required {classExpansionRequirement.ActiveExpansionLevel}/{classExpansionRequirement.AccountExpansionLevel})");

                        SendCharCreate(ResponseCodes.CharCreateExpansionClass);

                        return;
                    }
                }
                else
                {
                    Log.Logger.Error($"Expansion {_session.AccountExpansion} account:[{_session.AccountId}] tried to Create character for race/class combination that is missing requirements in db ({charCreate.CreateInfo.RaceId}/{charCreate.CreateInfo.ClassId})");
                }
            }
        }

        if (!_session.HasPermission(RBACPermissions.SkipCheckCharacterCreationRacemask))
        {
            if (raceEntry.GetFlags().HasFlag(ChrRacesFlag.NPCOnly))
            {
                Log.Logger.Error($"Race ({charCreate.CreateInfo.RaceId}) was not playable but requested while creating new char for account (ID: {_session.AccountId}): wrong DBC files or cheater?");
                SendCharCreate(ResponseCodes.CharCreateDisabled);

                return;
            }

            var raceMaskDisabled = _configuration.GetDefaultValue("CharacterCreating:Disabled:RaceMask", 0u);

            if (Convert.ToBoolean((ulong)SharedConst.GetMaskForRace(charCreate.CreateInfo.RaceId) & raceMaskDisabled))
            {
                SendCharCreate(ResponseCodes.CharCreateDisabled);

                return;
            }
        }

        if (!_session.HasPermission(RBACPermissions.SkipCheckCharacterCreationClassmask))
        {
            var classMaskDisabled = _configuration.GetDefaultValue("CharacterCreating:Disabled:ClassMask", 0u);

            if (Convert.ToBoolean((1 << ((int)charCreate.CreateInfo.ClassId - 1)) & classMaskDisabled))
            {
                SendCharCreate(ResponseCodes.CharCreateDisabled);

                return;
            }
        }

        // prevent character creating with invalid name
        if (!_objectManager.NormalizePlayerName(ref charCreate.CreateInfo.Name))
        {
            Log.Logger.Error("Account:[{0}] but tried to Create character with empty [name] ", _session.AccountId);
            SendCharCreate(ResponseCodes.CharNameNoName);

            return;
        }

        // check name limitations
        var res = _objectManager.CheckPlayerName(charCreate.CreateInfo.Name, _session.SessionDbcLocale, true);

        if (res != ResponseCodes.CharNameSuccess)
        {
            SendCharCreate(res);

            return;
        }

        if (!_session.HasPermission(RBACPermissions.SkipCheckCharacterCreationReservedname) && _objectManager.IsReservedName(charCreate.CreateInfo.Name))
        {
            SendCharCreate(ResponseCodes.CharNameReserved);

            return;
        }

        var createInfo = charCreate.CreateInfo;
        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHECK_NAME);
        stmt.AddValue(0, charCreate.CreateInfo.Name);

        _session.QueryProcessor.AddCallback(_characterDatabase.AsyncQuery(stmt)
                                    .WithChainingCallback((queryCallback, result) =>
                                    {
                                        if (!result.IsEmpty())
                                        {
                                            SendCharCreate(ResponseCodes.CharCreateNameInUse);

                                            return;
                                        }

                                        stmt = _loginDatabase.GetPreparedStatement(LoginStatements.SEL_SUM_REALM_CHARACTERS);
                                        stmt.AddValue(0, _session.AccountId);
                                        queryCallback.SetNextQuery(_loginDatabase.AsyncQuery(stmt));
                                    })
                                    .WithChainingCallback((queryCallback, result) =>
                                    {
                                        ulong acctCharCount = 0;

                                        if (!result.IsEmpty())
                                            acctCharCount = result.Read<ulong>(0);

                                        if (acctCharCount >= _configuration.GetDefaultValue("CharactersPerAccount", 60ul))
                                        {
                                            SendCharCreate(ResponseCodes.CharCreateAccountLimit);

                                            return;
                                        }

                                        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_SUM_CHARS);
                                        stmt.AddValue(0, _session.AccountId);
                                        queryCallback.SetNextQuery(_characterDatabase.AsyncQuery(stmt));
                                    })
                                    .WithChainingCallback((queryCallback, result) =>
                                    {
                                        if (!result.IsEmpty())
                                        {
                                            createInfo.CharCount = (byte)result.Read<ulong>(0); // SQL's COUNT() returns uint64 but it will always be less than uint8.Max

                                            if (createInfo.CharCount >= _configuration.GetDefaultValue("CharactersPerRealm", 0ul))
                                            {
                                                SendCharCreate(ResponseCodes.CharCreateServerLimit);

                                                return;
                                            }
                                        }

                                        var demonHunterReqLevel = _configuration.GetDefaultValue("CharacterCreating:MinLevelForDemonHunter", 0u);
                                        var hasDemonHunterReqLevel = demonHunterReqLevel == 0;
                                        var evokerReqLevel = _configuration.GetDefaultValue("CharacterCreating:MinLevelForEvoker", 0u);
                                        var hasEvokerReqLevel = (evokerReqLevel == 0);
                                        var allowTwoSideAccounts = !_worldManager.IsPvPRealm || _session.HasPermission(RBACPermissions.TwoSideCharacterCreation);
                                        var skipCinematics = _configuration.GetDefaultValue("SkipCinematics", 2u);
                                        var checkClassLevelReqs = createInfo.ClassId is PlayerClass.DemonHunter or PlayerClass.Evoker && !_session.HasPermission(RBACPermissions.SkipCheckCharacterCreationDemonHunter);
                                        var evokerLimit = _configuration.GetDefaultValue("CharacterCreating:EvokersPerRealm", 1u);
                                        var hasEvokerLimit = evokerLimit != 0;

                                        void FinalizeCharacterCreation(SQLResult result1)
                                        {
                                            var haveSameRace = false;

                                            if (result1 != null && !result1.IsEmpty() && result.GetFieldCount() >= 3)
                                            {
                                                var team = Player.TeamForRace(createInfo.RaceId, _cliDb);
                                                var accRace = result1.Read<byte>(1);
                                                var accClass = result1.Read<byte>(2);

                                                if (checkClassLevelReqs)
                                                {
                                                    if (!hasDemonHunterReqLevel)
                                                    {
                                                        var accLevel = result1.Read<byte>(0);

                                                        if (accLevel >= demonHunterReqLevel)
                                                            hasDemonHunterReqLevel = true;
                                                    }

                                                    if (!hasEvokerReqLevel)
                                                    {
                                                        var accLevel = result1.Read<byte>(0);

                                                        if (accLevel >= evokerReqLevel)
                                                            hasEvokerReqLevel = true;
                                                    }
                                                }

                                                if (accClass == (byte)PlayerClass.Evoker)
                                                    --evokerLimit;

                                                if (!allowTwoSideAccounts)
                                                {
                                                    TeamFaction accTeam = 0;

                                                    if (accRace > 0)
                                                        accTeam = Player.TeamForRace((Race)accRace, _cliDb);

                                                    if (accTeam != team)
                                                    {
                                                        SendCharCreate(ResponseCodes.CharCreatePvpTeamsViolation);

                                                        return;
                                                    }
                                                }

                                                // search same race for cinematic or same class if need
                                                // @todo check if cinematic already shown? (already logged in?; cinematic field)
                                                while ((skipCinematics == 1 && !haveSameRace) || createInfo.ClassId is PlayerClass.DemonHunter or PlayerClass.Evoker)
                                                {
                                                    if (!result1.NextRow())
                                                        break;

                                                    accRace = result1.Read<byte>(1);
                                                    accClass = result1.Read<byte>(2);

                                                    if (!haveSameRace)
                                                        haveSameRace = createInfo.RaceId == (Race)accRace;

                                                    if (checkClassLevelReqs)
                                                    {
                                                        if (!hasDemonHunterReqLevel)
                                                        {
                                                            var accLevel = result1.Read<byte>(0);

                                                            if (accLevel >= demonHunterReqLevel)
                                                                hasDemonHunterReqLevel = true;
                                                        }

                                                        if (!hasEvokerReqLevel)
                                                        {
                                                            var accLevel = result1.Read<byte>(0);

                                                            if (accLevel >= evokerReqLevel)
                                                                hasEvokerReqLevel = true;
                                                        }
                                                    }

                                                    if (accClass == (byte)PlayerClass.Evoker)
                                                        --evokerLimit;
                                                }
                                            }

                                            if (checkClassLevelReqs)
                                            {
                                                if (!hasDemonHunterReqLevel)
                                                {
                                                    SendCharCreate(ResponseCodes.CharCreateNewPlayer);

                                                    return;
                                                }

                                                if (!hasEvokerReqLevel)
                                                {
                                                    SendCharCreate(ResponseCodes.CharCreateDracthyrLevelRequirement);

                                                    return;
                                                }
                                            }

                                            if (createInfo.ClassId == PlayerClass.Evoker && hasEvokerLimit && evokerLimit < 1)
                                            {
                                                SendCharCreate(ResponseCodes.CharCreateNewPlayer);

                                                return;
                                            }

                                            // Check name uniqueness in the same step as saving to database
                                            if (_characterCache.GetCharacterCacheByName(createInfo.Name) != null)
                                            {
                                                SendCharCreate(ResponseCodes.CharCreateDracthyrDuplicate);

                                                return;
                                            }

                                            Player newChar = new(_session, _classFactory);
                                            newChar.MotionMaster.Initialize();

                                            if (!newChar.Create(_objectGuidGeneratorFactory.GetGenerator(HighGuid.Player).Generate(), createInfo))
                                            {
                                                // Player not create (race/class/etc problem?)
                                                newChar.CleanupsBeforeDelete();
                                                newChar.Dispose();
                                                SendCharCreate(ResponseCodes.CharCreateError);

                                                return;
                                            }

                                            if ((haveSameRace && skipCinematics == 1) || skipCinematics == 2)
                                                newChar.Cinematic = 1; // not show intro

                                            newChar.LoginFlags = AtLoginFlags.FirstLogin; // First login

                                            SQLTransaction characterTransaction = new();
                                            SQLTransaction loginTransaction = new();

                                            // Player created, save it now
                                            newChar.SaveToDB(loginTransaction, characterTransaction, true);
                                            createInfo.CharCount += 1;

                                            stmt = _loginDatabase.GetPreparedStatement(LoginStatements.REP_REALM_CHARACTERS);
                                            stmt.AddValue(0, createInfo.CharCount);
                                            stmt.AddValue(1, _session.AccountId);
                                            stmt.AddValue(2, WorldManager.Realm.Id.Index);
                                            loginTransaction.Append(stmt);

                                            _loginDatabase.CommitTransaction(loginTransaction);

                                            _session.AddTransactionCallback(_characterDatabase.AsyncCommitTransaction(characterTransaction))
                                                .AfterComplete(success =>
                                                {
                                                    if (success)
                                                    {
                                                        Log.Logger.Information("Account: {0} (IP: {1}) Create Character: {2} {3}", _session.AccountId, _session.RemoteAddress, createInfo.Name, newChar.GUID.ToString());
                                                        _scriptManager.ForEach<IPlayerOnCreate>(newChar.Class, p => p.OnCreate(newChar));
                                                        _characterCache.AddCharacterCacheEntry(newChar.GUID, _session.AccountId, newChar.GetName(), (byte)newChar.NativeGender, (byte)newChar.Race, (byte)newChar.Class, (byte)newChar.Level, false);

                                                        SendCharCreate(ResponseCodes.CharCreateSuccess, newChar.GUID);
                                                    }
                                                    else
                                                    {
                                                        SendCharCreate(ResponseCodes.CharCreateError);
                                                    }

                                                    newChar.CleanupsBeforeDelete();
                                                    newChar.Dispose();
                                                });
                                        }

                                        if (!allowTwoSideAccounts || skipCinematics == 1 || createInfo.ClassId == PlayerClass.DemonHunter)
                                        {
                                            FinalizeCharacterCreation(new SQLResult());

                                            return;
                                        }

                                        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHAR_CREATE_INFO);
                                        stmt.AddValue(0, _session.AccountId);
                                        stmt.AddValue(1, createInfo.ClassId is PlayerClass.DemonHunter or PlayerClass.Evoker ? 1200 : 1); // 200 (max chars per realm) + 1000 (max deleted chars per realm)
                                        queryCallback.WithCallback(FinalizeCharacterCreation).SetNextQuery(_characterDatabase.AsyncQuery(stmt));
                                    }));
    }

    [WorldPacketHandler(ClientOpcodes.CharCustomize, Status = SessionStatus.Authed)]
    private void HandleCharCustomize(CharCustomize packet)
    {
        if (!_legitCharacters.Contains(packet.CustomizeInfo.CharGUID))
        {
            Log.Logger.Error(
                        "Account {0}, IP: {1} tried to customise {2}, but it does not belong to their account!",
                        _session.AccountId,
                        _session.RemoteAddress,
                        packet.CustomizeInfo.CharGUID.ToString());

            _session.KickPlayer("WorldSession::HandleCharCustomize Trying to customise character of another account");

            return;
        }

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHAR_CUSTOMIZE_INFO);
        stmt.AddValue(0, packet.CustomizeInfo.CharGUID.Counter);

        _session.QueryProcessor.AddCallback(_characterDatabase.AsyncQuery(stmt).WithCallback(HandleCharCustomizeCallback, packet.CustomizeInfo));
    }

    private void HandleCharCustomizeCallback(CharCustomizeInfo customizeInfo, SQLResult result)
    {
        if (result.IsEmpty())
        {
            SendCharCustomize(ResponseCodes.CharCreateError, customizeInfo);

            return;
        }

        var oldName = result.Read<string>(0);
        var plrRace = (Race)result.Read<byte>(1);
        var plrClass = (PlayerClass)result.Read<byte>(2);
        var plrGender = (Gender)result.Read<byte>(3);
        var atLoginFlags = (AtLoginFlags)result.Read<ushort>(4);

        if (!ValidateAppearance(plrRace, plrClass, plrGender, customizeInfo.Customizations))
        {
            SendCharCustomize(ResponseCodes.CharCreateError, customizeInfo);

            return;
        }

        if (!atLoginFlags.HasAnyFlag(AtLoginFlags.Customize))
        {
            SendCharCustomize(ResponseCodes.CharCreateError, customizeInfo);

            return;
        }

        // prevent character rename
        if (_configuration.GetDefaultValue("PreventRenameCharacterOnCustomization", false) && (customizeInfo.CharName != oldName))
        {
            SendCharCustomize(ResponseCodes.CharNameFailure, customizeInfo);

            return;
        }

        atLoginFlags &= ~AtLoginFlags.Customize;

        // prevent character rename to invalid name
        if (!_objectManager.NormalizePlayerName(ref customizeInfo.CharName))
        {
            SendCharCustomize(ResponseCodes.CharNameNoName, customizeInfo);

            return;
        }

        var res = _objectManager.CheckPlayerName(customizeInfo.CharName, _session.SessionDbcLocale, true);

        if (res != ResponseCodes.CharNameSuccess)
        {
            SendCharCustomize(res, customizeInfo);

            return;
        }

        // check name limitations
        if (!_session.HasPermission(RBACPermissions.SkipCheckCharacterCreationReservedname) && _objectManager.IsReservedName(customizeInfo.CharName))
        {
            SendCharCustomize(ResponseCodes.CharNameReserved, customizeInfo);

            return;
        }

        // character with this name already exist
        // @todo: make async
        var newGuid = _characterCache.GetCharacterGuidByName(customizeInfo.CharName);

        if (!newGuid.IsEmpty)
            if (newGuid != customizeInfo.CharGUID)
            {
                SendCharCustomize(ResponseCodes.CharCreateNameInUse, customizeInfo);

                return;
            }

        SQLTransaction trans = new();
        var lowGuid = customizeInfo.CharGUID.Counter;

        // Customize
        _playerComputators.SaveCustomizations(trans, lowGuid, customizeInfo.Customizations);

        // Name Change and update atLogin flags
        {
            var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_CHAR_NAME_AT_LOGIN);
            stmt.AddValue(0, customizeInfo.CharName);
            stmt.AddValue(1, (ushort)atLoginFlags);
            stmt.AddValue(2, lowGuid);
            trans.Append(stmt);

            stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_DECLINED_NAME);
            stmt.AddValue(0, lowGuid);

            trans.Append(stmt);
        }

        _characterDatabase.CommitTransaction(trans);

        _characterCache.UpdateCharacterData(customizeInfo.CharGUID, customizeInfo.CharName, (byte)customizeInfo.SexID);

        SendCharCustomize(ResponseCodes.Success, customizeInfo);

        Log.Logger.Information(
                    "Account: {0} (IP: {1}), Character[{2}] ({3}) Customized to: {4}",
                    _session.AccountId,
                    _session.RemoteAddress,
                    oldName,
                    customizeInfo.CharGUID.ToString(),
                    customizeInfo.CharName);
    }

    [WorldPacketHandler(ClientOpcodes.CharDelete, Status = SessionStatus.Authed)]
    private void HandleCharDelete(CharDelete charDelete)
    {
        // Initiating
        var initAccountId = _session.AccountId;

        // can't delete loaded character
        if (_objectAccessor.FindPlayer(charDelete.Guid) == null)
        {
            _scriptManager.ForEach<IPlayerOnFailedDelete>(p => p.OnFailedDelete(charDelete.Guid, initAccountId));

            return;
        }

        // is guild leader
        if (_guildManager.GetGuildByLeader(charDelete.Guid) != null)
        {
            _scriptManager.ForEach<IPlayerOnFailedDelete>(p => p.OnFailedDelete(charDelete.Guid, initAccountId));
            SendCharDelete(ResponseCodes.CharDeleteFailedGuildLeader);

            return;
        }

        // is arena team captain
        if (_arenaTeamManager.GetArenaTeamByCaptain(charDelete.Guid) != null)
        {
            _scriptManager.ForEach<IPlayerOnFailedDelete>(p => p.OnFailedDelete(charDelete.Guid, initAccountId));
            SendCharDelete(ResponseCodes.CharDeleteFailedArenaCaptain);

            return;
        }

        var characterInfo = _characterCache.GetCharacterCacheByGuid(charDelete.Guid);

        if (characterInfo == null)
        {
            _scriptManager.ForEach<IPlayerOnFailedDelete>(p => p.OnFailedDelete(charDelete.Guid, initAccountId));

            return;
        }

        var accountId = characterInfo.AccountId;
        var name = characterInfo.Name;
        var level = characterInfo.Level;

        // prevent deleting other players' characters using cheating tools
        if (accountId != _session.AccountId)
        {
            _scriptManager.ForEach<IPlayerOnFailedDelete>(p => p.OnFailedDelete(charDelete.Guid, initAccountId));

            return;
        }

        var ipStr = _session.RemoteAddress;
        Log.Logger.Information("Account: {0}, IP: {1} deleted character: {2}, {3}, Level: {4}", accountId, ipStr, name, charDelete.Guid.ToString(), level);

        // To prevent hook failure, place hook before removing reference from DB
        _scriptManager.ForEach<IPlayerOnDelete>(p => p.OnDelete(charDelete.Guid, initAccountId)); // To prevent race conditioning, but as it also makes sense, we hand the accountId over for successful delete.

        // Shouldn't interfere with character deletion though

        _calendarManager.RemoveAllPlayerEventsAndInvites(charDelete.Guid);
        _playerComputators.DeleteFromDB(charDelete.Guid, accountId);

        SendCharDelete(ResponseCodes.CharDeleteSuccess);
    }

    [WorldPacketHandler(ClientOpcodes.EnumCharacters, Status = SessionStatus.Authed)]
    private void HandleCharEnum(EnumCharacters charEnum)
    {
        if (charEnum == null)
            return;

        // remove expired bans
        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_EXPIRED_BANS);
        _characterDatabase.Execute(stmt);

        // get all the data necessary for loading all characters (along with their pets) on the account
        EnumCharactersQueryHolder holder = new(_characterDatabase);

        if (!holder.Initialize(_session.AccountId, _configuration.GetDefaultValue("DeclinedNames", false), false))
        {
            HandleCharEnum(holder);

            return;
        }

        _session.AddQueryHolderCallback(_characterDatabase.DelayQueryHolder(holder)).AfterComplete(result => HandleCharEnum((EnumCharactersQueryHolder)result));
    }

    private void HandleCharEnum(EnumCharactersQueryHolder holder)
    {
        EnumCharactersResult charResult = new()
        {
            Success = true,
            IsDeletedCharacters = holder.IsDeletedCharacters,
            DisabledClassesMask = _configuration.GetDefaultValue("CharacterCreating:Disabled:ClassesMask", 0u),
        };

        if (!charResult.IsDeletedCharacters)
            _legitCharacters.Clear();

        MultiMap<ulong, ChrCustomizationChoice> customizations = new();
        var customizationsResult = holder.GetResult(EnumCharacterQueryLoad.Customizations);

        if (!customizationsResult.IsEmpty())
            do
            {
                ChrCustomizationChoice choice = new()
                {
                    ChrCustomizationOptionID = customizationsResult.Read<uint>(1),
                    ChrCustomizationChoiceID = customizationsResult.Read<uint>(2)
                };
                customizations.Add(customizationsResult.Read<ulong>(0), choice);
            } while (customizationsResult.NextRow());

        var result = holder.GetResult(EnumCharacterQueryLoad.Characters);

        if (!result.IsEmpty())
            do
            {
                EnumCharactersResult.CharacterInfo charInfo = new(result.GetFields(), _configuration, _objectManager, _dB2Manager);

                var customizationsForChar = customizations.LookupByKey(charInfo.Guid.Counter);

                if (!customizationsForChar.Empty())
                    charInfo.Customizations = new Array<ChrCustomizationChoice>(customizationsForChar.ToArray());

                Log.Logger.Debug("Loading Character {0} from account {1}.", charInfo.Guid.ToString(), _session.AccountId);

                if (!charResult.IsDeletedCharacters)
                {
                    if (!ValidateAppearance((Race)charInfo.RaceId, charInfo.ClassId, (Gender)charInfo.SexId, charInfo.Customizations))
                    {
                        Log.Logger.Error("Player {0} has wrong Appearance values (Hair/Skin/Color), forcing recustomize", charInfo.Guid.ToString());

                        charInfo.Customizations.Clear();

                        if (charInfo.Flags2 != CharacterCustomizeFlags.Customize)
                        {
                            var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_ADD_AT_LOGIN_FLAG);
                            stmt.AddValue(0, (ushort)AtLoginFlags.Customize);
                            stmt.AddValue(1, charInfo.Guid.Counter);
                            _characterDatabase.Execute(stmt);
                            charInfo.Flags2 = CharacterCustomizeFlags.Customize;
                        }
                    }

                    // Do not allow locked characters to login
                    if (!charInfo.Flags.HasAnyFlag(CharacterFlags.CharacterLockedForTransfer | CharacterFlags.LockedByBilling))
                        _legitCharacters.Add(charInfo.Guid);
                }

                if (!_characterCache.HasCharacterCacheEntry(charInfo.Guid)) // This can happen if characters are inserted into the database manually. Core hasn't loaded name data yet.
                    _characterCache.AddCharacterCacheEntry(charInfo.Guid, _session.AccountId, charInfo.Name, charInfo.SexId, charInfo.RaceId, (byte)charInfo.ClassId, charInfo.ExperienceLevel, false);

                charResult.MaxCharacterLevel = Math.Max(charResult.MaxCharacterLevel, charInfo.ExperienceLevel);

                charResult.Characters.Add(charInfo);
            } while (result.NextRow() && charResult.Characters.Count < 200);

        charResult.IsAlliedRacesCreationAllowed = _session.CanAccessAlliedRaces();

        foreach (var requirement in _classAndRaceExpansionRequirementsCache.RaceUnlockRequirements)
        {
            EnumCharactersResult.RaceUnlock raceUnlock = new()
            {
                RaceID = requirement.Key,
                HasExpansion = !_configuration.GetDefaultValue("character:EnforceRaceAndClassExpansions", true) || (byte)_session.AccountExpansion >= requirement.Value.Expansion,
                HasAchievement = (_configuration.GetDefaultValue("CharacterCreating:DisableAlliedRaceAchievementRequirement", true) || requirement.Value.AchievementId == 0)
            };

            charResult.RaceUnlockData.Add(raceUnlock);
        }

        _session.SendPacket(charResult);
    }

    [WorldPacketHandler(ClientOpcodes.CharRaceOrFactionChange, Status = SessionStatus.Authed)]
    private void HandleCharRaceOrFactionChange(CharRaceOrFactionChange packet)
    {
        if (!_legitCharacters.Contains(packet.RaceOrFactionChangeInfo.Guid))
        {
            Log.Logger.Error(
                        "Account {0}, IP: {1} tried to factionchange character {2}, but it does not belong to their account!",
                        _session.AccountId,
                        _session.RemoteAddress,
                        packet.RaceOrFactionChangeInfo.Guid.ToString());

            _session.KickPlayer("WorldSession::HandleCharFactionOrRaceChange Trying to change faction of character of another account");

            return;
        }

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHAR_RACE_OR_FACTION_CHANGE_INFOS);
        stmt.AddValue(0, packet.RaceOrFactionChangeInfo.Guid.Counter);

        _session.QueryProcessor.AddCallback(_characterDatabase.AsyncQuery(stmt).WithCallback(HandleCharRaceOrFactionChangeCallback, packet.RaceOrFactionChangeInfo));
    }

    private void HandleCharRaceOrFactionChangeCallback(CharRaceOrFactionChangeInfo factionChangeInfo, SQLResult result)
    {
        if (result.IsEmpty())
        {
            SendCharFactionChange(ResponseCodes.CharCreateError, factionChangeInfo);

            return;
        }

        // get the players old (at this moment current) race
        var characterInfo = _characterCache.GetCharacterCacheByGuid(factionChangeInfo.Guid);

        if (characterInfo == null)
        {
            SendCharFactionChange(ResponseCodes.CharCreateError, factionChangeInfo);

            return;
        }

        var oldName = characterInfo.Name;
        var oldRace = characterInfo.RaceId;
        var playerClass = characterInfo.ClassId;
        var level = characterInfo.Level;

        if (_objectManager.PlayerInfoCache.GetPlayerInfo(factionChangeInfo.RaceID, playerClass) == null)
        {
            SendCharFactionChange(ResponseCodes.CharCreateError, factionChangeInfo);

            return;
        }

        var atLoginFlags = (AtLoginFlags)result.Read<ushort>(0);
        var knownTitlesStr = result.Read<string>(1);

        var usedLoginFlag = (factionChangeInfo.FactionChange ? AtLoginFlags.ChangeFaction : AtLoginFlags.ChangeRace);

        if (!atLoginFlags.HasAnyFlag(usedLoginFlag))
        {
            SendCharFactionChange(ResponseCodes.CharCreateError, factionChangeInfo);

            return;
        }

        var newTeamId = Player.TeamIdForRace(factionChangeInfo.RaceID, _cliDb);

        if (newTeamId == TeamIds.Neutral)
        {
            SendCharFactionChange(ResponseCodes.CharCreateRestrictedRaceclass, factionChangeInfo);

            return;
        }

        if (factionChangeInfo.FactionChange == (Player.TeamIdForRace(oldRace, _cliDb) == newTeamId))
        {
            SendCharFactionChange(factionChangeInfo.FactionChange ? ResponseCodes.CharCreateCharacterSwapFaction : ResponseCodes.CharCreateCharacterRaceOnly, factionChangeInfo);

            return;
        }

        if (!_session.HasPermission(RBACPermissions.SkipCheckCharacterCreationRacemask))
        {
            var raceMaskDisabled = _configuration.GetDefaultValue("CharacterCreating:Disabled:RaceMask", 0u);

            if (Convert.ToBoolean((ulong)SharedConst.GetMaskForRace(factionChangeInfo.RaceID) & raceMaskDisabled))
            {
                SendCharFactionChange(ResponseCodes.CharCreateError, factionChangeInfo);

                return;
            }
        }

        // prevent character rename
        if (_configuration.GetDefaultValue("PreventRenameCharacterOnCustomization", false) && (factionChangeInfo.Name != oldName))
        {
            SendCharFactionChange(ResponseCodes.CharNameFailure, factionChangeInfo);

            return;
        }

        // prevent character rename to invalid name
        if (!_objectManager.NormalizePlayerName(ref factionChangeInfo.Name))
        {
            SendCharFactionChange(ResponseCodes.CharNameNoName, factionChangeInfo);

            return;
        }

        var res = _objectManager.CheckPlayerName(factionChangeInfo.Name, _session.SessionDbcLocale, true);

        if (res != ResponseCodes.CharNameSuccess)
        {
            SendCharFactionChange(res, factionChangeInfo);

            return;
        }

        // check name limitations
        if (!_session.HasPermission(RBACPermissions.SkipCheckCharacterCreationReservedname) && _objectManager.IsReservedName(factionChangeInfo.Name))
        {
            SendCharFactionChange(ResponseCodes.CharNameReserved, factionChangeInfo);

            return;
        }

        // character with this name already exist
        var newGuid = _characterCache.GetCharacterGuidByName(factionChangeInfo.Name);

        if (!newGuid.IsEmpty)
            if (newGuid != factionChangeInfo.Guid)
            {
                SendCharFactionChange(ResponseCodes.CharCreateNameInUse, factionChangeInfo);

                return;
            }

        if (_arenaTeamManager.GetArenaTeamByCaptain(factionChangeInfo.Guid) != null)
        {
            SendCharFactionChange(ResponseCodes.CharCreateCharacterArenaLeader, factionChangeInfo);

            return;
        }

        // All checks are fine, deal with race change now
        var lowGuid = factionChangeInfo.Guid.Counter;

        PreparedStatement stmt;
        SQLTransaction trans = new();

        // resurrect the character in case he's dead
        _playerComputators.OfflineResurrect(factionChangeInfo.Guid, trans);

        // Name Change and update atLogin flags
        {
            stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_CHAR_NAME_AT_LOGIN);
            stmt.AddValue(0, factionChangeInfo.Name);
            stmt.AddValue(1, (ushort)((atLoginFlags | AtLoginFlags.Resurrect) & ~usedLoginFlag));
            stmt.AddValue(2, lowGuid);

            trans.Append(stmt);

            stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_DECLINED_NAME);
            stmt.AddValue(0, lowGuid);

            trans.Append(stmt);
        }

        // Customize
        _playerComputators.SaveCustomizations(trans, lowGuid, factionChangeInfo.Customizations);

        // Race Change
        {
            stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_CHAR_RACE);
            stmt.AddValue(0, (byte)factionChangeInfo.RaceID);
            stmt.AddValue(1, (ushort)PlayerExtraFlags.HasRaceChanged);
            stmt.AddValue(2, lowGuid);

            trans.Append(stmt);
        }

        _characterCache.UpdateCharacterData(factionChangeInfo.Guid, factionChangeInfo.Name, (byte)factionChangeInfo.SexID, (byte)factionChangeInfo.RaceID);

        if (oldRace != factionChangeInfo.RaceID)
        {
            // Switch Languages
            // delete all languages first
            stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_SKILL_LANGUAGES);
            stmt.AddValue(0, lowGuid);
            trans.Append(stmt);

            // Now add them back
            stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_CHAR_SKILL_LANGUAGE);
            stmt.AddValue(0, lowGuid);

            // Faction specific languages
            stmt.AddValue(1, newTeamId == TeamIds.Horde ? 109 : 98);

            trans.Append(stmt);

            // Race specific languages
            if (factionChangeInfo.RaceID != Race.Orc && factionChangeInfo.RaceID != Race.Human)
            {
                stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_CHAR_SKILL_LANGUAGE);
                stmt.AddValue(0, lowGuid);

                switch (factionChangeInfo.RaceID)
                {
                    case Race.Dwarf:
                        stmt.AddValue(1, 111);

                        break;

                    case Race.Draenei:
                    case Race.LightforgedDraenei:
                        stmt.AddValue(1, 759);

                        break;

                    case Race.Gnome:
                        stmt.AddValue(1, 313);

                        break;

                    case Race.NightElf:
                        stmt.AddValue(1, 113);

                        break;

                    case Race.Worgen:
                        stmt.AddValue(1, 791);

                        break;

                    case Race.Undead:
                        stmt.AddValue(1, 673);

                        break;

                    case Race.Tauren:
                    case Race.HighmountainTauren:
                        stmt.AddValue(1, 115);

                        break;

                    case Race.Troll:
                        stmt.AddValue(1, 315);

                        break;

                    case Race.BloodElf:
                    case Race.VoidElf:
                        stmt.AddValue(1, 137);

                        break;

                    case Race.Goblin:
                        stmt.AddValue(1, 792);

                        break;

                    case Race.Nightborne:
                        stmt.AddValue(1, 2464);

                        break;

                    default:
                        Log.Logger.Error($"Could not find language data for race ({factionChangeInfo.RaceID}).");
                        SendCharFactionChange(ResponseCodes.CharCreateError, factionChangeInfo);

                        return;
                }

                trans.Append(stmt);
            }

            // Team Conversation
            if (factionChangeInfo.FactionChange)
            {
                // Delete all Flypaths
                stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_CHAR_TAXI_PATH);
                stmt.AddValue(0, lowGuid);
                trans.Append(stmt);

                if (level > 7)
                {
                    // Update Taxi path
                    // this doesn't seem to be 100% blizzlike... but it can't really be helped.
                    var taximaskstream = "";

                    var factionMask = newTeamId == TeamIds.Horde ? _cliDb.HordeTaxiNodesMask : _cliDb.AllianceTaxiNodesMask;

                    for (var i = 0; i < factionMask.Length; ++i)
                    {
                        // i = (315 - 1) / 8 = 39
                        // m = 1 << ((315 - 1) % 8) = 4
                        var deathKnightExtraNode = playerClass != PlayerClass.Deathknight || i != 39 ? 0 : 4;
                        taximaskstream += (uint)(factionMask[i] | deathKnightExtraNode) + ' ';
                    }

                    stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_CHAR_TAXIMASK);
                    stmt.AddValue(0, taximaskstream);
                    stmt.AddValue(1, lowGuid);
                    trans.Append(stmt);
                }

                if (!_configuration.GetDefaultValue("AllowTwoSide:Interaction:Guild", false))
                {
                    // Reset guild
                    var guild = _guildManager.GetGuildById(characterInfo.GuildId);

                    guild?.DeleteMember(trans, factionChangeInfo.Guid, false, false, true);

                    _playerComputators.LeaveAllArenaTeams(factionChangeInfo.Guid);
                }

                if (!_session.HasPermission(RBACPermissions.TwoSideAddFriend))
                {
                    // Delete Friend List
                    stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_SOCIAL_BY_GUID);
                    stmt.AddValue(0, lowGuid);
                    trans.Append(stmt);

                    stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_SOCIAL_BY_FRIEND);
                    stmt.AddValue(0, lowGuid);
                    trans.Append(stmt);
                }

                // Reset homebind and position
                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_PLAYER_HOMEBIND);
                stmt.AddValue(0, lowGuid);
                trans.Append(stmt);

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_PLAYER_HOMEBIND);
                stmt.AddValue(0, lowGuid);

                WorldLocation loc;
                ushort zoneId = 0;

                if (newTeamId == TeamIds.Alliance)
                {
                    loc = new WorldLocation(0, -8867.68f, 673.373f, 97.9034f);
                    zoneId = 1519;
                }
                else
                {
                    loc = new WorldLocation(1, 1633.33f, -4439.11f, 15.7588f);
                    zoneId = 1637;
                }

                stmt.AddValue(1, loc.MapId);
                stmt.AddValue(2, zoneId);
                stmt.AddValue(3, loc.X);
                stmt.AddValue(4, loc.Y);
                stmt.AddValue(5, loc.Z);
                trans.Append(stmt);

                _playerComputators.SavePositionInDB(loc, zoneId, factionChangeInfo.Guid, trans);

                // Achievement conversion
                foreach (var (achievAlliance, achievHorde) in _objectManager.FactionChangeCache.FactionChangeAchievements)
                {
                    stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_ACHIEVEMENT_BY_ACHIEVEMENT);
                    stmt.AddValue(0, (ushort)(newTeamId == TeamIds.Alliance ? achievAlliance : achievHorde));
                    stmt.AddValue(1, lowGuid);
                    trans.Append(stmt);

                    stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_CHAR_ACHIEVEMENT);
                    stmt.AddValue(0, (ushort)(newTeamId == TeamIds.Alliance ? achievAlliance : achievHorde));
                    stmt.AddValue(1, (ushort)(newTeamId == TeamIds.Alliance ? achievHorde : achievAlliance));
                    stmt.AddValue(2, lowGuid);
                    trans.Append(stmt);
                }

                // Item conversion
                var itemConversionMap = newTeamId == TeamIds.Alliance ? _objectManager.FactionChangeCache.FactionChangeItemsHordeToAlliance : _objectManager.FactionChangeCache.FactionChangeItemsAllianceToHorde;

                foreach (var (oldItemId, newItemId) in itemConversionMap)
                {
                    stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_CHAR_INVENTORY_FACTION_CHANGE);
                    stmt.AddValue(0, newItemId);
                    stmt.AddValue(1, oldItemId);
                    stmt.AddValue(2, lowGuid);
                    trans.Append(stmt);
                }

                // Delete all current quests
                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_QUESTSTATUS);
                stmt.AddValue(0, lowGuid);
                trans.Append(stmt);

                // Quest conversion
                foreach (var (questAlliance, questHorde) in _objectManager.FactionChangeCache.FactionChangeQuests)
                {
                    stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_QUESTSTATUS_REWARDED_BY_QUEST);
                    stmt.AddValue(0, lowGuid);
                    stmt.AddValue(1, (newTeamId == TeamIds.Alliance ? questAlliance : questHorde));
                    trans.Append(stmt);

                    stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_CHAR_QUESTSTATUS_REWARDED_FACTION_CHANGE);
                    stmt.AddValue(0, (newTeamId == TeamIds.Alliance ? questAlliance : questHorde));
                    stmt.AddValue(1, (newTeamId == TeamIds.Alliance ? questHorde : questAlliance));
                    stmt.AddValue(2, lowGuid);
                    trans.Append(stmt);
                }

                // Mark all rewarded quests as "active" (will count for completed quests achievements)
                stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_CHAR_QUESTSTATUS_REWARDED_ACTIVE);
                stmt.AddValue(0, lowGuid);
                trans.Append(stmt);

                // Disable all old-faction specific quests
                {
                    var questTemplates = _objectManager.QuestTemplateCache.QuestTemplates;

                    foreach (var quest in questTemplates.Values)
                    {
                        var newRaceMask = (long)(newTeamId == TeamIds.Alliance ? SharedConst.RaceMaskAlliance : SharedConst.RaceMaskHorde);

                        if (quest.AllowableRaces != -1 && !Convert.ToBoolean(quest.AllowableRaces & newRaceMask))
                        {
                            stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_CHAR_QUESTSTATUS_REWARDED_ACTIVE_BY_QUEST);
                            stmt.AddValue(0, lowGuid);
                            stmt.AddValue(1, quest.Id);
                            trans.Append(stmt);
                        }
                    }
                }

                // Spell conversion
                foreach (var (spellAlliance, spellHorde) in _objectManager.FactionChangeCache.FactionChangeSpells)
                {
                    stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_SPELL_BY_SPELL);
                    stmt.AddValue(0, (newTeamId == TeamIds.Alliance ? spellAlliance : spellHorde));
                    stmt.AddValue(1, lowGuid);
                    trans.Append(stmt);

                    stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_CHAR_SPELL_FACTION_CHANGE);
                    stmt.AddValue(0, (newTeamId == TeamIds.Alliance ? spellAlliance : spellHorde));
                    stmt.AddValue(1, (newTeamId == TeamIds.Alliance ? spellHorde : spellAlliance));
                    stmt.AddValue(2, lowGuid);
                    trans.Append(stmt);
                }

                // Reputation conversion
                foreach (var (reputationAlliance, reputationHorde) in _objectManager.FactionChangeCache.FactionChangeReputation)
                {
                    var newReputation = (newTeamId == TeamIds.Alliance) ? reputationAlliance : reputationHorde;
                    var oldReputation = (newTeamId == TeamIds.Alliance) ? reputationHorde : reputationAlliance;

                    // select old standing set in db
                    stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHAR_REP_BY_FACTION);
                    stmt.AddValue(0, oldReputation);
                    stmt.AddValue(1, lowGuid);

                    result = _characterDatabase.Query(stmt);

                    if (result.IsEmpty())
                        continue;

                    var oldDBRep = result.Read<int>(0);
                    var factionEntry = _cliDb.FactionStorage.LookupByKey(oldReputation);

                    // old base reputation
                    var oldBaseRep = ReputationMgr.GetBaseReputationOf(factionEntry, oldRace, playerClass);

                    // new base reputation
                    var newBaseRep = ReputationMgr.GetBaseReputationOf(_cliDb.FactionStorage.LookupByKey(newReputation), factionChangeInfo.RaceID, playerClass);

                    // final reputation shouldnt change
                    var finalRep = oldDBRep + oldBaseRep;
                    var newDBRep = finalRep - newBaseRep;

                    stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_REP_BY_FACTION);
                    stmt.AddValue(0, newReputation);
                    stmt.AddValue(1, lowGuid);
                    trans.Append(stmt);

                    stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_CHAR_REP_FACTION_CHANGE);
                    stmt.AddValue(0, (ushort)newReputation);
                    stmt.AddValue(1, newDBRep);
                    stmt.AddValue(2, (ushort)oldReputation);
                    stmt.AddValue(3, lowGuid);
                    trans.Append(stmt);
                }

                // Title conversion
                if (!string.IsNullOrEmpty(knownTitlesStr))
                {
                    List<uint> knownTitles = new();

                    var tokens = new StringArray(knownTitlesStr, ' ');

                    for (var index = 0; index < tokens.Length; ++index)
                        if (uint.TryParse(tokens[index], out var id))
                            knownTitles.Add(id);

                    foreach (var (titleAlliance, titleHorde) in _factionChangeTitleCache.FactionChangeTitles)
                    {
                        var atitleInfo = _cliDb.CharTitlesStorage.LookupByKey(titleAlliance);
                        var htitleInfo = _cliDb.CharTitlesStorage.LookupByKey(titleHorde);

                        // new team
                        if (newTeamId == TeamIds.Alliance)
                        {
                            uint maskID = htitleInfo.MaskID;
                            var index = (int)maskID / 32;

                            if (index >= knownTitles.Count)
                                continue;

                            var oldFlag = (uint)(1 << (int)(maskID % 32));
                            var newFlag = (uint)(1 << (atitleInfo.MaskID % 32));

                            if (Convert.ToBoolean(knownTitles[index] & oldFlag))
                            {
                                knownTitles[index] &= ~oldFlag;
                                // use index of the new title
                                knownTitles[atitleInfo.MaskID / 32] |= newFlag;
                            }
                        }
                        else
                        {
                            uint maskID = atitleInfo.MaskID;
                            var index = (int)maskID / 32;

                            if (index >= knownTitles.Count)
                                continue;

                            var oldFlag = (uint)(1 << (int)(maskID % 32));
                            var newFlag = (uint)(1 << (htitleInfo.MaskID % 32));

                            if (Convert.ToBoolean(knownTitles[index] & oldFlag))
                            {
                                knownTitles[index] &= ~oldFlag;
                                // use index of the new title
                                knownTitles[htitleInfo.MaskID / 32] |= newFlag;
                            }
                        }

                        var ss = knownTitles.Aggregate("", (current, t) => current + (t + ' '));

                        stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_CHAR_TITLES_FACTION_CHANGE);
                        stmt.AddValue(0, ss);
                        stmt.AddValue(1, lowGuid);
                        trans.Append(stmt);

                        // unset any currently chosen title
                        stmt = _characterDatabase.GetPreparedStatement(CharStatements.RES_CHAR_TITLES_FACTION_CHANGE);
                        stmt.AddValue(0, lowGuid);
                        trans.Append(stmt);
                    }
                }
            }
        }

        _characterDatabase.CommitTransaction(trans);

        Log.Logger.Debug("{0} (IP: {1}) changed race from {2} to {3}", _session.GetPlayerInfo(), _session.RemoteAddress, oldRace, factionChangeInfo.RaceID);

        SendCharFactionChange(ResponseCodes.Success, factionChangeInfo);
    }

    [WorldPacketHandler(ClientOpcodes.CharacterRenameRequest, Status = SessionStatus.Authed)]
    private void HandleCharRename(CharacterRenameRequest request)
    {
        if (!_legitCharacters.Contains(request.RenameInfo.Guid))
        {
            Log.Logger.Error(
                        "Account {0}, IP: {1} tried to rename character {2}, but it does not belong to their account!",
                        _session.AccountId,
                        _session.RemoteAddress,
                        request.RenameInfo.Guid.ToString());

            _session.KickPlayer("WorldSession::HandleCharRenameOpcode rename character from a different account");

            return;
        }

        // prevent character rename to invalid name
        if (!_objectManager.NormalizePlayerName(ref request.RenameInfo.NewName))
        {
            SendCharRename(ResponseCodes.CharNameNoName, request.RenameInfo);

            return;
        }

        var res = _objectManager.CheckPlayerName(request.RenameInfo.NewName, _session.SessionDbcLocale, true);

        if (res != ResponseCodes.CharNameSuccess)
        {
            SendCharRename(res, request.RenameInfo);

            return;
        }

        if (!_session.HasPermission(RBACPermissions.SkipCheckCharacterCreationReservedname) && _objectManager.IsReservedName(request.RenameInfo.NewName))
        {
            SendCharRename(ResponseCodes.CharNameReserved, request.RenameInfo);

            return;
        }

        // Ensure that there is no character with the desired new name
        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_FREE_NAME);
        stmt.AddValue(0, request.RenameInfo.Guid.Counter);
        stmt.AddValue(1, request.RenameInfo.NewName);

        _session.QueryProcessor.AddCallback(_characterDatabase.AsyncQuery(stmt).WithCallback(HandleCharRenameCallBack, request.RenameInfo));
    }

    private void HandleCharRenameCallBack(CharacterRenameInfo renameInfo, SQLResult result)
    {
        if (result.IsEmpty())
        {
            SendCharRename(ResponseCodes.CharNameFailure, renameInfo);

            return;
        }

        var oldName = result.Read<string>(0);
        // check name limitations
        var atLoginFlags = (AtLoginFlags)result.Read<uint>(1);

        if (!atLoginFlags.HasAnyFlag(AtLoginFlags.Rename))
        {
            SendCharRename(ResponseCodes.CharCreateError, renameInfo);

            return;
        }

        atLoginFlags &= ~AtLoginFlags.Rename;

        SQLTransaction trans = new();
        var lowGuid = renameInfo.Guid.Counter;

        // Update name and at_login flag in the db
        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_CHAR_NAME_AT_LOGIN);
        stmt.AddValue(0, renameInfo.NewName);
        stmt.AddValue(1, (ushort)atLoginFlags);
        stmt.AddValue(2, lowGuid);
        trans.Append(stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_DECLINED_NAME);
        stmt.AddValue(0, lowGuid);
        trans.Append(stmt);

        _characterDatabase.CommitTransaction(trans);

        Log.Logger.Information(
                    "Account: {0} (IP: {1}) Character:[{2}] ({3}) Changed name to: {4}",
                    _session.AccountId,
                    _session.RemoteAddress,
                    oldName,
                    renameInfo.Guid.ToString(),
                    renameInfo.NewName);

        SendCharRename(ResponseCodes.Success, renameInfo);

        _characterCache.UpdateCharacterData(renameInfo.Guid, renameInfo.NewName);
    }

    [WorldPacketHandler(ClientOpcodes.UndeleteCharacter, Status = SessionStatus.Authed)]
    private void HandleCharUndelete(UndeleteCharacter undeleteCharacter)
    {
        if (!_configuration.GetDefaultValue("FeatureSystem:CharacterUndelete:Enabled", false))
        {
            SendUndeleteCharacterResponse(CharacterUndeleteResult.Disabled, undeleteCharacter.UndeleteInfo);

            return;
        }

        var stmt = _loginDatabase.GetPreparedStatement(LoginStatements.SEL_LAST_CHAR_UNDELETE);
        stmt.AddValue(0, _session.BattlenetAccountId);

        var undeleteInfo = undeleteCharacter.UndeleteInfo;

        _session.QueryProcessor.AddCallback(_loginDatabase.AsyncQuery(stmt)
                                    .WithChainingCallback((queryCallback, result) =>
                                    {
                                        if (!result.IsEmpty())
                                        {
                                            var lastUndelete = result.Read<uint>(0);
                                            var maxCooldown = _configuration.GetDefaultValue("FeatureSystem:CharacterUndelete:Cooldown", 2592000u);

                                            if (lastUndelete != 0 && (lastUndelete + maxCooldown > GameTime.CurrentTime))
                                            {
                                                SendUndeleteCharacterResponse(CharacterUndeleteResult.Cooldown, undeleteInfo);

                                                return;
                                            }
                                        }

                                        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHAR_DEL_INFO_BY_GUID);
                                        stmt.AddValue(0, undeleteInfo.CharacterGuid.Counter);
                                        queryCallback.SetNextQuery(_characterDatabase.AsyncQuery(stmt));
                                    })
                                    .WithChainingCallback((queryCallback, result) =>
                                    {
                                        if (result.IsEmpty())
                                        {
                                            SendUndeleteCharacterResponse(CharacterUndeleteResult.CharCreate, undeleteInfo);

                                            return;
                                        }

                                        undeleteInfo.Name = result.Read<string>(1);
                                        var account = result.Read<uint>(2);

                                        if (account != _session.AccountId)
                                        {
                                            SendUndeleteCharacterResponse(CharacterUndeleteResult.Unknown, undeleteInfo);

                                            return;
                                        }

                                        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHECK_NAME);
                                        stmt.AddValue(0, undeleteInfo.Name);
                                        queryCallback.SetNextQuery(_characterDatabase.AsyncQuery(stmt));
                                    })
                                    .WithChainingCallback((queryCallback, result) =>
                                    {
                                        if (!result.IsEmpty())
                                        {
                                            SendUndeleteCharacterResponse(CharacterUndeleteResult.NameTakenByThisAccount, undeleteInfo);

                                            return;
                                        }

                                        // @todo: add more safety checks
                                        // * max char count per account
                                        // * max death knight count
                                        // * max demon hunter count
                                        // * team violation

                                        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_SUM_CHARS);
                                        stmt.AddValue(0, _session.AccountId);
                                        queryCallback.SetNextQuery(_characterDatabase.AsyncQuery(stmt));
                                    })
                                    .WithCallback(result =>
                                    {
                                        if (!result.IsEmpty())
                                            if (result.Read<ulong>(0) >= _configuration.GetDefaultValue("CharactersPerRealm", 60ul)) // SQL's COUNT() returns uint64 but it will always be less than uint8.Max
                                            {
                                                SendUndeleteCharacterResponse(CharacterUndeleteResult.CharCreate, undeleteInfo);

                                                return;
                                            }

                                        stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_RESTORE_DELETE_INFO);
                                        stmt.AddValue(0, undeleteInfo.Name);
                                        stmt.AddValue(1, _session.AccountId);
                                        stmt.AddValue(2, undeleteInfo.CharacterGuid.Counter);
                                        _characterDatabase.Execute(stmt);

                                        stmt = _loginDatabase.GetPreparedStatement(LoginStatements.UPD_LAST_CHAR_UNDELETE);
                                        stmt.AddValue(0, _session.BattlenetAccountId);
                                        _loginDatabase.Execute(stmt);

                                        _characterCache.UpdateCharacterInfoDeleted(undeleteInfo.CharacterGuid, false, undeleteInfo.Name);

                                        SendUndeleteCharacterResponse(CharacterUndeleteResult.Ok, undeleteInfo);
                                    }));
    }

    [WorldPacketHandler(ClientOpcodes.EnumCharactersDeletedByClient, Status = SessionStatus.Authed)]
    private void HandleCharUndeleteEnum(EnumCharacters enumCharacters)
    {
        if (enumCharacters == null)
            return;

        // get all the data necessary for loading all undeleted characters (along with their pets) on the account
        EnumCharactersQueryHolder holder = new(_characterDatabase);

        if (!holder.Initialize(_session.AccountId, _configuration.GetDefaultValue("DeclinedNames", false), true))
        {
            HandleCharEnum(holder);

            return;
        }

        _session.AddQueryHolderCallback(_characterDatabase.DelayQueryHolder(holder)).AfterComplete(result => HandleCharEnum((EnumCharactersQueryHolder)result));
    }

    private void HandleCharUndeleteEnumCallback(SQLResult result)
    {
        EnumCharactersResult charEnum = new()
        {
            Success = true,
            IsDeletedCharacters = true,
            DisabledClassesMask = _configuration.GetDefaultValue("CharacterCreating:Disabled:ClassMask", 0u)
        };

        if (!result.IsEmpty())
            do
            {
                EnumCharactersResult.CharacterInfo charInfo = new(result.GetFields(), _configuration, _objectManager, _dB2Manager);

                Log.Logger.Information("Loading undeleted char guid {0} from account {1}.", charInfo.Guid.ToString(), _session.AccountId);

                if (!_characterCache.HasCharacterCacheEntry(charInfo.Guid)) // This can happen if characters are inserted into the database manually. Core hasn't loaded name data yet.
                    _characterCache.AddCharacterCacheEntry(charInfo.Guid, _session.AccountId, charInfo.Name, charInfo.SexId, charInfo.RaceId, (byte)charInfo.ClassId, charInfo.ExperienceLevel, true);

                charEnum.Characters.Add(charInfo);
            } while (result.NextRow());

        _session.SendPacket(charEnum);
    }

    [WorldPacketHandler(ClientOpcodes.CheckCharacterNameAvailability)]
    private void HandleCheckCharacterNameAvailability(CheckCharacterNameAvailability checkCharacterNameAvailability)
    {
        // prevent character rename to invalid name
        if (!_objectManager.NormalizePlayerName(ref checkCharacterNameAvailability.Name))
        {
            _session.SendPacket(new CheckCharacterNameAvailabilityResult(checkCharacterNameAvailability.SequenceIndex, ResponseCodes.CharNameNoName));

            return;
        }

        var res = _objectManager.CheckPlayerName(checkCharacterNameAvailability.Name, _session.SessionDbcLocale, true);

        if (res != ResponseCodes.CharNameSuccess)
        {
            _session.SendPacket(new CheckCharacterNameAvailabilityResult(checkCharacterNameAvailability.SequenceIndex, res));

            return;
        }

        // check name limitations
        if (!_session.HasPermission(RBACPermissions.SkipCheckCharacterCreationReservedname) && _objectManager.IsReservedName(checkCharacterNameAvailability.Name))
        {
            _session.SendPacket(new CheckCharacterNameAvailabilityResult(checkCharacterNameAvailability.SequenceIndex, ResponseCodes.CharNameReserved));

            return;
        }

        // Ensure that there is no character with the desired new name
        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHECK_NAME);
        stmt.AddValue(0, checkCharacterNameAvailability.Name);

        var sequenceIndex = checkCharacterNameAvailability.SequenceIndex;
        _session.QueryProcessor.AddCallback(_characterDatabase.AsyncQuery(stmt).WithCallback(result => { _session.SendPacket(new CheckCharacterNameAvailabilityResult(sequenceIndex, !result.IsEmpty() ? ResponseCodes.CharCreateNameInUse : ResponseCodes.Success)); }));
    }

    [WorldPacketHandler(ClientOpcodes.DeleteEquipmentSet)]
    private void HandleDeleteEquipmentSet(DeleteEquipmentSet packet)
    {
        _session.Player.DeleteEquipmentSet(packet.ID);
    }

    [WorldPacketHandler(ClientOpcodes.SaveEquipmentSet)]
    private void HandleEquipmentSetSave(SaveEquipmentSet saveEquipmentSet)
    {
        if (saveEquipmentSet.Set.SetId >= ItemConst.MaxEquipmentSetIndex) // client set slots amount
            return;

        if (saveEquipmentSet.Set.Type > EquipmentSetInfo.EquipmentSetType.Transmog)
            return;

        for (byte i = 0; i < EquipmentSlot.End; ++i)
            if (!Convert.ToBoolean(saveEquipmentSet.Set.IgnoreMask & (1 << i)))
            {
                if (saveEquipmentSet.Set.Type == EquipmentSetInfo.EquipmentSetType.Equipment)
                {
                    saveEquipmentSet.Set.Appearances[i] = 0;

                    var itemGuid = saveEquipmentSet.Set.Pieces[i];

                    if (!itemGuid.IsEmpty)
                    {
                        var item = _session.Player.GetItemByPos(InventorySlots.Bag0, i);

                        // cheating check 1 (item equipped but sent empty guid)
                        if (item == null)
                            return;

                        // cheating check 2 (sent guid does not match equipped item)
                        if (item.GUID != itemGuid)
                            return;
                    }
                    else
                    {
                        saveEquipmentSet.Set.IgnoreMask |= 1u << i;
                    }
                }
                else
                {
                    saveEquipmentSet.Set.Pieces[i].Clear();

                    if (saveEquipmentSet.Set.Appearances[i] != 0)
                    {
                        if (!_cliDb.ItemModifiedAppearanceStorage.ContainsKey(saveEquipmentSet.Set.Appearances[i]))
                            return;

                        var (hasAppearance, _) = _collectionMgr.HasItemAppearance((uint)saveEquipmentSet.Set.Appearances[i]);

                        if (!hasAppearance)
                            return;
                    }
                    else
                    {
                        saveEquipmentSet.Set.IgnoreMask |= 1u << i;
                    }
                }
            }
            else
            {
                saveEquipmentSet.Set.Pieces[i].Clear();
                saveEquipmentSet.Set.Appearances[i] = 0;
            }

        saveEquipmentSet.Set.IgnoreMask &= 0x7FFFF; // clear invalid bits (i > EQUIPMENT_SLOT_END)

        if (saveEquipmentSet.Set.Type == EquipmentSetInfo.EquipmentSetType.Equipment)
        {
            saveEquipmentSet.Set.Enchants[0] = 0;
            saveEquipmentSet.Set.Enchants[1] = 0;
        }
        else
        {
            var validateIllusion = new Func<uint, bool>(enchantId =>
            {
                var illusion = _cliDb.SpellItemEnchantmentStorage.LookupByKey(enchantId);

                if (illusion == null)
                    return false;

                if (illusion.ItemVisual == 0 || !illusion.GetFlags().HasFlag(SpellItemEnchantmentFlags.AllowTransmog))
                    return false;

                var condition = _cliDb.PlayerConditionStorage.LookupByKey(illusion.TransmogUseConditionID);

                if (condition != null && !_conditionManager.IsPlayerMeetingCondition(_session.Player, condition))
                    return false;

                return illusion.ScalingClassRestricted <= 0 || illusion.ScalingClassRestricted == (byte)_session.Player.Class;
            });

            if (saveEquipmentSet.Set.Enchants[0] != 0 && !validateIllusion((uint)saveEquipmentSet.Set.Enchants[0]))
                return;

            if (saveEquipmentSet.Set.Enchants[1] != 0 && !validateIllusion((uint)saveEquipmentSet.Set.Enchants[1]))
                return;
        }

        _session.Player.SetEquipmentSet(saveEquipmentSet.Set);
    }

    [WorldPacketHandler(ClientOpcodes.GetUndeleteCharacterCooldownStatus, Status = SessionStatus.Authed)]
    private void HandleGetUndeleteCooldownStatus(GetUndeleteCharacterCooldownStatus getCooldown)
    {
        if (getCooldown == null)
            return;

        var stmt = _loginDatabase.GetPreparedStatement(LoginStatements.SEL_LAST_CHAR_UNDELETE);
        stmt.AddValue(0, _session.BattlenetAccountId);

        _session.QueryProcessor.AddCallback(_loginDatabase.AsyncQuery(stmt).WithCallback(HandleUndeleteCooldownStatusCallback));
    }

    [WorldPacketHandler(ClientOpcodes.LoadingScreenNotify, Status = SessionStatus.Authed)]
    private void HandleLoadScreen(LoadingScreenNotify loadingScreenNotify)
    {
        if (loadingScreenNotify != null)
        {
            // TODO: Do something with this packet
        }
    }

    [WorldPacketHandler(ClientOpcodes.OpeningCinematic)]
    private void HandleOpeningCinematic(OpeningCinematic packet)
    {
        if (packet == null)
            return;

        // Only players that has not yet gained any experience can use this
        if (_session.Player.ActivePlayerData.XP != 0)
            return;

        var classEntry = _cliDb.ChrClassesStorage.LookupByKey((uint)_session.Player.Class);

        if (classEntry == null)
            return;

        var raceEntry = _cliDb.ChrRacesStorage.LookupByKey((uint)_session.Player.Race);

        if (classEntry.CinematicSequenceID != 0)
            _session.Player.SendCinematicStart(classEntry.CinematicSequenceID);
        else if (raceEntry != null)
            _session.Player.SendCinematicStart(raceEntry.CinematicSequenceID);
    }

    [WorldPacketHandler(ClientOpcodes.OverrideScreenFlash)]
    private void HandleOverrideScreenFlash(OverrideScreenFlash packet)
    {
        _session.Player.OverrideScreenFlash = packet.ScreenFlashEnabled;
    }

    [WorldPacketHandler(ClientOpcodes.PlayerLogin, Status = SessionStatus.Authed)]
    private void HandlePlayerLogin(PlayerLogin playerLogin)
    {
        if (_session.PlayerLoadingGuid.IsEmpty || _session.Player != null)
        {
            Log.Logger.Error("Player tries to login again, _session.AccountId = {0}", _session.AccountId);
            _session.KickPlayer("WorldSession::HandlePlayerLoginOpcode Another client logging in");

            return;
        }

        _session.PlayerLoadingGuid = playerLogin.Guid;
        Log.Logger.Debug("Character {0} logging in", playerLogin.Guid.ToString());

        if (!_legitCharacters.Contains(playerLogin.Guid))
        {
            Log.Logger.Error("Account ({0}) can't login with that character ({1}).", _session.AccountId, playerLogin.Guid.ToString());
            _session.KickPlayer("WorldSession::HandlePlayerLoginOpcode Trying to login with a character of another account");

            return;
        }

        _session.SendConnectToInstance(ConnectToSerial.WorldAttempt1);
    }

    [WorldPacketHandler(ClientOpcodes.ClientPortGraveyard)]
    private void HandlePortGraveyard(PortGraveyard packet)
    {
        if (packet == null || _session.Player.IsAlive || !_session.Player.HasPlayerFlag(PlayerFlags.Ghost))
            return;

        _session.Player.RepopAtGraveyard();
    }

    [WorldPacketHandler(ClientOpcodes.QuickJoinAutoAcceptRequests)]
    private void HandleQuickJoinAutoAcceptRequests(QuickJoinAutoAcceptRequest packet)
    {
        _session.Player.AutoAcceptQuickJoin = packet.AutoAccept;
    }

    [WorldPacketHandler(ClientOpcodes.GenerateRandomCharacterName, Status = SessionStatus.Authed)]
    private void HandleRandomizeCharName(GenerateRandomCharacterName packet)
    {
        if (!_playerComputators.IsValidRace((Race)packet.Race))
        {
            Log.Logger.Error("Invalid race ({0}) sent by accountId: {1}", packet.Race, _session.AccountId);

            return;
        }

        if (!_playerComputators.IsValidGender((Gender)packet.Sex))
        {
            Log.Logger.Error("Invalid gender ({0}) sent by accountId: {1}", packet.Sex, _session.AccountId);

            return;
        }

        GenerateRandomCharacterNameResult result = new()
        {
            Success = true,
            Name = _dB2Manager.GetNameGenEntry(packet.Race, packet.Sex)
        };

        _session.SendPacket(result);
    }

    [WorldPacketHandler(ClientOpcodes.ReclaimCorpse)]
    private void HandleReclaimCorpse(ReclaimCorpse packet)
    {
        if (_session.Player.IsAlive || packet == null)
            return;

        // do not allow corpse reclaim in arena
        if (_session.Player.InArena)
            return;

        // body not released yet
        if (!_session.Player.HasPlayerFlag(PlayerFlags.Ghost))
            return;

        if (_session.Player.Corpse == null)
            return;

        // prevent resurrect before 30-sec delay after body release not finished
        if ((_session.Player.Corpse.GhostTime + _session.Player.GetCorpseReclaimDelay(_session.Player.Corpse.CorpseType == CorpseType.ResurrectablePVP)) > GameTime.CurrentTime)
            return;

        if (!_session.Player.Corpse.Location.IsWithinDistInMap(_session.Player, 39))
            return;

        // resurrect
        _session.Player.ResurrectPlayer(_session.Player.InBattleground ? 1.0f : 0.5f);

        // spawn bones
        _session.Player.SpawnCorpseBones();
    }

    [WorldPacketHandler(ClientOpcodes.ReorderCharacters, Status = SessionStatus.Authed)]
    private void HandleReorderCharacters(ReorderCharacters reorderChars)
    {
        SQLTransaction trans = new();

        foreach (var reorderInfo in reorderChars.Entries)
        {
            var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_CHAR_LIST_SLOT);
            stmt.AddValue(0, reorderInfo.NewPosition);
            stmt.AddValue(1, reorderInfo.PlayerGUID.Counter);
            stmt.AddValue(2, _session.AccountId);
            trans.Append(stmt);
        }

        _characterDatabase.CommitTransaction(trans);
    }

    [WorldPacketHandler(ClientOpcodes.RepopRequest)]
    private void HandleRepopRequest(RepopRequest packet)
    {
        if (packet == null || _session.Player.IsAlive || _session.Player.HasPlayerFlag(PlayerFlags.Ghost))
            return;

        if (_session.Player.HasAuraType(AuraType.PreventResurrection))
            return; // silently return, client should display the error by itself

        // the world update order is sessions, players, creatures
        // the netcode runs in parallel with all of these
        // creatures can kill players
        // so if the server is lagging enough the player can
        // release spirit after he's killed but before he is updated
        if (_session.Player.DeathState == DeathState.JustDied)
        {
            Log.Logger.Debug(
                        "HandleRepopRequestOpcode: got request after player {0} ({1}) was killed and before he was updated",
                        _session.Player.GetName(),
                        _session.Player.GUID.ToString());

            _session.Player.KillPlayer();
        }

        //this is spirit release confirm?
        _session.Player.RemovePet(null, PetSaveMode.NotInSlot, true);
        _session.Player.BuildPlayerRepop();
        _session.Player.RepopAtGraveyard();
    }

    [WorldPacketHandler(ClientOpcodes.RequestCemeteryList, Processing = PacketProcessing.Inplace)]
    private void HandleRequestCemeteryList(RequestCemeteryList requestCemeteryList)
    {
        if (requestCemeteryList == null)
            return;

        var zoneId = _session.Player.Location.Zone;
        var team = (uint)_session.Player.Team;

        List<uint> graveyardIds = new();
        var range = _graveyardCache.GraveYardStorage.LookupByKey(zoneId);

        for (uint i = 0; i < range.Count && graveyardIds.Count < 16; ++i) // client max
        {
            var gYard = range[(int)i];

            if (gYard.Team == 0 || gYard.Team == team)
                graveyardIds.Add(i);
        }

        if (graveyardIds.Empty())
        {
            Log.Logger.Debug(
                        "No graveyards found for zone {0} for player {1} (team {2}) in CMSG_REQUEST_CEMETERY_LIST",
                        zoneId,
                        _session.Player.GUID.Counter,
                        team);

            return;
        }

        RequestCemeteryListResponse packet = new()
        {
            IsGossipTriggered = false
        };

        foreach (var id in graveyardIds)
            packet.CemeteryID.Add(id);

        _session.SendPacket(packet);
    }

    [WorldPacketHandler(ClientOpcodes.RequestForcedReactions)]
    private void HandleRequestForcedReactions(RequestForcedReactions requestForcedReactions)
    {
        if (requestForcedReactions == null)
            return;

        _session.Player.ReputationMgr.SendForceReactions();
    }

    [WorldPacketHandler(ClientOpcodes.ResurrectResponse)]
    private void HandleResurrectResponse(ResurrectResponse packet)
    {
        if (_session.Player.IsAlive)
            return;

        if (packet.Response != 0) // Accept = 0 Decline = 1 Timeout = 2
        {
            _session.Player.ClearResurrectRequestData(); // reject

            return;
        }

        if (!_session.Player.IsRessurectRequestedBy(packet.Resurrecter))
            return;

        var ressPlayer = _objectAccessor.GetPlayer(_session.Player, packet.Resurrecter);

        var instance = ressPlayer?.Location.InstanceScript;

        if (instance != null)
            if (instance.IsEncounterInProgress())
            {
                if (instance.GetCombatResurrectionCharges() == 0)
                    return;

                instance.UseCombatResurrection();
            }

        _session.Player.ResurrectUsingRequestData();
    }

    [WorldPacketHandler(ClientOpcodes.SetFactionAtWar)]
    private void HandleSetFactionAtWar(SetFactionAtWar packet)
    {
        _session.Player.ReputationMgr.SetAtWar(packet.FactionIndex, true);
    }

    [WorldPacketHandler(ClientOpcodes.SetFactionInactive)]
    private void HandleSetFactionInactive(SetFactionInactive packet)
    {
        _session.Player.ReputationMgr.SetInactive(packet.Index, packet.State);
    }

    [WorldPacketHandler(ClientOpcodes.SetFactionNotAtWar)]
    private void HandleSetFactionNotAtWar(SetFactionNotAtWar packet)
    {
        _session.Player.ReputationMgr.SetAtWar(packet.FactionIndex, false);
    }

    [WorldPacketHandler(ClientOpcodes.SetPlayerDeclinedNames, Status = SessionStatus.Authed)]
    private void HandleSetPlayerDeclinedNames(SetPlayerDeclinedNames packet)
    {
        // not accept declined names for unsupported languages
        if (!_characterCache.GetCharacterNameByGuid(packet.Player, out var name))
        {
            SendSetPlayerDeclinedNamesResult(DeclinedNameResult.Error, packet.Player);

            return;
        }

        if (!char.IsLetter(name[0])) // name already stored as only single alphabet using
        {
            SendSetPlayerDeclinedNamesResult(DeclinedNameResult.Error, packet.Player);

            return;
        }

        for (var i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
        {
            var declinedName = packet.DeclinedNames.Name[i];

            if (!_objectManager.NormalizePlayerName(ref declinedName))
            {
                SendSetPlayerDeclinedNamesResult(DeclinedNameResult.Error, packet.Player);

                return;
            }

            packet.DeclinedNames.Name[i] = declinedName;
        }

        for (var i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
        {
            var declinedName = packet.DeclinedNames.Name[i];
            CharacterDatabase.EscapeString(ref declinedName);
            packet.DeclinedNames.Name[i] = declinedName;
        }

        SQLTransaction trans = new();

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_DECLINED_NAME);
        stmt.AddValue(0, packet.Player.Counter);
        trans.Append(stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_CHAR_DECLINED_NAME);
        stmt.AddValue(0, packet.Player.Counter);

        for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; i++)
            stmt.AddValue(i + 1, packet.DeclinedNames.Name[i]);

        trans.Append(stmt);

        _characterDatabase.CommitTransaction(trans);

        SendSetPlayerDeclinedNamesResult(DeclinedNameResult.Success, packet.Player);
    }

    [WorldPacketHandler(ClientOpcodes.SetWatchedFaction)]
    private void HandleSetWatchedFaction(SetWatchedFaction packet)
    {
        _session.Player.SetWatchedFactionIndex(packet.FactionIndex);
    }

    [WorldPacketHandler(ClientOpcodes.StandStateChange)]
    private void HandleStandStateChange(StandStateChange packet)
    {
        switch (packet.StandState)
        {
            case UnitStandStateType.Stand:
            case UnitStandStateType.Sit:
            case UnitStandStateType.Sleep:
            case UnitStandStateType.Kneel:
                break;

            default:
                return;
        }

        _session.Player.SetStandState(packet.StandState);
    }

    [WorldPacketHandler(ClientOpcodes.Tutorial)]
    private void HandleTutorialFlag(TutorialSetFlag packet)
    {
        switch (packet.Action)
        {
            case TutorialAction.Update:
            {
                var index = (byte)(packet.TutorialBit >> 5);

                if (index >= SharedConst.MaxAccountTutorialValues)
                {
                    Log.Logger.Error("CMSG_TUTORIAL_FLAG received bad TutorialBit {0}.", packet.TutorialBit);

                    return;
                }

                var flag = _session.GetTutorialInt(index);
                flag |= (uint)(1 << (int)(packet.TutorialBit & 0x1F));
                _session.SetTutorialInt(index, flag);

                break;
            }
            case TutorialAction.Clear:
                for (byte i = 0; i < SharedConst.MaxAccountTutorialValues; ++i)
                    _session.SetTutorialInt(i, 0xFFFFFFFF);

                break;

            case TutorialAction.Reset:
                for (byte i = 0; i < SharedConst.MaxAccountTutorialValues; ++i)
                    _session.SetTutorialInt(i, 0x00000000);

                break;

            default:
                Log.Logger.Error("CMSG_TUTORIAL_FLAG received unknown TutorialAction {0}.", packet.Action);

                return;
        }
    }

    private void HandleUndeleteCooldownStatusCallback(SQLResult result)
    {
        uint cooldown = 0;
        var maxCooldown = _configuration.GetDefaultValue("FeatureSystem:CharacterUndelete:Cooldown", 2592000u);

        if (!result.IsEmpty())
        {
            var lastUndelete = result.Read<uint>(0);
            var now = (uint)GameTime.CurrentTime;

            if (lastUndelete + maxCooldown > now)
                cooldown = Math.Max(0, lastUndelete + maxCooldown - now);
        }

        SendUndeleteCooldownStatusResponse(cooldown, maxCooldown);
    }

    [WorldPacketHandler(ClientOpcodes.UseEquipmentSet, Processing = PacketProcessing.Inplace)]
    private void HandleUseEquipmentSet(UseEquipmentSet useEquipmentSet)
    {
        ObjectGuid ignoredItemGuid = new(0x0C00040000000000, 0xFFFFFFFFFFFFFFFF);

        for (byte i = 0; i < EquipmentSlot.End; ++i)
        {
            Log.Logger.Debug("{0}: ContainerSlot: {1}, Slot: {2}", useEquipmentSet.Items[i].Item.ToString(), useEquipmentSet.Items[i].ContainerSlot, useEquipmentSet.Items[i].Slot);

            // check if item slot is set to "ignored" (raw value == 1), must not be unequipped then
            if (useEquipmentSet.Items[i].Item == ignoredItemGuid)
                continue;

            // Only equip weapons in combat
            if (_session.Player.IsInCombat && i != EquipmentSlot.MainHand && i != EquipmentSlot.OffHand)
                continue;

            var item = _session.Player.GetItemByGuid(useEquipmentSet.Items[i].Item);

            var dstPos = (ushort)(i | (InventorySlots.Bag0 << 8));

            if (item == null)
            {
                var uItem = _session.Player.GetItemByPos(InventorySlots.Bag0, i);

                if (uItem == null)
                    continue;

                List<ItemPosCount> itemPosCount = new();
                var inventoryResult = _session.Player.CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, itemPosCount, uItem);

                if (inventoryResult == InventoryResult.Ok)
                {
                    if (_session.Player.CanUnequipItem(dstPos, true) != InventoryResult.Ok)
                        continue;

                    _session.Player.RemoveItem(InventorySlots.Bag0, i, true);
                    _session.Player.StoreItem(itemPosCount, uItem, true);
                }
                else
                {
                    _session.Player.SendEquipError(inventoryResult, uItem);
                }

                continue;
            }

            if (item.Pos == dstPos)
                continue;

            if (_session.Player.CanEquipItem(i, out dstPos, item, true) != InventoryResult.Ok)
                continue;

            _session.Player.SwapItem(item.Pos, dstPos);
        }

        UseEquipmentSetResult result = new()
        {
            GUID = useEquipmentSet.GUID,
            Reason = 0 // 4 - equipment swap failed - inventory is full
        };
        _session.SendPacket(result);
    }

    private void SendCharCreate(ResponseCodes result, ObjectGuid guid = default)
    {
        CreateChar response = new()
        {
            Code = result,
            Guid = guid
        };

        _session.SendPacket(response);
    }

    private void SendCharCustomize(ResponseCodes result, CharCustomizeInfo customizeInfo)
    {
        if (result == ResponseCodes.Success)
        {
            CharCustomizeSuccess response = new(customizeInfo);
            _session.SendPacket(response);
        }
        else
        {
            CharCustomizeFailure failed = new()
            {
                Result = (byte)result,
                CharGUID = customizeInfo.CharGUID
            };
            _session.SendPacket(failed);
        }
    }

    private void SendCharDelete(ResponseCodes result)
    {
        DeleteChar response = new()
        {
            Code = result
        };

        _session.SendPacket(response);
    }

    private void SendCharFactionChange(ResponseCodes result, CharRaceOrFactionChangeInfo factionChangeInfo)
    {
        CharFactionChangeResult packet = new()
        {
            Result = result,
            Guid = factionChangeInfo.Guid
        };

        if (result == ResponseCodes.Success)
        {
            packet.Display = new CharFactionChangeResult.CharFactionChangeDisplayInfo
            {
                Name = factionChangeInfo.Name,
                SexID = (byte)factionChangeInfo.SexID,
                Customizations = factionChangeInfo.Customizations,
                RaceID = (byte)factionChangeInfo.RaceID
            };
        }

        _session.SendPacket(packet);
    }

    private void SendCharRename(ResponseCodes result, CharacterRenameInfo renameInfo)
    {
        CharacterRenameResult packet = new()
        {
            Result = result,
            Name = renameInfo.NewName
        };

        if (result == ResponseCodes.Success)
            packet.Guid = renameInfo.Guid;

        _session.SendPacket(packet);
    }

    private void SendSetPlayerDeclinedNamesResult(DeclinedNameResult result, ObjectGuid guid)
    {
        SetPlayerDeclinedNamesResult packet = new()
        {
            ResultCode = result,
            Player = guid
        };

        _session.SendPacket(packet);
    }

    private void SendUndeleteCharacterResponse(CharacterUndeleteResult result, CharacterUndeleteInfo undeleteInfo)
    {
        UndeleteCharacterResponse response = new()
        {
            UndeleteInfo = undeleteInfo,
            Result = result
        };

        _session.SendPacket(response);
    }

    private void SendUndeleteCooldownStatusResponse(uint currentCooldown, uint maxCooldown)
    {
        UndeleteCooldownStatusResponse response = new()
        {
            OnCooldown = (currentCooldown > 0),
            MaxCooldown = maxCooldown,
            CurrentCooldown = currentCooldown
        };

        _session.SendPacket(response);
    }
}

public class LoginQueryHolder : SQLQueryHolder<PlayerLoginQueryLoad>
{
    private readonly uint _accountId;
    private readonly CharacterDatabase _characterDatabase;
    private readonly IConfiguration _configuration;
    private ObjectGuid _guid;

    public LoginQueryHolder(uint accountId, ObjectGuid guid, CharacterDatabase characterDatabase, IConfiguration worldConfig)
    {
        _accountId = accountId;
        _guid = guid;
        _characterDatabase = characterDatabase;
        _configuration = worldConfig;
    }

    public ObjectGuid GetGuid()
    {
        return _guid;
    }

    public void Initialize()
    {
        var lowGuid = _guid.Counter;

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.From, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_CUSTOMIZATIONS);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.Customizations, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_GROUP_MEMBER);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.Group, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_AURAS);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.Auras, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_AURA_EFFECTS);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.AuraEffects, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_AURA_STORED_LOCATIONS);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.AuraStoredLocations, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_SPELL);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.Spells, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_SPELL_FAVORITES);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.SpellFavorites, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_QUESTSTATUS);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.QuestStatus, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_QUESTSTATUS_OBJECTIVES);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.QuestStatusObjectives, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_QUESTSTATUS_OBJECTIVES_CRITERIA);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.QuestStatusObjectivesCriteria, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_QUESTSTATUS_OBJECTIVES_CRITERIA_PROGRESS);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.QuestStatusObjectivesCriteriaProgress, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_QUESTSTATUS_DAILY);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.DailyQuestStatus, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_QUESTSTATUS_WEEKLY);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.WeeklyQuestStatus, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_QUESTSTATUS_MONTHLY);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.MonthlyQuestStatus, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_QUESTSTATUS_SEASONAL);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.SeasonalQuestStatus, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_REPUTATION);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.Reputation, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_INVENTORY);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.Inventory, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_ITEM_INSTANCE_ARTIFACT);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.Artifacts, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_ITEM_INSTANCE_AZERITE);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.Azerite, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_ITEM_INSTANCE_AZERITE_MILESTONE_POWER);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.AzeriteMilestonePowers, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_ITEM_INSTANCE_AZERITE_UNLOCKED_ESSENCE);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.AzeriteUnlockedEssences, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_ITEM_INSTANCE_AZERITE_EMPOWERED);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.AzeriteEmpowered, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHAR_VOID_STORAGE);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.VoidStorage, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_MAIL);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.Mails, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_MAILITEMS);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.MailItems, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_MAILITEMS_ARTIFACT);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.MailItemsArtifact, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_MAILITEMS_AZERITE);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.MailItemsAzerite, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_MAILITEMS_AZERITE_MILESTONE_POWER);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.MailItemsAzeriteMilestonePower, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_MAILITEMS_AZERITE_UNLOCKED_ESSENCE);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.MailItemsAzeriteUnlockedEssence, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_MAILITEMS_AZERITE_EMPOWERED);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.MailItemsAzeriteEmpowered, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_SOCIALLIST);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.SocialList, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_HOMEBIND);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.HomeBind, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_SPELLCOOLDOWNS);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.SpellCooldowns, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_SPELL_CHARGES);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.SpellCharges, stmt);

        if (_configuration.GetDefaultValue("DeclinedNames", false))
        {
            stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_DECLINEDNAMES);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.DeclinedNames, stmt);
        }

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_GUILD_MEMBER);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.Guild, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_ARENAINFO);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.ArenaInfo, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_ACHIEVEMENTS);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.Achievements, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_CRITERIAPROGRESS);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.CriteriaProgress, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_EQUIPMENTSETS);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.EquipmentSets, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_TRANSMOG_OUTFITS);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.TransmogOutfits, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHAR_CUF_PROFILES);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.CufProfiles, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_BGDATA);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.BgData, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_GLYPHS);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.Glyphs, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_TALENTS);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.Talents, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_PVP_TALENTS);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.PvpTalents, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_PLAYER_ACCOUNT_DATA);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.AccountData, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_SKILLS);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.Skills, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_RANDOMBG);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.RandomBg, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_BANNED);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.Banned, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_QUESTSTATUSREW);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.QuestStatusRew, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_ACCOUNT_INSTANCELOCKTIMES);
        stmt.AddValue(0, _accountId);
        SetQuery(PlayerLoginQueryLoad.InstanceLockTimes, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_PLAYER_CURRENCY);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.Currency, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CORPSE_LOCATION);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.CorpseLocation, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHAR_PETS);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.PetSlots, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_GARRISON);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.Garrison, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_GARRISON_BLUEPRINTS);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.GarrisonBlueprints, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_GARRISON_BUILDINGS);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.GarrisonBuildings, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_GARRISON_FOLLOWERS);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.GarrisonFollowers, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_GARRISON_FOLLOWER_ABILITIES);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.GarrisonFollowerAbilities, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHAR_TRAIT_ENTRIES);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.TraitEntries, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHAR_TRAIT_CONFIGS);
        stmt.AddValue(0, lowGuid);
        SetQuery(PlayerLoginQueryLoad.TraitConfigs, stmt);
    }

    private uint GetAccountId()
    {
        return _accountId;
    }
}

internal class EnumCharactersQueryHolder : SQLQueryHolder<EnumCharacterQueryLoad>
{
    private readonly CharacterDatabase _characterDatabase;
    private bool _isDeletedCharacters;

    public EnumCharactersQueryHolder(CharacterDatabase characterDatabase)
    {
        _characterDatabase = characterDatabase;
    }

    public bool IsDeletedCharacters => _isDeletedCharacters;

    public bool Initialize(uint accountId, bool withDeclinedNames, bool isDeletedCharacters)
    {
        _isDeletedCharacters = isDeletedCharacters;

        CharStatements[][] statements =
        {
            new[]
            {
                CharStatements.SEL_ENUM, CharStatements.SEL_ENUM_DECLINED_NAME, CharStatements.SEL_ENUM_CUSTOMIZATIONS
            },
            new[]
            {
                CharStatements.SEL_UNDELETE_ENUM, CharStatements.SEL_UNDELETE_ENUM_DECLINED_NAME, CharStatements.SEL_UNDELETE_ENUM_CUSTOMIZATIONS
            }
        };

        var stmt = _characterDatabase.GetPreparedStatement(statements[isDeletedCharacters ? 1 : 0][withDeclinedNames ? 1 : 0]);
        stmt.AddValue(0, accountId);
        SetQuery(EnumCharacterQueryLoad.Characters, stmt);

        stmt = _characterDatabase.GetPreparedStatement(statements[isDeletedCharacters ? 1 : 0][2]);
        stmt.AddValue(0, accountId);
        SetQuery(EnumCharacterQueryLoad.Customizations, stmt);

        return true;
    }
}