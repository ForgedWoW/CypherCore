// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Forged.RealmServer.DataStorage;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Networking;
using Forged.RealmServer.Networking.Packets;
using Forged.RealmServer.Scripting.Interfaces.IPlayer;
using Game.Common.Handlers;
using Microsoft.Extensions.Configuration;
using Serilog;
using Forged.RealmServer.Scripting;
using Framework.Util;

namespace Forged.RealmServer;

public class CharacterHandler : IWorldSessionHandler
{
    private readonly WorldSession _session;
    private readonly CliDB _cliDB;
    private readonly CollectionMgr _collectionMgr;
    private readonly IConfiguration _configuration;
    private readonly WorldConfig _worldConfig;
    private readonly CharacterDatabase _characterDatabase;
    private readonly ScriptManager _scriptManager;
    private readonly GameTime _gameTime;
    private readonly LoginDatabase _loginDatabase;

    public CharacterHandler(WorldSession session, CliDB cliDB, CollectionMgr collectionMgr, IConfiguration configuration, WorldConfig worldConfig, 
		CharacterDatabase characterDatabase, ScriptManager scriptManager, GameTime gameTime, LoginDatabase loginDatabase)
    {
        _session = session;
        _cliDB = cliDB;
        _collectionMgr = collectionMgr;
        _configuration = configuration;
        _worldConfig = worldConfig;
        _characterDatabase = characterDatabase;
        _scriptManager = scriptManager;
        _gameTime = gameTime;
        _loginDatabase = loginDatabase;
    }

    public bool MeetsChrCustomizationReq(ChrCustomizationReqRecord req, PlayerClass playerClass, bool checkRequiredDependentChoices, List<ChrCustomizationChoice> selectedChoices)
	{
		if (!req.GetFlags().HasFlag(ChrCustomizationReqFlag.HasRequirements))
			return true;

		if (req.ClassMask != 0 && (req.ClassMask & (1 << ((int)playerClass - 1))) == 0)
			return false;

		if (req.AchievementID != 0 /*&& !HasAchieved(req->AchievementID)*/)
			return false;

		if (req.ItemModifiedAppearanceID != 0 && !_collectionMgr.HasItemAppearance(req.ItemModifiedAppearanceID).PermAppearance)
			return false;

		if (req.QuestID != 0)
		{
			if (!_session.Player)
				return false;

			if (!_session.Player.IsQuestRewarded((uint)req.QuestID))
				return false;
		}

		if (checkRequiredDependentChoices)
		{
			var requiredChoices = Global.DB2Mgr.GetRequiredCustomizationChoices(req.Id);

			if (requiredChoices != null)
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
		}

		return true;
	}

	public bool ValidateAppearance(Race race, PlayerClass playerClass, Gender gender, List<ChrCustomizationChoice> customizations)
	{
		var options = Global.DB2Mgr.GetCustomiztionOptions(race, gender);

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
			var customizationOptionData = options.Find(option => { return option.Id == playerChoice.ChrCustomizationOptionID; });

			// option not found for race/gender combination
			if (customizationOptionData == null)
				return false;

			var req = _cliDB.ChrCustomizationReqStorage.LookupByKey((uint)customizationOptionData.ChrCustomizationReqID);

			if (req != null)
				if (!MeetsChrCustomizationReq(req, playerClass, false, customizations))
					return false;

			var choicesForOption = Global.DB2Mgr.GetCustomiztionChoices(playerChoice.ChrCustomizationOptionID);

			if (choicesForOption.Empty())
				return false;

			var customizationChoiceData = choicesForOption.Find(choice => { return choice.Id == playerChoice.ChrCustomizationChoiceID; });

			// choice not found for option
			if (customizationChoiceData == null)
				return false;

			var reqEntry = _cliDB.ChrCustomizationReqStorage.LookupByKey(customizationChoiceData.ChrCustomizationReqID);

			if (reqEntry != null)
				if (!MeetsChrCustomizationReq(reqEntry, playerClass, true, customizations))
					return false;
		}

		return true;
	}

	public void HandleContinuePlayerLogin()
	{
		if (_session.PlayerLoading.IsEmpty || _session.Player)
		{
			_session.KickPlayer("WorldSession::HandleContinuePlayerLogin incorrect player state when logging in");

			return;
		}

		LoginQueryHolder holder = new(_session.AccountId, _session.PlayerLoading, _characterDatabase, _worldConfig);
		holder.Initialize();

		_session.SendPacket(new ResumeComms(ConnectionType.Instance));

		_session.AddQueryHolderCallback(_characterDatabase.DelayQueryHolder(holder)).AfterComplete(holder => HandlePlayerLogin((LoginQueryHolder)holder));
	}

	public void HandlePlayerLogin(LoginQueryHolder holder)
	{
		var playerGuid = holder.GetGuid();

		Player pCurrChar = new(_session);

		if (!pCurrChar.LoadFromDB(playerGuid, holder))
		{
            _session.Player = null;
			_session.KickPlayer("WorldSession::HandlePlayerLogin Player::LoadFromDB failed");
			_session.PlayerLoading.Clear();

			return;
		}

		pCurrChar.SetVirtualPlayerRealm(Global.WorldMgr.VirtualRealmAddress);

		_session.SendAccountDataTimes(ObjectGuid.Empty, AccountDataTypes.GlobalCacheMask);
		_session.SendTutorialsData();

		pCurrChar.MotionMaster.Initialize();
		pCurrChar.SendDungeonDifficulty();

		LoginVerifyWorld loginVerifyWorld = new();
		loginVerifyWorld.MapID = (int)pCurrChar.Location.MapId;
		loginVerifyWorld.Pos = pCurrChar.Location;
		_session.SendPacket(loginVerifyWorld);

		// load player specific part before send times
		_session.LoadAccountData(holder.GetResult(PlayerLoginQueryLoad.AccountData), AccountDataTypes.PerCharacterCacheMask);

		_session.SendAccountDataTimes(playerGuid, AccountDataTypes.AllAccountDataCacheMask);

		SendFeatureSystemStatus();

		MOTD motd = new();
		motd.Text = Global.WorldMgr.Motd;
		_session.SendPacket(motd);

		_session.SendSetTimeZoneInformation();

		// Send PVPSeason
		{
			SeasonInfo seasonInfo = new();
			seasonInfo.PreviousArenaSeason = (_worldConfig.GetIntValue(WorldCfg.ArenaSeasonId) - (_worldConfig.GetBoolValue(WorldCfg.ArenaSeasonInProgress) ? 1 : 0));

			if (_worldConfig.GetBoolValue(WorldCfg.ArenaSeasonInProgress))
				seasonInfo.CurrentArenaSeason = _worldConfig.GetIntValue(WorldCfg.ArenaSeasonId);

			_session.SendPacket(seasonInfo);
		}

		var resultGuild = holder.GetResult(PlayerLoginQueryLoad.Guild);

		if (!resultGuild.IsEmpty())
		{
			pCurrChar.SetInGuild(resultGuild.Read<uint>(0));
			pCurrChar.SetGuildRank(resultGuild.Read<byte>(1));
			var guild = Global.GuildMgr.GetGuildById(pCurrChar.GuildId);

			if (guild)
				pCurrChar.GuildLevel = guild.GetLevel();
		}
		else if (pCurrChar.GuildId != 0)
		{
			pCurrChar.SetInGuild(0);
			pCurrChar.SetGuildRank(0);
			pCurrChar.GuildLevel = 0;
		}

		// Send stable contents to display icons on Call Pet spells
		if (pCurrChar.HasSpell(SharedConst.CallPetSpellId))
            _session.SendStablePet(ObjectGuid.Empty);

		pCurrChar.Session.BattlePetMgr.SendJournalLockStatus();

		pCurrChar.SendInitialPacketsBeforeAddToMap();

		//Show cinematic at the first time that player login
		if (pCurrChar.Cinematic == 0)
		{
			pCurrChar.Cinematic = 1;
			var playerInfo = Global.ObjectMgr.GetPlayerInfo(pCurrChar.Race, pCurrChar.Class);

			if (playerInfo != null)
				switch (pCurrChar.CreateMode)
				{
					case PlayerCreateMode.Normal:
						if (playerInfo.IntroMovieId.HasValue)
							pCurrChar.SendMovieStart(playerInfo.IntroMovieId.Value);
						else if (playerInfo.IntroSceneId.HasValue)
							pCurrChar.SceneMgr.PlayScene(playerInfo.IntroSceneId.Value);
						else if (_cliDB.ChrClassesStorage.TryGetValue((uint)pCurrChar.Class, out var chrClassesRecord) && chrClassesRecord.CinematicSequenceID != 0)
							pCurrChar.SendCinematicStart(chrClassesRecord.CinematicSequenceID);
						else if (_cliDB.ChrRacesStorage.TryGetValue((uint)pCurrChar.Race, out var chrRacesRecord) && chrRacesRecord.CinematicSequenceID != 0)
							pCurrChar.SendCinematicStart(chrRacesRecord.CinematicSequenceID);

						break;
					case PlayerCreateMode.NPE:
						if (playerInfo.IntroSceneIdNpe.HasValue)
							pCurrChar.SceneMgr.PlayScene(playerInfo.IntroSceneIdNpe.Value);

						break;
					default:
						break;
				}
		}

		if (!pCurrChar.Map.AddPlayerToMap(pCurrChar))
		{
			var at = Global.ObjectMgr.GetGoBackTrigger(pCurrChar.Location.MapId);

			if (at != null)
				pCurrChar.TeleportTo(at.target_mapId, at.target_X, at.target_Y, at.target_Z, pCurrChar.Location.Orientation);
			else
				pCurrChar.TeleportTo(pCurrChar.Homebind);
		}

		Global.ObjAccessor.AddObject(pCurrChar);

		if (pCurrChar.GuildId != 0)
		{
			var guild = Global.GuildMgr.GetGuildById(pCurrChar.GuildId);

			if (guild)
			{
				guild.SendLoginInfo(_session);
			}
			else
			{
				// remove wrong guild data
				Log.Logger.Error(
							"Player {0} ({1}) marked as member of not existing guild (id: {2}), removing guild membership for player.",
							pCurrChar.GetName(),
							pCurrChar.GUID.ToString(),
							pCurrChar.GuildId);

				pCurrChar.SetInGuild(0);
			}
		}

		pCurrChar.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Login);

		pCurrChar.SendInitialPacketsAfterAddToMap();

		var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_CHAR_ONLINE);
		stmt.AddValue(0, pCurrChar.GUID.Counter);
		_characterDatabase.Execute(stmt);

		stmt = _loginDatabase.GetPreparedStatement(LoginStatements.UPD_ACCOUNT_ONLINE);
		stmt.AddValue(0, _session.AccountId);
		_loginDatabase.Execute(stmt);

		pCurrChar.SetInGameTime(_gameTime.GetGameTimeMS);

		// announce group about member online (must be after add to player list to receive announce to self)
		var group = pCurrChar.Group;

		if (group)
		{
			group.SendUpdate();

			if (group.LeaderGUID == pCurrChar.GUID)
				group.StopLeaderOfflineTimer();
		}

		// friend status
		Global.SocialMgr.SendFriendStatus(pCurrChar, FriendsResult.Online, pCurrChar.GUID, true);

		// Place character in world (and load zone) before some object loading
		pCurrChar.LoadCorpse(holder.GetResult(PlayerLoginQueryLoad.CorpseLocation));

		// setting Ghost+speed if dead
		if (pCurrChar.DeathState == DeathState.Dead)
		{
			// not blizz like, we must correctly save and load player instead...
			if (pCurrChar.Race == Race.NightElf && !pCurrChar.HasAura(20584))
				pCurrChar.CastSpell(pCurrChar, 20584, new CastSpellExtraArgs(true)); // auras SPELL_AURA_INCREASE_SPEED(+speed in wisp form), SPELL_AURA_INCREASE_SWIM_SPEED(+swim speed in wisp form), SPELL_AURA_TRANSFORM (to wisp form)

			if (!pCurrChar.HasAura(8326))
				pCurrChar.CastSpell(pCurrChar, 8326, new CastSpellExtraArgs(true)); // auras SPELL_AURA_GHOST, SPELL_AURA_INCREASE_SPEED(why?), SPELL_AURA_INCREASE_SWIM_SPEED(why?)

			pCurrChar.SetWaterWalking(true);
		}

		pCurrChar.ContinueTaxiFlight();

		// reset for all pets before pet loading
		if (pCurrChar.HasAtLoginFlag(AtLoginFlags.ResetPetTalents))
		{
			// Delete all of the player's pet spells
			var stmtSpells = _characterDatabase.GetPreparedStatement(CharStatements.DEL_ALL_PET_SPELLS_BY_OWNER);
			stmtSpells.AddValue(0, pCurrChar.GUID.Counter);
			_characterDatabase.Execute(stmtSpells);

			// Then reset all of the player's pet specualizations
			var stmtSpec = _characterDatabase.GetPreparedStatement(CharStatements.UPD_PET_SPECS_BY_OWNER);
			stmtSpec.AddValue(0, pCurrChar.GUID.Counter);
			_characterDatabase.Execute(stmtSpec);
		}

		// Load pet if any (if player not alive and in taxi flight or another then pet will remember as temporary unsummoned)
		pCurrChar.ResummonPetTemporaryUnSummonedIfAny();

		// Set FFA PvP for non GM in non-rest mode
		if (Global.WorldMgr.IsFFAPvPRealm && !pCurrChar.IsGameMaster && !pCurrChar.HasPlayerFlag(PlayerFlags.Resting))
			pCurrChar.SetPvpFlag(UnitPVPStateFlags.FFAPvp);

		if (pCurrChar.HasPlayerFlag(PlayerFlags.ContestedPVP))
			pCurrChar.SetContestedPvP();

		// Apply at_login requests
		if (pCurrChar.HasAtLoginFlag(AtLoginFlags.ResetSpells))
		{
			pCurrChar.ResetSpells();
            _session.SendNotification(CypherStrings.ResetSpells);
		}

		if (pCurrChar.HasAtLoginFlag(AtLoginFlags.ResetTalents))
		{
			pCurrChar.ResetTalents(true);
			pCurrChar.ResetTalentSpecialization();
			pCurrChar.SendTalentsInfoData(); // original talents send already in to SendInitialPacketsBeforeAddToMap, resend reset state
            _session.SendNotification(CypherStrings.ResetTalents);
		}

		if (pCurrChar.HasAtLoginFlag(AtLoginFlags.FirstLogin))
		{
			pCurrChar.RemoveAtLoginFlag(AtLoginFlags.FirstLogin);

			var info = Global.ObjectMgr.GetPlayerInfo(pCurrChar.Race, pCurrChar.Class);

			foreach (var spellId in info.CastSpells[(int)pCurrChar.CreateMode])
				pCurrChar.CastSpell(pCurrChar, spellId, new CastSpellExtraArgs(true));

			// start with every map explored
			if (_worldConfig.GetBoolValue(WorldCfg.StartAllExplored))
				for (uint i = 0; i < PlayerConst.ExploredZonesSize; i++)
					pCurrChar.AddExploredZones(i, 0xFFFFFFFFFFFFFFFF);

			//Reputations if "StartAllReputation" is enabled
			if (_worldConfig.GetBoolValue(WorldCfg.StartAllRep))
			{
				var repMgr = pCurrChar.ReputationMgr;
				repMgr.SetOneFactionReputation(_cliDB.FactionStorage.LookupByKey(942u), 42999, false);  // Cenarion Expedition
				repMgr.SetOneFactionReputation(_cliDB.FactionStorage.LookupByKey(935u), 42999, false);  // The Sha'tar
				repMgr.SetOneFactionReputation(_cliDB.FactionStorage.LookupByKey(936u), 42999, false);  // Shattrath City
				repMgr.SetOneFactionReputation(_cliDB.FactionStorage.LookupByKey(1011u), 42999, false); // Lower City
				repMgr.SetOneFactionReputation(_cliDB.FactionStorage.LookupByKey(970u), 42999, false);  // Sporeggar
				repMgr.SetOneFactionReputation(_cliDB.FactionStorage.LookupByKey(967u), 42999, false);  // The Violet Eye
				repMgr.SetOneFactionReputation(_cliDB.FactionStorage.LookupByKey(989u), 42999, false);  // Keepers of Time
				repMgr.SetOneFactionReputation(_cliDB.FactionStorage.LookupByKey(932u), 42999, false);  // The Aldor
				repMgr.SetOneFactionReputation(_cliDB.FactionStorage.LookupByKey(934u), 42999, false);  // The Scryers
				repMgr.SetOneFactionReputation(_cliDB.FactionStorage.LookupByKey(1038u), 42999, false); // Ogri'la
				repMgr.SetOneFactionReputation(_cliDB.FactionStorage.LookupByKey(1077u), 42999, false); // Shattered Sun Offensive
				repMgr.SetOneFactionReputation(_cliDB.FactionStorage.LookupByKey(1106u), 42999, false); // Argent Crusade
				repMgr.SetOneFactionReputation(_cliDB.FactionStorage.LookupByKey(1104u), 42999, false); // Frenzyheart Tribe
				repMgr.SetOneFactionReputation(_cliDB.FactionStorage.LookupByKey(1090u), 42999, false); // Kirin Tor
				repMgr.SetOneFactionReputation(_cliDB.FactionStorage.LookupByKey(1098u), 42999, false); // Knights of the Ebon Blade
				repMgr.SetOneFactionReputation(_cliDB.FactionStorage.LookupByKey(1156u), 42999, false); // The Ashen Verdict
				repMgr.SetOneFactionReputation(_cliDB.FactionStorage.LookupByKey(1073u), 42999, false); // The Kalu'ak
				repMgr.SetOneFactionReputation(_cliDB.FactionStorage.LookupByKey(1105u), 42999, false); // The Oracles
				repMgr.SetOneFactionReputation(_cliDB.FactionStorage.LookupByKey(1119u), 42999, false); // The Sons of Hodir
				repMgr.SetOneFactionReputation(_cliDB.FactionStorage.LookupByKey(1091u), 42999, false); // The Wyrmrest Accord

				// Factions depending on team, like cities and some more stuff
				switch (pCurrChar.Team)
				{
					case TeamFaction.Alliance:
						repMgr.SetOneFactionReputation(_cliDB.FactionStorage.LookupByKey(72u), 42999, false);   // Stormwind
						repMgr.SetOneFactionReputation(_cliDB.FactionStorage.LookupByKey(47u), 42999, false);   // Ironforge
						repMgr.SetOneFactionReputation(_cliDB.FactionStorage.LookupByKey(69u), 42999, false);   // Darnassus
						repMgr.SetOneFactionReputation(_cliDB.FactionStorage.LookupByKey(930u), 42999, false);  // Exodar
						repMgr.SetOneFactionReputation(_cliDB.FactionStorage.LookupByKey(730u), 42999, false);  // Stormpike Guard
						repMgr.SetOneFactionReputation(_cliDB.FactionStorage.LookupByKey(978u), 42999, false);  // Kurenai
						repMgr.SetOneFactionReputation(_cliDB.FactionStorage.LookupByKey(54u), 42999, false);   // Gnomeregan Exiles
						repMgr.SetOneFactionReputation(_cliDB.FactionStorage.LookupByKey(946u), 42999, false);  // Honor Hold
						repMgr.SetOneFactionReputation(_cliDB.FactionStorage.LookupByKey(1037u), 42999, false); // Alliance Vanguard
						repMgr.SetOneFactionReputation(_cliDB.FactionStorage.LookupByKey(1068u), 42999, false); // Explorers' League
						repMgr.SetOneFactionReputation(_cliDB.FactionStorage.LookupByKey(1126u), 42999, false); // The Frostborn
						repMgr.SetOneFactionReputation(_cliDB.FactionStorage.LookupByKey(1094u), 42999, false); // The Silver Covenant
						repMgr.SetOneFactionReputation(_cliDB.FactionStorage.LookupByKey(1050u), 42999, false); // Valiance Expedition

						break;
					case TeamFaction.Horde:
						repMgr.SetOneFactionReputation(_cliDB.FactionStorage.LookupByKey(76u), 42999, false);   // Orgrimmar
						repMgr.SetOneFactionReputation(_cliDB.FactionStorage.LookupByKey(68u), 42999, false);   // Undercity
						repMgr.SetOneFactionReputation(_cliDB.FactionStorage.LookupByKey(81u), 42999, false);   // Thunder Bluff
						repMgr.SetOneFactionReputation(_cliDB.FactionStorage.LookupByKey(911u), 42999, false);  // Silvermoon City
						repMgr.SetOneFactionReputation(_cliDB.FactionStorage.LookupByKey(729u), 42999, false);  // Frostwolf Clan
						repMgr.SetOneFactionReputation(_cliDB.FactionStorage.LookupByKey(941u), 42999, false);  // The Mag'har
						repMgr.SetOneFactionReputation(_cliDB.FactionStorage.LookupByKey(530u), 42999, false);  // Darkspear Trolls
						repMgr.SetOneFactionReputation(_cliDB.FactionStorage.LookupByKey(947u), 42999, false);  // Thrallmar
						repMgr.SetOneFactionReputation(_cliDB.FactionStorage.LookupByKey(1052u), 42999, false); // Horde Expedition
						repMgr.SetOneFactionReputation(_cliDB.FactionStorage.LookupByKey(1067u), 42999, false); // The Hand of Vengeance
						repMgr.SetOneFactionReputation(_cliDB.FactionStorage.LookupByKey(1124u), 42999, false); // The Sunreavers
						repMgr.SetOneFactionReputation(_cliDB.FactionStorage.LookupByKey(1064u), 42999, false); // The Taunka
						repMgr.SetOneFactionReputation(_cliDB.FactionStorage.LookupByKey(1085u), 42999, false); // Warsong Offensive

						break;
					default:
						break;
				}

				repMgr.SendState(null);
			}
		}

		// show time before shutdown if shutdown planned.
		if (Global.WorldMgr.IsShuttingDown)
			Global.WorldMgr.ShutdownMsg(true, pCurrChar);

		if (_worldConfig.GetBoolValue(WorldCfg.AllTaxiPaths))
			pCurrChar.SetTaxiCheater(true);

		if (pCurrChar.IsGameMaster)
			_session.SendNotification(CypherStrings.GmOn);

		var IP_str = _session.RemoteAddress;
		Log.Logger.Debug($"Account: {_session.AccountId} (IP: {_session.RemoteAddress}) Login Character: [{pCurrChar.GetName()}] ({pCurrChar.GUID}) Level: {pCurrChar.Level}, XP: {_session.Player.XP}/{_session.Player.XPForNextLevel} ({_session.Player.XPForNextLevel - _session.Player.XP} left)");

		if (!pCurrChar.IsStandState && !pCurrChar.HasUnitState(UnitState.Stunned))
			pCurrChar.SetStandState(UnitStandStateType.Stand);

		pCurrChar.UpdateAverageItemLevelTotal();
		pCurrChar.UpdateAverageItemLevelEquipped();

		_session.PlayerLoading.Clear();

		// Handle Login-Achievements (should be handled after loading)
		_session.Player.UpdateCriteria(CriteriaType.Login, 1);

		_scriptManager.ForEach<IPlayerOnLogin>(p => p.OnLogin(pCurrChar));
	}

	public void AbortLogin(LoginFailureReason reason)
	{
		if (_session.PlayerLoading.IsEmpty || _session.Player)
		{
			_session.KickPlayer("WorldSession::AbortLogin incorrect player state when logging in");

			return;
		}

		_session.PlayerLoading.Clear();
		_session.SendPacket(new CharacterLoginFailed(reason));
	}

	public void SendFeatureSystemStatus()
	{
		FeatureSystemStatus features = new();

		// START OF DUMMY VALUES
		features.ComplaintStatus = (byte)ComplaintStatus.EnabledWithAutoIgnore;
		features.TwitterPostThrottleLimit = 60;
		features.TwitterPostThrottleCooldown = 20;
		features.CfgRealmID = 2;
		features.CfgRealmRecID = 0;
		features.TokenPollTimeSeconds = 300;
		features.VoiceEnabled = false;
		features.BrowserEnabled = false; // Has to be false, otherwise client will crash if "Customer Support" is opened

		EuropaTicketConfig europaTicketSystemStatus = new();
		europaTicketSystemStatus.ThrottleState.MaxTries = 10;
		europaTicketSystemStatus.ThrottleState.PerMilliseconds = 60000;
		europaTicketSystemStatus.ThrottleState.TryCount = 1;
		europaTicketSystemStatus.ThrottleState.LastResetTimeBeforeNow = 111111;
		features.TutorialsEnabled = true;
		features.NPETutorialsEnabled = true;
		// END OF DUMMY VALUES

		europaTicketSystemStatus.TicketsEnabled = _worldConfig.GetBoolValue(WorldCfg.SupportTicketsEnabled);
		europaTicketSystemStatus.BugsEnabled = _worldConfig.GetBoolValue(WorldCfg.SupportBugsEnabled);
		europaTicketSystemStatus.ComplaintsEnabled = _worldConfig.GetBoolValue(WorldCfg.SupportComplaintsEnabled);
		europaTicketSystemStatus.SuggestionsEnabled = _worldConfig.GetBoolValue(WorldCfg.SupportSuggestionsEnabled);

		features.EuropaTicketSystemStatus = europaTicketSystemStatus;

		features.CharUndeleteEnabled = _worldConfig.GetBoolValue(WorldCfg.FeatureSystemCharacterUndeleteEnabled);
		features.BpayStoreEnabled = _worldConfig.GetBoolValue(WorldCfg.FeatureSystemBpayStoreEnabled);
		features.WarModeFeatureEnabled = _worldConfig.GetBoolValue(WorldCfg.FeatureSystemWarModeEnabled);
		features.IsMuted = !_session.CanSpeak;


		features.TextToSpeechFeatureEnabled = false;

		_session.SendPacket(features);
	}

	[WorldPacketHandler(ClientOpcodes.EnumCharacters, Status = SessionStatus.Authed)]
	void HandleCharEnum(EnumCharacters charEnum)
	{
		// remove expired bans
		var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_EXPIRED_BANS);
		_characterDatabase.Execute(stmt);

		// get all the data necessary for loading all characters (along with their pets) on the account
		EnumCharactersQueryHolder holder = new();

		if (!holder.Initialize(_session.AccountId, _worldConfig.GetBoolValue(WorldCfg.DeclinedNamesUsed), false, _characterDatabase))
		{
			HandleCharEnum(holder);

			return;
		}

		_session.AddQueryHolderCallback(_characterDatabase.DelayQueryHolder(holder)).AfterComplete(result => HandleCharEnum((EnumCharactersQueryHolder)result));
	}

	void HandleCharEnum(EnumCharactersQueryHolder holder)
	{
		EnumCharactersResult charResult = new();
		charResult.Success = true;
		charResult.IsDeletedCharacters = holder.IsDeletedCharacters();
		charResult.DisabledClassesMask = _worldConfig.GetUIntValue(WorldCfg.CharacterCreatingDisabledClassmask);

		if (!charResult.IsDeletedCharacters)
            _session.LegitCharacters.Clear();

		MultiMap<ulong, ChrCustomizationChoice> customizations = new();
		var customizationsResult = holder.GetResult(EnumCharacterQueryLoad.Customizations);

		if (!customizationsResult.IsEmpty())
			do
			{
				ChrCustomizationChoice choice = new();
				choice.ChrCustomizationOptionID = customizationsResult.Read<uint>(1);
				choice.ChrCustomizationChoiceID = customizationsResult.Read<uint>(2);
				customizations.Add(customizationsResult.Read<ulong>(0), choice);
			} while (customizationsResult.NextRow());

		var result = holder.GetResult(EnumCharacterQueryLoad.Characters);

		if (!result.IsEmpty())
			do
			{
				EnumCharactersResult.CharacterInfo charInfo = new(result.GetFields());

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
                        _session.LegitCharacters.Add(charInfo.Guid);
				}

				if (!Global.CharacterCacheStorage.HasCharacterCacheEntry(charInfo.Guid)) // This can happen if characters are inserted into the database manually. Core hasn't loaded name data yet.
					Global.CharacterCacheStorage.AddCharacterCacheEntry(charInfo.Guid, _session.AccountId, charInfo.Name, charInfo.SexId, charInfo.RaceId, (byte)charInfo.ClassId, charInfo.ExperienceLevel, false);

				charResult.MaxCharacterLevel = Math.Max(charResult.MaxCharacterLevel, charInfo.ExperienceLevel);

				charResult.Characters.Add(charInfo);
			} while (result.NextRow() && charResult.Characters.Count < 200);

		charResult.IsAlliedRacesCreationAllowed = _session.CanAccessAlliedRaces();

		foreach (var requirement in Global.ObjectMgr.GetRaceUnlockRequirements())
		{
			EnumCharactersResult.RaceUnlock raceUnlock = new();
			raceUnlock.RaceID = requirement.Key;
			raceUnlock.HasExpansion = _configuration.GetDefaultValue("character.EnforceRaceAndClassExpansions", true) ? (byte)_session.AccountExpansion >= requirement.Value.Expansion : true;
			raceUnlock.HasAchievement = (_worldConfig.GetBoolValue(WorldCfg.CharacterCreatingDisableAlliedRaceAchievementRequirement) ? true : requirement.Value.AchievementId != 0 ? false : true); // TODO: fix false here for actual check of criteria.

			charResult.RaceUnlockData.Add(raceUnlock);
		}

		_session.SendPacket(charResult);
	}

	[WorldPacketHandler(ClientOpcodes.EnumCharactersDeletedByClient, Status = SessionStatus.Authed)]
	void HandleCharUndeleteEnum(EnumCharacters enumCharacters)
	{
		// get all the data necessary for loading all undeleted characters (along with their pets) on the account
		EnumCharactersQueryHolder holder = new();

		if (!holder.Initialize(_session.AccountId, _worldConfig.GetBoolValue(WorldCfg.DeclinedNamesUsed), true, _characterDatabase))
		{
			HandleCharEnum(holder);

			return;
		}

		_session.AddQueryHolderCallback(_characterDatabase.DelayQueryHolder(holder)).AfterComplete(result => HandleCharEnum((EnumCharactersQueryHolder)result));
	}

	void HandleCharUndeleteEnumCallback(SQLResult result)
	{
		EnumCharactersResult charEnum = new();
		charEnum.Success = true;
		charEnum.IsDeletedCharacters = true;
		charEnum.DisabledClassesMask = _worldConfig.GetUIntValue(WorldCfg.CharacterCreatingDisabledClassmask);

		if (!result.IsEmpty())
			do
			{
				EnumCharactersResult.CharacterInfo charInfo = new(result.GetFields());

				Log.Logger.Information("Loading undeleted char guid {0} from account {1}.", charInfo.Guid.ToString(), _session.AccountId);

				if (!Global.CharacterCacheStorage.HasCharacterCacheEntry(charInfo.Guid)) // This can happen if characters are inserted into the database manually. Core hasn't loaded name data yet.
					Global.CharacterCacheStorage.AddCharacterCacheEntry(charInfo.Guid, _session.AccountId, charInfo.Name, charInfo.SexId, charInfo.RaceId, (byte)charInfo.ClassId, charInfo.ExperienceLevel, true);

				charEnum.Characters.Add(charInfo);
			} while (result.NextRow());

		_session.SendPacket(charEnum);
	}

	[WorldPacketHandler(ClientOpcodes.CreateCharacter, Status = SessionStatus.Authed)]
	void HandleCharCreate(CreateCharacter charCreate)
	{
		if (!_session.HasPermission(RBACPermissions.SkipCheckCharacterCreationTeammask))
		{
			var mask = _worldConfig.GetIntValue(WorldCfg.CharacterCreatingDisabled);

			if (mask != 0)
			{
				var disabled = false;

				var team = Player.TeamIdForRace(charCreate.CreateInfo.RaceId);

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

		var classEntry = _cliDB.ChrClassesStorage.LookupByKey((uint)charCreate.CreateInfo.ClassId);

		if (classEntry == null)
		{
			Log.Logger.Error("Class ({0}) not found in DBC while creating new char for account (ID: {1}): wrong DBC files or cheater?", charCreate.CreateInfo.ClassId, _session.AccountId);
			SendCharCreate(ResponseCodes.CharCreateFailed);

			return;
		}

		var raceEntry = _cliDB.ChrRacesStorage.LookupByKey((uint)charCreate.CreateInfo.RaceId);

		if (raceEntry == null)
		{
			Log.Logger.Error("Race ({0}) not found in DBC while creating new char for account (ID: {1}): wrong DBC files or cheater?", charCreate.CreateInfo.RaceId, _session.AccountId);
			SendCharCreate(ResponseCodes.CharCreateFailed);

			return;
		}

		if (_configuration.GetDefaultValue("character.EnforceRaceAndClassExpansions", true))
		{
			// prevent character creating Expansion race without Expansion account
			var raceExpansionRequirement = Global.ObjectMgr.GetRaceUnlockRequirement(charCreate.CreateInfo.RaceId);

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
			var raceClassExpansionRequirement = Global.ObjectMgr.GetClassExpansionRequirement(charCreate.CreateInfo.RaceId, charCreate.CreateInfo.ClassId);

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
				var classExpansionRequirement = Global.ObjectMgr.GetClassExpansionRequirementFallback((byte)charCreate.CreateInfo.ClassId);

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

			var raceMaskDisabled = _worldConfig.GetUInt64Value(WorldCfg.CharacterCreatingDisabledRacemask);

			if (Convert.ToBoolean((ulong)SharedConst.GetMaskForRace(charCreate.CreateInfo.RaceId) & raceMaskDisabled))
			{
				SendCharCreate(ResponseCodes.CharCreateDisabled);

				return;
			}
		}

		if (!_session.HasPermission(RBACPermissions.SkipCheckCharacterCreationClassmask))
		{
			var classMaskDisabled = _worldConfig.GetIntValue(WorldCfg.CharacterCreatingDisabledClassmask);

			if (Convert.ToBoolean((1 << ((int)charCreate.CreateInfo.ClassId - 1)) & classMaskDisabled))
			{
				SendCharCreate(ResponseCodes.CharCreateDisabled);

				return;
			}
		}

		// prevent character creating with invalid name
		if (!ObjectManager.NormalizePlayerName(ref charCreate.CreateInfo.Name))
		{
			Log.Logger.Error("Account:[{0}] but tried to Create character with empty [name] ", _session.AccountId);
			SendCharCreate(ResponseCodes.CharNameNoName);

			return;
		}

		// check name limitations
		var res = ObjectManager.CheckPlayerName(charCreate.CreateInfo.Name, _session.SessionDbcLocale, true);

		if (res != ResponseCodes.CharNameSuccess)
		{
			SendCharCreate(res);

			return;
		}

		if (!_session.HasPermission(RBACPermissions.SkipCheckCharacterCreationReservedname) && Global.ObjectMgr.IsReservedName(charCreate.CreateInfo.Name))
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

										if (acctCharCount >= _worldConfig.GetUIntValue(WorldCfg.CharactersPerAccount))
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

											if (createInfo.CharCount >= _worldConfig.GetIntValue(WorldCfg.CharactersPerRealm))
											{
												SendCharCreate(ResponseCodes.CharCreateServerLimit);

												return;
											}
										}

										var demonHunterReqLevel = _worldConfig.GetIntValue(WorldCfg.CharacterCreatingMinLevelForDemonHunter);
										var hasDemonHunterReqLevel = demonHunterReqLevel == 0;
										var evokerReqLevel = _worldConfig.GetUIntValue(WorldCfg.CharacterCreatingMinLevelForEvoker);
										var hasEvokerReqLevel = (evokerReqLevel == 0);
										var allowTwoSideAccounts = !Global.WorldMgr.IsPvPRealm || _session.HasPermission(RBACPermissions.TwoSideCharacterCreation);
										var skipCinematics = _worldConfig.GetIntValue(WorldCfg.SkipCinematics);
										var checkClassLevelReqs = (createInfo.ClassId == PlayerClass.DemonHunter || createInfo.ClassId == PlayerClass.Evoker) && !_session.HasPermission(RBACPermissions.SkipCheckCharacterCreationDemonHunter);
										var evokerLimit = _worldConfig.GetIntValue(WorldCfg.CharacterCreatingEvokersPerRealm);
										var hasEvokerLimit = evokerLimit != 0;

										void finalizeCharacterCreation(SQLResult result1)
										{
											var haveSameRace = false;

											if (result1 != null && !result1.IsEmpty() && result.GetFieldCount() >= 3)
											{
												var team = Player.TeamForRace(createInfo.RaceId);
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

												// need to check team only for first character
												// @todo what to if account already has characters of both races?
												if (!allowTwoSideAccounts)
												{
													TeamFaction accTeam = 0;

													if (accRace > 0)
														accTeam = Player.TeamForRace((Race)accRace);

													if (accTeam != team)
													{
														SendCharCreate(ResponseCodes.CharCreatePvpTeamsViolation);

														return;
													}
												}

												// search same race for cinematic or same class if need
												// @todo check if cinematic already shown? (already logged in?; cinematic field)
												while ((skipCinematics == 1 && !haveSameRace) || createInfo.ClassId == PlayerClass.DemonHunter || createInfo.ClassId == PlayerClass.Evoker)
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
															var acc_level = result1.Read<byte>(0);

															if (acc_level >= demonHunterReqLevel)
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
											if (Global.CharacterCacheStorage.GetCharacterCacheByName(createInfo.Name) != null)
											{
												SendCharCreate(ResponseCodes.CharCreateDracthyrDuplicate);

												return;
											}

											Player newChar = new(_session);
											newChar.MotionMaster.Initialize();

											if (!newChar.Create(Global.ObjectMgr.GetGenerator(HighGuid.Player).Generate(), createInfo))
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
											stmt.AddValue(2, Global.WorldMgr.Realm.Id.Index);
											loginTransaction.Append(stmt);

											_loginDatabase.CommitTransaction(loginTransaction);

											_session.AddTransactionCallback(_characterDatabase.AsyncCommitTransaction(characterTransaction))
												.AfterComplete(success =>
												{
													if (success)
													{
														Log.Logger.Information("Account: {0} (IP: {1}) Create Character: {2} {3}", _session.AccountId, _session.RemoteAddress, createInfo.Name, newChar.GUID.ToString());
														_scriptManager.ForEach<IPlayerOnCreate>(newChar.Class, p => p.OnCreate(newChar));
														Global.CharacterCacheStorage.AddCharacterCacheEntry(newChar.GUID, _session.AccountId, newChar.GetName(), (byte)newChar.NativeGender, (byte)newChar.Race, (byte)newChar.Class, (byte)newChar.Level, false);

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
											finalizeCharacterCreation(new SQLResult());

											return;
										}

										stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHAR_CREATE_INFO);
										stmt.AddValue(0, _session.AccountId);
										stmt.AddValue(1, (skipCinematics == 1 || createInfo.ClassId == PlayerClass.DemonHunter || createInfo.ClassId == PlayerClass.Evoker) ? 1200 : 1); // 200 (max chars per realm) + 1000 (max deleted chars per realm)
										queryCallback.WithCallback(finalizeCharacterCreation).SetNextQuery(_characterDatabase.AsyncQuery(stmt));
									}));
	}

	[WorldPacketHandler(ClientOpcodes.CharDelete, Status = SessionStatus.Authed)]
	void HandleCharDelete(CharDelete charDelete)
	{
		// Initiating
		var initAccountId = _session.AccountId;

		// can't delete loaded character
		if (Global.ObjAccessor.FindPlayer(charDelete.Guid))
		{
			_scriptManager.ForEach<IPlayerOnFailedDelete>(p => p.OnFailedDelete(charDelete.Guid, initAccountId));

			return;
		}

		// is guild leader
		if (Global.GuildMgr.GetGuildByLeader(charDelete.Guid))
		{
			_scriptManager.ForEach<IPlayerOnFailedDelete>(p => p.OnFailedDelete(charDelete.Guid, initAccountId));
			SendCharDelete(ResponseCodes.CharDeleteFailedGuildLeader);

			return;
		}

		// is arena team captain
		if (Global.ArenaTeamMgr.GetArenaTeamByCaptain(charDelete.Guid) != null)
		{
			_scriptManager.ForEach<IPlayerOnFailedDelete>(p => p.OnFailedDelete(charDelete.Guid, initAccountId));
			SendCharDelete(ResponseCodes.CharDeleteFailedArenaCaptain);

			return;
		}

		var characterInfo = Global.CharacterCacheStorage.GetCharacterCacheByGuid(charDelete.Guid);

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

		var IP_str = _session.RemoteAddress;
		Log.Logger.Information("Account: {0}, IP: {1} deleted character: {2}, {3}, Level: {4}", accountId, IP_str, name, charDelete.Guid.ToString(), level);

		// To prevent hook failure, place hook before removing reference from DB
		_scriptManager.ForEach<IPlayerOnDelete>(p => p.OnDelete(charDelete.Guid, initAccountId)); // To prevent race conditioning, but as it also makes sense, we hand the accountId over for successful delete.

		// Shouldn't interfere with character deletion though

		Global.CalendarMgr.RemoveAllPlayerEventsAndInvites(charDelete.Guid);
		Player.DeleteFromDB(charDelete.Guid, accountId);

		SendCharDelete(ResponseCodes.CharDeleteSuccess);
	}

	[WorldPacketHandler(ClientOpcodes.GenerateRandomCharacterName, Status = SessionStatus.Authed)]
	void HandleRandomizeCharName(GenerateRandomCharacterName packet)
	{
		if (!Player.IsValidRace((Race)packet.Race))
		{
			Log.Logger.Error("Invalid race ({0}) sent by accountId: {1}", packet.Race, _session.AccountId);

			return;
		}

		if (!Player.IsValidGender((Gender)packet.Sex))
		{
			Log.Logger.Error("Invalid gender ({0}) sent by accountId: {1}", packet.Sex, _session.AccountId);

			return;
		}

		GenerateRandomCharacterNameResult result = new();
		result.Success = true;
		result.Name = Global.DB2Mgr.GetNameGenEntry(packet.Race, packet.Sex);

		_session.SendPacket(result);
	}

	[WorldPacketHandler(ClientOpcodes.ReorderCharacters, Status = SessionStatus.Authed)]
	void HandleReorderCharacters(ReorderCharacters reorderChars)
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

	[WorldPacketHandler(ClientOpcodes.PlayerLogin, Status = SessionStatus.Authed)]
	void HandlePlayerLogin(PlayerLogin playerLogin)
	{
		if (_session.PlayerLoading.IsEmpty || _session.Player != null)
		{
			Log.Logger.Error("Player tries to login again, _session.AccountId = {0}", _session.AccountId);
			_session.KickPlayer("WorldSession::HandlePlayerLoginOpcode Another client logging in");

			return;
		}

		_session.PlayerLoading = playerLogin.Guid;
		Log.Logger.Debug("Character {0} logging in", playerLogin.Guid.ToString());

		if (!_session.LegitCharacters.Contains(playerLogin.Guid))
		{
			Log.Logger.Error("Account ({0}) can't login with that character ({1}).", _session.AccountId, playerLogin.Guid.ToString());
			_session.KickPlayer("WorldSession::HandlePlayerLoginOpcode Trying to login with a character of another account");

			return;
		}

        _session.SendConnectToInstance(ConnectToSerial.WorldAttempt1);
	}

	[WorldPacketHandler(ClientOpcodes.LoadingScreenNotify, Status = SessionStatus.Authed)]
	void HandleLoadScreen(LoadingScreenNotify loadingScreenNotify)
	{
		// TODO: Do something with this packet
	}

	[WorldPacketHandler(ClientOpcodes.SetFactionAtWar)]
	void HandleSetFactionAtWar(SetFactionAtWar packet)
	{
		_session.Player.ReputationMgr.SetAtWar(packet.FactionIndex, true);
	}

	[WorldPacketHandler(ClientOpcodes.SetFactionNotAtWar)]
	void HandleSetFactionNotAtWar(SetFactionNotAtWar packet)
	{
		_session.Player.ReputationMgr.SetAtWar(packet.FactionIndex, false);
	}

	[WorldPacketHandler(ClientOpcodes.Tutorial)]
	void HandleTutorialFlag(TutorialSetFlag packet)
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

	[WorldPacketHandler(ClientOpcodes.SetWatchedFaction)]
	void HandleSetWatchedFaction(SetWatchedFaction packet)
	{
		_session.Player.SetWatchedFactionIndex(packet.FactionIndex);
	}

	[WorldPacketHandler(ClientOpcodes.SetFactionInactive)]
	void HandleSetFactionInactive(SetFactionInactive packet)
	{
		_session.Player.ReputationMgr.SetInactive(packet.Index, packet.State);
	}

	[WorldPacketHandler(ClientOpcodes.CheckCharacterNameAvailability)]
	void HandleCheckCharacterNameAvailability(CheckCharacterNameAvailability checkCharacterNameAvailability)
	{
		// prevent character rename to invalid name
		if (!ObjectManager.NormalizePlayerName(ref checkCharacterNameAvailability.Name))
		{
			_session.SendPacket(new CheckCharacterNameAvailabilityResult(checkCharacterNameAvailability.SequenceIndex, ResponseCodes.CharNameNoName));

			return;
		}

		var res = ObjectManager.CheckPlayerName(checkCharacterNameAvailability.Name, _session.SessionDbcLocale, true);

		if (res != ResponseCodes.CharNameSuccess)
		{
			_session.SendPacket(new CheckCharacterNameAvailabilityResult(checkCharacterNameAvailability.SequenceIndex, res));

			return;
		}

		// check name limitations
		if (!_session.HasPermission(RBACPermissions.SkipCheckCharacterCreationReservedname) && Global.ObjectMgr.IsReservedName(checkCharacterNameAvailability.Name))
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

	[WorldPacketHandler(ClientOpcodes.RequestForcedReactions)]
	void HandleRequestForcedReactions(RequestForcedReactions requestForcedReactions)
	{
		_session.Player.ReputationMgr.SendForceReactions();
	}

	[WorldPacketHandler(ClientOpcodes.CharacterRenameRequest, Status = SessionStatus.Authed)]
	void HandleCharRename(CharacterRenameRequest request)
	{
		if (!_session.LegitCharacters.Contains(request.RenameInfo.Guid))
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
		if (!ObjectManager.NormalizePlayerName(ref request.RenameInfo.NewName))
		{
			SendCharRename(ResponseCodes.CharNameNoName, request.RenameInfo);

			return;
		}

		var res = ObjectManager.CheckPlayerName(request.RenameInfo.NewName, _session.SessionDbcLocale, true);

		if (res != ResponseCodes.CharNameSuccess)
		{
			SendCharRename(res, request.RenameInfo);

			return;
		}

		if (!_session.HasPermission(RBACPermissions.SkipCheckCharacterCreationReservedname) && Global.ObjectMgr.IsReservedName(request.RenameInfo.NewName))
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

	void HandleCharRenameCallBack(CharacterRenameInfo renameInfo, SQLResult result)
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

		Global.CharacterCacheStorage.UpdateCharacterData(renameInfo.Guid, renameInfo.NewName);
	}

	[WorldPacketHandler(ClientOpcodes.SetPlayerDeclinedNames, Status = SessionStatus.Authed)]
	void HandleSetPlayerDeclinedNames(SetPlayerDeclinedNames packet)
	{
		// not accept declined names for unsupported languages
		if (!Global.CharacterCacheStorage.GetCharacterNameByGuid(packet.Player, out var name))
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

			if (!ObjectManager.NormalizePlayerName(ref declinedName))
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

	[WorldPacketHandler(ClientOpcodes.AlterAppearance)]
	void HandleAlterAppearance(AlterApperance packet)
	{
		if (!ValidateAppearance(_session.Player.Race, _session.Player.Class, (Gender)packet.NewSex, packet.Customizations))
			return;

		var go = _session.Player.FindNearestGameObjectOfType(GameObjectTypes.BarberChair, 5.0f);

		if (!go)
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
			_session.Player.RestoreDisplayId(false);
		}

		_session.Player.SetCustomizations(packet.Customizations);

		_session.Player.UpdateCriteria(CriteriaType.GotHaircut, 1);

		_session.Player.SetStandState(UnitStandStateType.Stand);

		Global.CharacterCacheStorage.UpdateCharacterGender(_session.Player.GUID, packet.NewSex);
	}

	[WorldPacketHandler(ClientOpcodes.CharCustomize, Status = SessionStatus.Authed)]
	void HandleCharCustomize(CharCustomize packet)
	{
		if (!_session.LegitCharacters.Contains(packet.CustomizeInfo.CharGUID))
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

	void HandleCharCustomizeCallback(CharCustomizeInfo customizeInfo, SQLResult result)
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
		if (_worldConfig.GetBoolValue(WorldCfg.PreventRenameCustomization) && (customizeInfo.CharName != oldName))
		{
			SendCharCustomize(ResponseCodes.CharNameFailure, customizeInfo);

			return;
		}

		atLoginFlags &= ~AtLoginFlags.Customize;

		// prevent character rename to invalid name
		if (!ObjectManager.NormalizePlayerName(ref customizeInfo.CharName))
		{
			SendCharCustomize(ResponseCodes.CharNameNoName, customizeInfo);

			return;
		}

		var res = ObjectManager.CheckPlayerName(customizeInfo.CharName, _session.SessionDbcLocale, true);

		if (res != ResponseCodes.CharNameSuccess)
		{
			SendCharCustomize(res, customizeInfo);

			return;
		}

		// check name limitations
		if (!_session.HasPermission(RBACPermissions.SkipCheckCharacterCreationReservedname) && Global.ObjectMgr.IsReservedName(customizeInfo.CharName))
		{
			SendCharCustomize(ResponseCodes.CharNameReserved, customizeInfo);

			return;
		}

		// character with this name already exist
		// @todo: make async
		var newGuid = Global.CharacterCacheStorage.GetCharacterGuidByName(customizeInfo.CharName);

		if (!newGuid.IsEmpty)
			if (newGuid != customizeInfo.CharGUID)
			{
				SendCharCustomize(ResponseCodes.CharCreateNameInUse, customizeInfo);

				return;
			}

		PreparedStatement stmt;
		SQLTransaction trans = new();
		var lowGuid = customizeInfo.CharGUID.Counter;

		// Customize
		Player.SaveCustomizations(trans, lowGuid, customizeInfo.Customizations);

		// Name Change and update atLogin flags
		{
			stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_CHAR_NAME_AT_LOGIN);
			stmt.AddValue(0, customizeInfo.CharName);
			stmt.AddValue(1, (ushort)atLoginFlags);
			stmt.AddValue(2, lowGuid);
			trans.Append(stmt);

			stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_DECLINED_NAME);
			stmt.AddValue(0, lowGuid);

			trans.Append(stmt);
		}

		_characterDatabase.CommitTransaction(trans);

		Global.CharacterCacheStorage.UpdateCharacterData(customizeInfo.CharGUID, customizeInfo.CharName, (byte)customizeInfo.SexID);

		SendCharCustomize(ResponseCodes.Success, customizeInfo);

		Log.Logger.Information(
					"Account: {0} (IP: {1}), Character[{2}] ({3}) Customized to: {4}",
					_session.AccountId,
                    _session.RemoteAddress,
					oldName,
					customizeInfo.CharGUID.ToString(),
					customizeInfo.CharName);
	}

	[WorldPacketHandler(ClientOpcodes.SaveEquipmentSet)]
	void HandleEquipmentSetSave(SaveEquipmentSet saveEquipmentSet)
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
						if (!item)
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
						if (!_cliDB.ItemModifiedAppearanceStorage.ContainsKey(saveEquipmentSet.Set.Appearances[i]))
							return;

						(var hasAppearance, _) = _collectionMgr.HasItemAppearance((uint)saveEquipmentSet.Set.Appearances[i]);

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
				var illusion = _cliDB.SpellItemEnchantmentStorage.LookupByKey(enchantId);

				if (illusion == null)
					return false;

				if (illusion.ItemVisual == 0 || !illusion.GetFlags().HasFlag(SpellItemEnchantmentFlags.AllowTransmog))
					return false;

				var condition = _cliDB.PlayerConditionStorage.LookupByKey(illusion.TransmogUseConditionID);

				if (condition != null)
					if (!ConditionManager.IsPlayerMeetingCondition(_session.Player, condition))
						return false;

				if (illusion.ScalingClassRestricted > 0 && illusion.ScalingClassRestricted != (byte)_session.Player.Class)
					return false;

				return true;
			});

			if (saveEquipmentSet.Set.Enchants[0] != 0 && !validateIllusion((uint)saveEquipmentSet.Set.Enchants[0]))
				return;

			if (saveEquipmentSet.Set.Enchants[1] != 0 && !validateIllusion((uint)saveEquipmentSet.Set.Enchants[1]))
				return;
		}

		_session.Player.SetEquipmentSet(saveEquipmentSet.Set);
	}

	[WorldPacketHandler(ClientOpcodes.DeleteEquipmentSet)]
	void HandleDeleteEquipmentSet(DeleteEquipmentSet packet)
	{
		_session.Player.DeleteEquipmentSet(packet.ID);
	}

	[WorldPacketHandler(ClientOpcodes.UseEquipmentSet, Processing = PacketProcessing.Inplace)]
	void HandleUseEquipmentSet(UseEquipmentSet useEquipmentSet)
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

			if (!item)
			{
				var uItem = _session.Player.GetItemByPos(InventorySlots.Bag0, i);

				if (!uItem)
					continue;

				List<ItemPosCount> itemPosCount = new();
				var inventoryResult = _session.Player.CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, itemPosCount, uItem, false);

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

		UseEquipmentSetResult result = new();
		result.GUID = useEquipmentSet.GUID;
		result.Reason = 0; // 4 - equipment swap failed - inventory is full
		_session.SendPacket(result);
	}

	[WorldPacketHandler(ClientOpcodes.CharRaceOrFactionChange, Status = SessionStatus.Authed)]
	void HandleCharRaceOrFactionChange(CharRaceOrFactionChange packet)
	{
		if (!_session.LegitCharacters.Contains(packet.RaceOrFactionChangeInfo.Guid))
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

	void HandleCharRaceOrFactionChangeCallback(CharRaceOrFactionChangeInfo factionChangeInfo, SQLResult result)
	{
		if (result.IsEmpty())
		{
			SendCharFactionChange(ResponseCodes.CharCreateError, factionChangeInfo);

			return;
		}

		// get the players old (at this moment current) race
		var characterInfo = Global.CharacterCacheStorage.GetCharacterCacheByGuid(factionChangeInfo.Guid);

		if (characterInfo == null)
		{
			SendCharFactionChange(ResponseCodes.CharCreateError, factionChangeInfo);

			return;
		}

		var oldName = characterInfo.Name;
		var oldRace = characterInfo.RaceId;
		var playerClass = characterInfo.ClassId;
		var level = characterInfo.Level;

		if (Global.ObjectMgr.GetPlayerInfo(factionChangeInfo.RaceID, playerClass) == null)
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

		var newTeamId = Player.TeamIdForRace(factionChangeInfo.RaceID);

		if (newTeamId == TeamIds.Neutral)
		{
			SendCharFactionChange(ResponseCodes.CharCreateRestrictedRaceclass, factionChangeInfo);

			return;
		}

		if (factionChangeInfo.FactionChange == (Player.TeamIdForRace(oldRace) == newTeamId))
		{
			SendCharFactionChange(factionChangeInfo.FactionChange ? ResponseCodes.CharCreateCharacterSwapFaction : ResponseCodes.CharCreateCharacterRaceOnly, factionChangeInfo);

			return;
		}

		if (!_session.HasPermission(RBACPermissions.SkipCheckCharacterCreationRacemask))
		{
			var raceMaskDisabled = _worldConfig.GetUInt64Value(WorldCfg.CharacterCreatingDisabledRacemask);

			if (Convert.ToBoolean((ulong)SharedConst.GetMaskForRace(factionChangeInfo.RaceID) & raceMaskDisabled))
			{
				SendCharFactionChange(ResponseCodes.CharCreateError, factionChangeInfo);

				return;
			}
		}

		// prevent character rename
		if (_worldConfig.GetBoolValue(WorldCfg.PreventRenameCustomization) && (factionChangeInfo.Name != oldName))
		{
			SendCharFactionChange(ResponseCodes.CharNameFailure, factionChangeInfo);

			return;
		}

		// prevent character rename to invalid name
		if (!ObjectManager.NormalizePlayerName(ref factionChangeInfo.Name))
		{
			SendCharFactionChange(ResponseCodes.CharNameNoName, factionChangeInfo);

			return;
		}

		var res = ObjectManager.CheckPlayerName(factionChangeInfo.Name, _session.SessionDbcLocale, true);

		if (res != ResponseCodes.CharNameSuccess)
		{
			SendCharFactionChange(res, factionChangeInfo);

			return;
		}

		// check name limitations
		if (!_session.HasPermission(RBACPermissions.SkipCheckCharacterCreationReservedname) && Global.ObjectMgr.IsReservedName(factionChangeInfo.Name))
		{
			SendCharFactionChange(ResponseCodes.CharNameReserved, factionChangeInfo);

			return;
		}

		// character with this name already exist
		var newGuid = Global.CharacterCacheStorage.GetCharacterGuidByName(factionChangeInfo.Name);

		if (!newGuid.IsEmpty)
			if (newGuid != factionChangeInfo.Guid)
			{
				SendCharFactionChange(ResponseCodes.CharCreateNameInUse, factionChangeInfo);

				return;
			}

		if (Global.ArenaTeamMgr.GetArenaTeamByCaptain(factionChangeInfo.Guid) != null)
		{
			SendCharFactionChange(ResponseCodes.CharCreateCharacterArenaLeader, factionChangeInfo);

			return;
		}

		// All checks are fine, deal with race change now
		var lowGuid = factionChangeInfo.Guid.Counter;

		PreparedStatement stmt;
		SQLTransaction trans = new();

		// resurrect the character in case he's dead
		Player.OfflineResurrect(factionChangeInfo.Guid, trans);

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
		Player.SaveCustomizations(trans, lowGuid, factionChangeInfo.Customizations);

		// Race Change
		{
			stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_CHAR_RACE);
			stmt.AddValue(0, (byte)factionChangeInfo.RaceID);
			stmt.AddValue(1, (ushort)PlayerExtraFlags.HasRaceChanged);
			stmt.AddValue(2, lowGuid);

			trans.Append(stmt);
		}

		Global.CharacterCacheStorage.UpdateCharacterData(factionChangeInfo.Guid, factionChangeInfo.Name, (byte)factionChangeInfo.SexID, (byte)factionChangeInfo.RaceID);

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
			if (newTeamId == TeamIds.Horde)
				stmt.AddValue(1, 109);
			else
				stmt.AddValue(1, 98);

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


					var factionMask = newTeamId == TeamIds.Horde ? _cliDB.HordeTaxiNodesMask : _cliDB.AllianceTaxiNodesMask;

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

				if (!_worldConfig.GetBoolValue(WorldCfg.AllowTwoSideInteractionGuild))
				{
					// Reset guild
					var guild = Global.GuildMgr.GetGuildById(characterInfo.GuildId);

					if (guild != null)
						guild.DeleteMember(trans, factionChangeInfo.Guid, false, false, true);

					Player.LeaveAllArenaTeams(factionChangeInfo.Guid);
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
					loc = new WorldLocation(0, -8867.68f, 673.373f, 97.9034f, 0.0f);
					zoneId = 1519;
				}
				else
				{
					loc = new WorldLocation(1, 1633.33f, -4439.11f, 15.7588f, 0.0f);
					zoneId = 1637;
				}

				stmt.AddValue(1, loc.MapId);
				stmt.AddValue(2, zoneId);
				stmt.AddValue(3, loc.X);
				stmt.AddValue(4, loc.Y);
				stmt.AddValue(5, loc.Z);
				trans.Append(stmt);

				Player.SavePositionInDB(loc, zoneId, factionChangeInfo.Guid, trans);

				// Achievement conversion
				foreach (var it in Global.ObjectMgr.FactionChangeAchievements)
				{
					var achiev_alliance = it.Key;
					var achiev_horde = it.Value;

					stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_ACHIEVEMENT_BY_ACHIEVEMENT);
					stmt.AddValue(0, (ushort)(newTeamId == TeamIds.Alliance ? achiev_alliance : achiev_horde));
					stmt.AddValue(1, lowGuid);
					trans.Append(stmt);

					stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_CHAR_ACHIEVEMENT);
					stmt.AddValue(0, (ushort)(newTeamId == TeamIds.Alliance ? achiev_alliance : achiev_horde));
					stmt.AddValue(1, (ushort)(newTeamId == TeamIds.Alliance ? achiev_horde : achiev_alliance));
					stmt.AddValue(2, lowGuid);
					trans.Append(stmt);
				}

				// Item conversion
				var itemConversionMap = newTeamId == TeamIds.Alliance ? Global.ObjectMgr.FactionChangeItemsHordeToAlliance : Global.ObjectMgr.FactionChangeItemsAllianceToHorde;

				foreach (var it in itemConversionMap)
				{
					var oldItemId = it.Key;
					var newItemId = it.Value;

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
				foreach (var it in Global.ObjectMgr.FactionChangeQuests)
				{
					var quest_alliance = it.Key;
					var quest_horde = it.Value;

					stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_QUESTSTATUS_REWARDED_BY_QUEST);
					stmt.AddValue(0, lowGuid);
					stmt.AddValue(1, (newTeamId == TeamIds.Alliance ? quest_alliance : quest_horde));
					trans.Append(stmt);

					stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_CHAR_QUESTSTATUS_REWARDED_FACTION_CHANGE);
					stmt.AddValue(0, (newTeamId == TeamIds.Alliance ? quest_alliance : quest_horde));
					stmt.AddValue(1, (newTeamId == TeamIds.Alliance ? quest_horde : quest_alliance));
					stmt.AddValue(2, lowGuid);
					trans.Append(stmt);
				}

				// Mark all rewarded quests as "active" (will count for completed quests achievements)
				stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_CHAR_QUESTSTATUS_REWARDED_ACTIVE);
				stmt.AddValue(0, lowGuid);
				trans.Append(stmt);

				// Disable all old-faction specific quests
				{
					var questTemplates = Global.ObjectMgr.GetQuestTemplates();

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
				foreach (var it in Global.ObjectMgr.FactionChangeSpells)
				{
					var spell_alliance = it.Key;
					var spell_horde = it.Value;

					stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_SPELL_BY_SPELL);
					stmt.AddValue(0, (newTeamId == TeamIds.Alliance ? spell_alliance : spell_horde));
					stmt.AddValue(1, lowGuid);
					trans.Append(stmt);

					stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_CHAR_SPELL_FACTION_CHANGE);
					stmt.AddValue(0, (newTeamId == TeamIds.Alliance ? spell_alliance : spell_horde));
					stmt.AddValue(1, (newTeamId == TeamIds.Alliance ? spell_horde : spell_alliance));
					stmt.AddValue(2, lowGuid);
					trans.Append(stmt);
				}

				// Reputation conversion
				foreach (var it in Global.ObjectMgr.FactionChangeReputation)
				{
					var reputation_alliance = it.Key;
					var reputation_horde = it.Value;
					var newReputation = (newTeamId == TeamIds.Alliance) ? reputation_alliance : reputation_horde;
					var oldReputation = (newTeamId == TeamIds.Alliance) ? reputation_horde : reputation_alliance;

					// select old standing set in db
					stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHAR_REP_BY_FACTION);
					stmt.AddValue(0, oldReputation);
					stmt.AddValue(1, lowGuid);

					result = _characterDatabase.Query(stmt);

					if (!result.IsEmpty())
					{
						var oldDBRep = result.Read<int>(0);
						var factionEntry = _cliDB.FactionStorage.LookupByKey(oldReputation);

						// old base reputation
						var oldBaseRep = ReputationMgr.GetBaseReputationOf(factionEntry, oldRace, playerClass);

						// new base reputation
						var newBaseRep = ReputationMgr.GetBaseReputationOf(_cliDB.FactionStorage.LookupByKey(newReputation), factionChangeInfo.RaceID, playerClass);

						// final reputation shouldnt change
						var FinalRep = oldDBRep + oldBaseRep;
						var newDBRep = FinalRep - newBaseRep;

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
				}

				// Title conversion
				if (!string.IsNullOrEmpty(knownTitlesStr))
				{
					List<uint> knownTitles = new();

					var tokens = new StringArray(knownTitlesStr, ' ');

					for (var index = 0; index < tokens.Length; ++index)
						if (uint.TryParse(tokens[index], out var id))
							knownTitles.Add(id);

					foreach (var it in Global.ObjectMgr.FactionChangeTitles)
					{
						var title_alliance = it.Key;
						var title_horde = it.Value;

						var atitleInfo = _cliDB.CharTitlesStorage.LookupByKey(title_alliance);
						var htitleInfo = _cliDB.CharTitlesStorage.LookupByKey(title_horde);

						// new team
						if (newTeamId == TeamIds.Alliance)
						{
							uint maskID = htitleInfo.MaskID;
							var index = (int)maskID / 32;

							if (index >= knownTitles.Count)
								continue;

							var old_flag = (uint)(1 << (int)(maskID % 32));
							var new_flag = (uint)(1 << (atitleInfo.MaskID % 32));

							if (Convert.ToBoolean(knownTitles[index] & old_flag))
							{
								knownTitles[index] &= ~old_flag;
								// use index of the new title
								knownTitles[atitleInfo.MaskID / 32] |= new_flag;
							}
						}
						else
						{
							uint maskID = atitleInfo.MaskID;
							var index = (int)maskID / 32;

							if (index >= knownTitles.Count)
								continue;

							var old_flag = (uint)(1 << (int)(maskID % 32));
							var new_flag = (uint)(1 << (htitleInfo.MaskID % 32));

							if (Convert.ToBoolean(knownTitles[index] & old_flag))
							{
								knownTitles[index] &= ~old_flag;
								// use index of the new title
								knownTitles[htitleInfo.MaskID / 32] |= new_flag;
							}
						}

						var ss = "";

						for (var index = 0; index < knownTitles.Count; ++index)
							ss += knownTitles[index] + ' ';

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

	[WorldPacketHandler(ClientOpcodes.OpeningCinematic)]
	void HandleOpeningCinematic(OpeningCinematic packet)
	{
		// Only players that has not yet gained any experience can use this
		if (_session.Player.ActivePlayerData.XP != 0)
			return;

		var classEntry = _cliDB.ChrClassesStorage.LookupByKey((uint)_session.Player.Class);

		if (classEntry != null)
		{
			var raceEntry = _cliDB.ChrRacesStorage.LookupByKey((uint)_session.Player.Race);

			if (classEntry.CinematicSequenceID != 0)
				_session.Player.SendCinematicStart(classEntry.CinematicSequenceID);
			else if (raceEntry != null)
				_session.Player.SendCinematicStart(raceEntry.CinematicSequenceID);
		}
	}

	[WorldPacketHandler(ClientOpcodes.GetUndeleteCharacterCooldownStatus, Status = SessionStatus.Authed)]
	void HandleGetUndeleteCooldownStatus(GetUndeleteCharacterCooldownStatus getCooldown)
	{
		var stmt = _loginDatabase.GetPreparedStatement(LoginStatements.SEL_LAST_CHAR_UNDELETE);
		stmt.AddValue(0, _session.BattlenetAccountId);

        _session.QueryProcessor.AddCallback(_loginDatabase.AsyncQuery(stmt).WithCallback(HandleUndeleteCooldownStatusCallback));
	}

	void HandleUndeleteCooldownStatusCallback(SQLResult result)
	{
		uint cooldown = 0;
		var maxCooldown = _worldConfig.GetUIntValue(WorldCfg.FeatureSystemCharacterUndeleteCooldown);

		if (!result.IsEmpty())
		{
			var lastUndelete = result.Read<uint>(0);
			var now = (uint)_gameTime.GetGameTime;

			if (lastUndelete + maxCooldown > now)
				cooldown = Math.Max(0, lastUndelete + maxCooldown - now);
		}

		SendUndeleteCooldownStatusResponse(cooldown, maxCooldown);
	}

	[WorldPacketHandler(ClientOpcodes.UndeleteCharacter, Status = SessionStatus.Authed)]
	void HandleCharUndelete(UndeleteCharacter undeleteCharacter)
	{
		if (!_worldConfig.GetBoolValue(WorldCfg.FeatureSystemCharacterUndeleteEnabled))
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
											var maxCooldown = _worldConfig.GetUIntValue(WorldCfg.FeatureSystemCharacterUndeleteCooldown);

											if (lastUndelete != 0 && (lastUndelete + maxCooldown > _gameTime.GetGameTime))
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
											if (result.Read<ulong>(0) >= _worldConfig.GetUIntValue(WorldCfg.CharactersPerRealm)) // SQL's COUNT() returns uint64 but it will always be less than uint8.Max
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

										Global.CharacterCacheStorage.UpdateCharacterInfoDeleted(undeleteInfo.CharacterGuid, false, undeleteInfo.Name);

										SendUndeleteCharacterResponse(CharacterUndeleteResult.Ok, undeleteInfo);
									}));
	}

	[WorldPacketHandler(ClientOpcodes.RepopRequest)]
	void HandleRepopRequest(RepopRequest packet)
	{
		if (_session.Player.IsAlive || _session.Player.HasPlayerFlag(PlayerFlags.Ghost))
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

	[WorldPacketHandler(ClientOpcodes.ClientPortGraveyard)]
	void HandlePortGraveyard(PortGraveyard packet)
	{
		if (_session.Player.IsAlive || !_session.Player.HasPlayerFlag(PlayerFlags.Ghost))
			return;

		_session.Player.RepopAtGraveyard();
	}

	[WorldPacketHandler(ClientOpcodes.RequestCemeteryList, Processing = PacketProcessing.Inplace)]
	void HandleRequestCemeteryList(RequestCemeteryList requestCemeteryList)
	{
		var zoneId = _session.Player.Zone;
		var team = (uint)_session.Player.Team;

		List<uint> graveyardIds = new();
		var range = Global.ObjectMgr.GraveYardStorage.LookupByKey(zoneId);

		for (uint i = 0; i < range.Count && graveyardIds.Count < 16; ++i) // client max
		{
			var gYard = range[(int)i];

			if (gYard.team == 0 || gYard.team == team)
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

		RequestCemeteryListResponse packet = new();
		packet.IsGossipTriggered = false;

		foreach (var id in graveyardIds)
			packet.CemeteryID.Add(id);

		_session.SendPacket(packet);
	}

	[WorldPacketHandler(ClientOpcodes.ReclaimCorpse)]
	void HandleReclaimCorpse(ReclaimCorpse packet)
	{
		if (_session.Player.IsAlive)
			return;

		// do not allow corpse reclaim in arena
		if (_session.Player.InArena)
			return;

		// body not released yet
		if (!_session.Player.HasPlayerFlag(PlayerFlags.Ghost))
			return;

		var corpse = _session.Player.GetCorpse();

		if (!corpse)
			return;

		// prevent resurrect before 30-sec delay after body release not finished
		if ((corpse.GetGhostTime() + _session.Player.GetCorpseReclaimDelay(corpse.GetCorpseType() == CorpseType.ResurrectablePVP)) > _gameTime.GetGameTime)
			return;

		if (!corpse.IsWithinDistInMap(_session.Player, 39, true))
			return;

		// resurrect
		_session.Player.ResurrectPlayer(_session.Player.InBattleground ? 1.0f : 0.5f);

		// spawn bones
		_session.Player.SpawnCorpseBones();
	}

	[WorldPacketHandler(ClientOpcodes.ResurrectResponse)]
	void HandleResurrectResponse(ResurrectResponse packet)
	{
        // Send to map server
    }

    [WorldPacketHandler(ClientOpcodes.StandStateChange)]
	void HandleStandStateChange(StandStateChange packet)
	{
        // Send to map server
    }

    [WorldPacketHandler(ClientOpcodes.QuickJoinAutoAcceptRequests)]
	void HandleQuickJoinAutoAcceptRequests(QuickJoinAutoAcceptRequest packet)
	{
		_session.Player.AutoAcceptQuickJoin = packet.AutoAccept;
	}

	[WorldPacketHandler(ClientOpcodes.OverrideScreenFlash)]
	void HandleOverrideScreenFlash(OverrideScreenFlash packet)
	{
		_session.Player.OverrideScreenFlash = packet.ScreenFlashEnabled;
	}

	void SendCharCreate(ResponseCodes result, ObjectGuid guid = default)
	{
		CreateChar response = new();
		response.Code = result;
		response.Guid = guid;

		_session.SendPacket(response);
	}

	void SendCharDelete(ResponseCodes result)
	{
		DeleteChar response = new();
		response.Code = result;

		_session.SendPacket(response);
	}

	void SendCharRename(ResponseCodes result, CharacterRenameInfo renameInfo)
	{
		CharacterRenameResult packet = new();
		packet.Result = result;
		packet.Name = renameInfo.NewName;

		if (result == ResponseCodes.Success)
			packet.Guid = renameInfo.Guid;

		_session.SendPacket(packet);
	}

	void SendCharCustomize(ResponseCodes result, CharCustomizeInfo customizeInfo)
	{
		if (result == ResponseCodes.Success)
		{
			CharCustomizeSuccess response = new(customizeInfo);
			_session.SendPacket(response);
		}
		else
		{
			CharCustomizeFailure failed = new();
			failed.Result = (byte)result;
			failed.CharGUID = customizeInfo.CharGUID;
			_session.SendPacket(failed);
		}
	}

	void SendCharFactionChange(ResponseCodes result, CharRaceOrFactionChangeInfo factionChangeInfo)
	{
		CharFactionChangeResult packet = new();
		packet.Result = result;
		packet.Guid = factionChangeInfo.Guid;

		if (result == ResponseCodes.Success)
		{
			packet.Display = new CharFactionChangeResult.CharFactionChangeDisplayInfo();
			packet.Display.Name = factionChangeInfo.Name;
			packet.Display.SexID = (byte)factionChangeInfo.SexID;
			packet.Display.Customizations = factionChangeInfo.Customizations;
			packet.Display.RaceID = (byte)factionChangeInfo.RaceID;
		}

		_session.SendPacket(packet);
	}

	void SendSetPlayerDeclinedNamesResult(DeclinedNameResult result, ObjectGuid guid)
	{
		SetPlayerDeclinedNamesResult packet = new();
		packet.ResultCode = result;
		packet.Player = guid;

		_session.SendPacket(packet);
	}

	void SendUndeleteCooldownStatusResponse(uint currentCooldown, uint maxCooldown)
	{
		UndeleteCooldownStatusResponse response = new();
		response.OnCooldown = (currentCooldown > 0);
		response.MaxCooldown = maxCooldown;
		response.CurrentCooldown = currentCooldown;

		_session.SendPacket(response);
	}

	void SendUndeleteCharacterResponse(CharacterUndeleteResult result, CharacterUndeleteInfo undeleteInfo)
	{
		UndeleteCharacterResponse response = new();
		response.UndeleteInfo = undeleteInfo;
		response.Result = result;

		_session.SendPacket(response);
	}
}

public class LoginQueryHolder : SQLQueryHolder<PlayerLoginQueryLoad>
{
	readonly uint _accountId;
	readonly CharacterDatabase _characterDatabase;
    readonly WorldConfig _worldConfig;
    ObjectGuid _guid;

	public LoginQueryHolder(uint accountId, ObjectGuid guid, CharacterDatabase characterDatabase, WorldConfig worldConfig)
	{
		_accountId = accountId;
		_guid = guid;
        _characterDatabase = characterDatabase;
        _worldConfig = worldConfig;
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

		if (_worldConfig.GetBoolValue(WorldCfg.DeclinedNamesUsed))
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

	public ObjectGuid GetGuid()
	{
		return _guid;
	}

	uint GetAccountId()
	{
		return _accountId;
	}
}

class EnumCharactersQueryHolder : SQLQueryHolder<EnumCharacterQueryLoad>
{
	bool _isDeletedCharacters = false;

	public bool Initialize(uint accountId, bool withDeclinedNames, bool isDeletedCharacters, CharacterDatabase characterDatabase)
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

		var result = true;
		var stmt = characterDatabase.GetPreparedStatement(statements[isDeletedCharacters ? 1 : 0][withDeclinedNames ? 1 : 0]);
		stmt.AddValue(0, accountId);
		SetQuery(EnumCharacterQueryLoad.Characters, stmt);

		stmt = characterDatabase.GetPreparedStatement(statements[isDeletedCharacters ? 1 : 0][2]);
		stmt.AddValue(0, accountId);
		SetQuery(EnumCharacterQueryLoad.Customizations, stmt);

		return result;
	}

	public bool IsDeletedCharacters()
	{
		return _isDeletedCharacters;
	}
}

// used at player loading query list preparing, and later result selection
public enum PlayerLoginQueryLoad
{
	From,
	Customizations,
	Group,
	Auras,
	AuraEffects,
	AuraStoredLocations,
	Spells,
	SpellFavorites,
	QuestStatus,
	QuestStatusObjectives,
	QuestStatusObjectivesCriteria,
	QuestStatusObjectivesCriteriaProgress,
	DailyQuestStatus,
	Reputation,
	Inventory,
	Artifacts,
	Azerite,
	AzeriteMilestonePowers,
	AzeriteUnlockedEssences,
	AzeriteEmpowered,
	Mails,
	MailItems,
	MailItemsArtifact,
	MailItemsAzerite,
	MailItemsAzeriteMilestonePower,
	MailItemsAzeriteUnlockedEssence,
	MailItemsAzeriteEmpowered,
	SocialList,
	HomeBind,
	SpellCooldowns,
	SpellCharges,
	DeclinedNames,
	Guild,
	ArenaInfo,
	Achievements,
	CriteriaProgress,
	EquipmentSets,
	TransmogOutfits,
	BgData,
	Glyphs,
	Talents,
	PvpTalents,
	AccountData,
	Skills,
	WeeklyQuestStatus,
	RandomBg,
	Banned,
	QuestStatusRew,
	InstanceLockTimes,
	SeasonalQuestStatus,
	MonthlyQuestStatus,
	VoidStorage,
	Currency,
	CufProfiles,
	CorpseLocation,
	PetSlots,
	Garrison,
	GarrisonBlueprints,
	GarrisonBuildings,
	GarrisonFollowers,
	GarrisonFollowerAbilities,
	TraitEntries,
	TraitConfigs,
	Max
}

enum EnumCharacterQueryLoad
{
	Characters,
	Customizations
}