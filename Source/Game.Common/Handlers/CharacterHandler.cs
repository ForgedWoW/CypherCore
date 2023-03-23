// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Collections;
using Framework.Configuration;
using Framework.Constants;
using Framework.Database;
using Game.DataStorage;
using Game.Entities;
using Game.Networking;
using Game.Networking.Packets;
using Game.Scripting.Interfaces.IPlayer;
using Game.Spells;

namespace Game;

public partial class WorldSession
{
	public bool MeetsChrCustomizationReq(ChrCustomizationReqRecord req, PlayerClass playerClass, bool checkRequiredDependentChoices, List<ChrCustomizationChoice> selectedChoices)
	{
		if (!req.GetFlags().HasFlag(ChrCustomizationReqFlag.HasRequirements))
			return true;

		if (req.ClassMask != 0 && (req.ClassMask & (1 << ((int)playerClass - 1))) == 0)
			return false;

		if (req.AchievementID != 0 /*&& !HasAchieved(req->AchievementID)*/)
			return false;

		if (req.ItemModifiedAppearanceID != 0 && !CollectionMgr.HasItemAppearance(req.ItemModifiedAppearanceID).PermAppearance)
			return false;

		if (req.QuestID != 0)
		{
			if (!_player)
				return false;

			if (!_player.IsQuestRewarded((uint)req.QuestID))
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

			var req = CliDB.ChrCustomizationReqStorage.LookupByKey(customizationOptionData.ChrCustomizationReqID);

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

			var reqEntry = CliDB.ChrCustomizationReqStorage.LookupByKey(customizationChoiceData.ChrCustomizationReqID);

			if (reqEntry != null)
				if (!MeetsChrCustomizationReq(reqEntry, playerClass, true, customizations))
					return false;
		}

		return true;
	}

	public void HandleContinuePlayerLogin()
	{
		if (!PlayerLoading || Player)
		{
			KickPlayer("WorldSession::HandleContinuePlayerLogin incorrect player state when logging in");

			return;
		}

		LoginQueryHolder holder = new(AccountId, _playerLoading);
		holder.Initialize();

		SendPacket(new ResumeComms(ConnectionType.Instance));

		AddQueryHolderCallback(DB.Characters.DelayQueryHolder(holder)).AfterComplete(holder => HandlePlayerLogin((LoginQueryHolder)holder));
	}

	public void HandlePlayerLogin(LoginQueryHolder holder)
	{
		var playerGuid = holder.GetGuid();

		Player pCurrChar = new(this);

		if (!pCurrChar.LoadFromDB(playerGuid, holder))
		{
			Player = null;
			KickPlayer("WorldSession::HandlePlayerLogin Player::LoadFromDB failed");
			_playerLoading.Clear();

			return;
		}

		pCurrChar.SetVirtualPlayerRealm(Global.WorldMgr.VirtualRealmAddress);

		SendAccountDataTimes(ObjectGuid.Empty, AccountDataTypes.GlobalCacheMask);
		SendTutorialsData();

		pCurrChar.MotionMaster.Initialize();
		pCurrChar.SendDungeonDifficulty();

		LoginVerifyWorld loginVerifyWorld = new();
		loginVerifyWorld.MapID = (int)pCurrChar.Location.MapId;
		loginVerifyWorld.Pos = pCurrChar.Location;
		SendPacket(loginVerifyWorld);

		// load player specific part before send times
		LoadAccountData(holder.GetResult(PlayerLoginQueryLoad.AccountData), AccountDataTypes.PerCharacterCacheMask);

		SendAccountDataTimes(playerGuid, AccountDataTypes.AllAccountDataCacheMask);

		SendFeatureSystemStatus();

		MOTD motd = new();
		motd.Text = Global.WorldMgr.Motd;
		SendPacket(motd);

		SendSetTimeZoneInformation();

		// Send PVPSeason
		{
			SeasonInfo seasonInfo = new();
			seasonInfo.PreviousArenaSeason = (WorldConfig.GetIntValue(WorldCfg.ArenaSeasonId) - (WorldConfig.GetBoolValue(WorldCfg.ArenaSeasonInProgress) ? 1 : 0));

			if (WorldConfig.GetBoolValue(WorldCfg.ArenaSeasonInProgress))
				seasonInfo.CurrentArenaSeason = WorldConfig.GetIntValue(WorldCfg.ArenaSeasonId);

			SendPacket(seasonInfo);
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
			SendStablePet(ObjectGuid.Empty);

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
						else if (CliDB.ChrClassesStorage.TryGetValue((uint)pCurrChar.Class, out var chrClassesRecord) && chrClassesRecord.CinematicSequenceID != 0)
							pCurrChar.SendCinematicStart(chrClassesRecord.CinematicSequenceID);
						else if (CliDB.ChrRacesStorage.TryGetValue((uint)pCurrChar.Race, out var chrRacesRecord) && chrRacesRecord.CinematicSequenceID != 0)
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
				guild.SendLoginInfo(this);
			}
			else
			{
				// remove wrong guild data
				Log.outError(LogFilter.Server,
							"Player {0} ({1}) marked as member of not existing guild (id: {2}), removing guild membership for player.",
							pCurrChar.GetName(),
							pCurrChar.GUID.ToString(),
							pCurrChar.GuildId);

				pCurrChar.SetInGuild(0);
			}
		}

		pCurrChar.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Login);

		pCurrChar.SendInitialPacketsAfterAddToMap();

		var stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHAR_ONLINE);
		stmt.AddValue(0, pCurrChar.GUID.Counter);
		DB.Characters.Execute(stmt);

		stmt = DB.Login.GetPreparedStatement(LoginStatements.UPD_ACCOUNT_ONLINE);
		stmt.AddValue(0, AccountId);
		DB.Login.Execute(stmt);

		pCurrChar.SetInGameTime(GameTime.GetGameTimeMS());

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
			var stmtSpells = DB.Characters.GetPreparedStatement(CharStatements.DEL_ALL_PET_SPELLS_BY_OWNER);
			stmtSpells.AddValue(0, pCurrChar.GUID.Counter);
			DB.Characters.Execute(stmtSpells);

			// Then reset all of the player's pet specualizations
			var stmtSpec = DB.Characters.GetPreparedStatement(CharStatements.UPD_PET_SPECS_BY_OWNER);
			stmtSpec.AddValue(0, pCurrChar.GUID.Counter);
			DB.Characters.Execute(stmtSpec);
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
			SendNotification(CypherStrings.ResetSpells);
		}

		if (pCurrChar.HasAtLoginFlag(AtLoginFlags.ResetTalents))
		{
			pCurrChar.ResetTalents(true);
			pCurrChar.ResetTalentSpecialization();
			pCurrChar.SendTalentsInfoData(); // original talents send already in to SendInitialPacketsBeforeAddToMap, resend reset state
			SendNotification(CypherStrings.ResetTalents);
		}

		if (pCurrChar.HasAtLoginFlag(AtLoginFlags.FirstLogin))
		{
			pCurrChar.RemoveAtLoginFlag(AtLoginFlags.FirstLogin);

			var info = Global.ObjectMgr.GetPlayerInfo(pCurrChar.Race, pCurrChar.Class);

			foreach (var spellId in info.CastSpells[(int)pCurrChar.CreateMode])
				pCurrChar.CastSpell(pCurrChar, spellId, new CastSpellExtraArgs(true));

			// start with every map explored
			if (WorldConfig.GetBoolValue(WorldCfg.StartAllExplored))
				for (uint i = 0; i < PlayerConst.ExploredZonesSize; i++)
					pCurrChar.AddExploredZones(i, 0xFFFFFFFFFFFFFFFF);

			//Reputations if "StartAllReputation" is enabled
			if (WorldConfig.GetBoolValue(WorldCfg.StartAllRep))
			{
				var repMgr = pCurrChar.ReputationMgr;
				repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(942), 42999, false);  // Cenarion Expedition
				repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(935), 42999, false);  // The Sha'tar
				repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(936), 42999, false);  // Shattrath City
				repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(1011), 42999, false); // Lower City
				repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(970), 42999, false);  // Sporeggar
				repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(967), 42999, false);  // The Violet Eye
				repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(989), 42999, false);  // Keepers of Time
				repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(932), 42999, false);  // The Aldor
				repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(934), 42999, false);  // The Scryers
				repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(1038), 42999, false); // Ogri'la
				repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(1077), 42999, false); // Shattered Sun Offensive
				repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(1106), 42999, false); // Argent Crusade
				repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(1104), 42999, false); // Frenzyheart Tribe
				repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(1090), 42999, false); // Kirin Tor
				repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(1098), 42999, false); // Knights of the Ebon Blade
				repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(1156), 42999, false); // The Ashen Verdict
				repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(1073), 42999, false); // The Kalu'ak
				repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(1105), 42999, false); // The Oracles
				repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(1119), 42999, false); // The Sons of Hodir
				repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(1091), 42999, false); // The Wyrmrest Accord

				// Factions depending on team, like cities and some more stuff
				switch (pCurrChar.Team)
				{
					case TeamFaction.Alliance:
						repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(72), 42999, false);   // Stormwind
						repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(47), 42999, false);   // Ironforge
						repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(69), 42999, false);   // Darnassus
						repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(930), 42999, false);  // Exodar
						repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(730), 42999, false);  // Stormpike Guard
						repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(978), 42999, false);  // Kurenai
						repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(54), 42999, false);   // Gnomeregan Exiles
						repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(946), 42999, false);  // Honor Hold
						repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(1037), 42999, false); // Alliance Vanguard
						repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(1068), 42999, false); // Explorers' League
						repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(1126), 42999, false); // The Frostborn
						repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(1094), 42999, false); // The Silver Covenant
						repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(1050), 42999, false); // Valiance Expedition

						break;
					case TeamFaction.Horde:
						repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(76), 42999, false);   // Orgrimmar
						repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(68), 42999, false);   // Undercity
						repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(81), 42999, false);   // Thunder Bluff
						repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(911), 42999, false);  // Silvermoon City
						repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(729), 42999, false);  // Frostwolf Clan
						repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(941), 42999, false);  // The Mag'har
						repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(530), 42999, false);  // Darkspear Trolls
						repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(947), 42999, false);  // Thrallmar
						repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(1052), 42999, false); // Horde Expedition
						repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(1067), 42999, false); // The Hand of Vengeance
						repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(1124), 42999, false); // The Sunreavers
						repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(1064), 42999, false); // The Taunka
						repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(1085), 42999, false); // Warsong Offensive

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

		if (WorldConfig.GetBoolValue(WorldCfg.AllTaxiPaths))
			pCurrChar.SetTaxiCheater(true);

		if (pCurrChar.IsGameMaster)
			SendNotification(CypherStrings.GmOn);

		var IP_str = RemoteAddress;
		Log.outDebug(LogFilter.Network, $"Account: {AccountId} (IP: {RemoteAddress}) Login Character: [{pCurrChar.GetName()}] ({pCurrChar.GUID}) Level: {pCurrChar.Level}, XP: {_player.XP}/{_player.XPForNextLevel} ({_player.XPForNextLevel - _player.XP} left)");

		if (!pCurrChar.IsStandState && !pCurrChar.HasUnitState(UnitState.Stunned))
			pCurrChar.SetStandState(UnitStandStateType.Stand);

		pCurrChar.UpdateAverageItemLevelTotal();
		pCurrChar.UpdateAverageItemLevelEquipped();

		_playerLoading.Clear();

		// Handle Login-Achievements (should be handled after loading)
		_player.UpdateCriteria(CriteriaType.Login, 1);

		Global.ScriptMgr.ForEach<IPlayerOnLogin>(p => p.OnLogin(pCurrChar));
	}

	public void AbortLogin(LoginFailureReason reason)
	{
		if (!PlayerLoading || Player)
		{
			KickPlayer("WorldSession::AbortLogin incorrect player state when logging in");

			return;
		}

		_playerLoading.Clear();
		SendPacket(new CharacterLoginFailed(reason));
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

		europaTicketSystemStatus.TicketsEnabled = WorldConfig.GetBoolValue(WorldCfg.SupportTicketsEnabled);
		europaTicketSystemStatus.BugsEnabled = WorldConfig.GetBoolValue(WorldCfg.SupportBugsEnabled);
		europaTicketSystemStatus.ComplaintsEnabled = WorldConfig.GetBoolValue(WorldCfg.SupportComplaintsEnabled);
		europaTicketSystemStatus.SuggestionsEnabled = WorldConfig.GetBoolValue(WorldCfg.SupportSuggestionsEnabled);

		features.EuropaTicketSystemStatus = europaTicketSystemStatus;

		features.CharUndeleteEnabled = WorldConfig.GetBoolValue(WorldCfg.FeatureSystemCharacterUndeleteEnabled);
		features.BpayStoreEnabled = WorldConfig.GetBoolValue(WorldCfg.FeatureSystemBpayStoreEnabled);
		features.WarModeFeatureEnabled = WorldConfig.GetBoolValue(WorldCfg.FeatureSystemWarModeEnabled);
		features.IsMuted = !CanSpeak;


		features.TextToSpeechFeatureEnabled = false;

		SendPacket(features);
	}

	[WorldPacketHandler(ClientOpcodes.EnumCharacters, Status = SessionStatus.Authed)]
	void HandleCharEnum(EnumCharacters charEnum)
	{
		// remove expired bans
		var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_EXPIRED_BANS);
		DB.Characters.Execute(stmt);

		// get all the data necessary for loading all characters (along with their pets) on the account
		EnumCharactersQueryHolder holder = new();

		if (!holder.Initialize(AccountId, WorldConfig.GetBoolValue(WorldCfg.DeclinedNamesUsed), false))
		{
			HandleCharEnum(holder);

			return;
		}

		AddQueryHolderCallback(DB.Characters.DelayQueryHolder(holder)).AfterComplete(result => HandleCharEnum((EnumCharactersQueryHolder)result));
	}

	void HandleCharEnum(EnumCharactersQueryHolder holder)
	{
		EnumCharactersResult charResult = new();
		charResult.Success = true;
		charResult.IsDeletedCharacters = holder.IsDeletedCharacters();
		charResult.DisabledClassesMask = WorldConfig.GetUIntValue(WorldCfg.CharacterCreatingDisabledClassmask);

		if (!charResult.IsDeletedCharacters)
			_legitCharacters.Clear();

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

				Log.outDebug(LogFilter.Network, "Loading Character {0} from account {1}.", charInfo.Guid.ToString(), AccountId);

				if (!charResult.IsDeletedCharacters)
				{
					if (!ValidateAppearance((Race)charInfo.RaceId, charInfo.ClassId, (Gender)charInfo.SexId, charInfo.Customizations))
					{
						Log.outError(LogFilter.Player, "Player {0} has wrong Appearance values (Hair/Skin/Color), forcing recustomize", charInfo.Guid.ToString());

						charInfo.Customizations.Clear();

						if (charInfo.Flags2 != CharacterCustomizeFlags.Customize)
						{
							var stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_ADD_AT_LOGIN_FLAG);
							stmt.AddValue(0, (ushort)AtLoginFlags.Customize);
							stmt.AddValue(1, charInfo.Guid.Counter);
							DB.Characters.Execute(stmt);
							charInfo.Flags2 = CharacterCustomizeFlags.Customize;
						}
					}

					// Do not allow locked characters to login
					if (!charInfo.Flags.HasAnyFlag(CharacterFlags.CharacterLockedForTransfer | CharacterFlags.LockedByBilling))
						_legitCharacters.Add(charInfo.Guid);
				}

				if (!Global.CharacterCacheStorage.HasCharacterCacheEntry(charInfo.Guid)) // This can happen if characters are inserted into the database manually. Core hasn't loaded name data yet.
					Global.CharacterCacheStorage.AddCharacterCacheEntry(charInfo.Guid, AccountId, charInfo.Name, charInfo.SexId, charInfo.RaceId, (byte)charInfo.ClassId, charInfo.ExperienceLevel, false);

				charResult.MaxCharacterLevel = Math.Max(charResult.MaxCharacterLevel, charInfo.ExperienceLevel);

				charResult.Characters.Add(charInfo);
			} while (result.NextRow() && charResult.Characters.Count < 200);

		charResult.IsAlliedRacesCreationAllowed = CanAccessAlliedRaces();

		foreach (var requirement in Global.ObjectMgr.GetRaceUnlockRequirements())
		{
			EnumCharactersResult.RaceUnlock raceUnlock = new();
			raceUnlock.RaceID = requirement.Key;
			raceUnlock.HasExpansion = ConfigMgr.GetDefaultValue("character.EnforceRaceAndClassExpansions", true) ? (byte)AccountExpansion >= requirement.Value.Expansion : true;
			raceUnlock.HasAchievement = (WorldConfig.GetBoolValue(WorldCfg.CharacterCreatingDisableAlliedRaceAchievementRequirement) ? true : requirement.Value.AchievementId != 0 ? false : true); // TODO: fix false here for actual check of criteria.

			charResult.RaceUnlockData.Add(raceUnlock);
		}

		SendPacket(charResult);
	}

	
	[WorldPacketHandler(ClientOpcodes.PlayerLogin, Status = SessionStatus.Authed)]
	void HandlePlayerLogin(PlayerLogin playerLogin)
	{
		if (PlayerLoading || Player != null)
		{
			Log.outError(LogFilter.Network, "Player tries to login again, AccountId = {0}", AccountId);
			KickPlayer("WorldSession::HandlePlayerLoginOpcode Another client logging in");

			return;
		}

		_playerLoading = playerLogin.Guid;
		Log.outDebug(LogFilter.Network, "Character {0} logging in", playerLogin.Guid.ToString());

		if (!_legitCharacters.Contains(playerLogin.Guid))
		{
			Log.outError(LogFilter.Network, "Account ({0}) can't login with that character ({1}).", AccountId, playerLogin.Guid.ToString());
			KickPlayer("WorldSession::HandlePlayerLoginOpcode Trying to login with a character of another account");

			return;
		}

		SendConnectToInstance(ConnectToSerial.WorldAttempt1);
	}

	[WorldPacketHandler(ClientOpcodes.LoadingScreenNotify, Status = SessionStatus.Authed)]
	void HandleLoadScreen(LoadingScreenNotify loadingScreenNotify)
	{
		// TODO: Do something with this packet
	}

	[WorldPacketHandler(ClientOpcodes.RequestForcedReactions)]
	void HandleRequestForcedReactions(RequestForcedReactions requestForcedReactions)
	{
		Player.ReputationMgr.SendForceReactions();
	}


	[WorldPacketHandler(ClientOpcodes.AlterAppearance)]
	void HandleAlterAppearance(AlterApperance packet)
	{
		if (!ValidateAppearance(_player.Race, _player.Class, (Gender)packet.NewSex, packet.Customizations))
			return;

		var go = Player.FindNearestGameObjectOfType(GameObjectTypes.BarberChair, 5.0f);

		if (!go)
		{
			SendPacket(new BarberShopResult(BarberShopResult.ResultEnum.NotOnChair));

			return;
		}

		if (Player.StandState != (UnitStandStateType)((int)UnitStandStateType.SitLowChair + go.Template.BarberChair.chairheight))
		{
			SendPacket(new BarberShopResult(BarberShopResult.ResultEnum.NotOnChair));

			return;
		}

		var cost = Player.GetBarberShopCost(packet.Customizations);

		if (!Player.HasEnoughMoney(cost))
		{
			SendPacket(new BarberShopResult(BarberShopResult.ResultEnum.NoMoney));

			return;
		}

		SendPacket(new BarberShopResult(BarberShopResult.ResultEnum.Success));

		_player.ModifyMoney(-cost);
		_player.UpdateCriteria(CriteriaType.MoneySpentAtBarberShop, (ulong)cost);

		if (_player.NativeGender != (Gender)packet.NewSex)
		{
			_player.NativeGender = (Gender)packet.NewSex;
			_player.InitDisplayIds();
			_player.RestoreDisplayId(false);
		}

		_player.SetCustomizations(packet.Customizations);

		_player.UpdateCriteria(CriteriaType.GotHaircut, 1);

		_player.SetStandState(UnitStandStateType.Stand);

		Global.CharacterCacheStorage.UpdateCharacterGender(_player.GUID, packet.NewSex);
	}


	[WorldPacketHandler(ClientOpcodes.GetUndeleteCharacterCooldownStatus, Status = SessionStatus.Authed)]
	void HandleGetUndeleteCooldownStatus(GetUndeleteCharacterCooldownStatus getCooldown)
	{
		var stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_LAST_CHAR_UNDELETE);
		stmt.AddValue(0, BattlenetAccountId);

		_queryProcessor.AddCallback(DB.Login.AsyncQuery(stmt).WithCallback(HandleUndeleteCooldownStatusCallback));
	}

	void HandleUndeleteCooldownStatusCallback(SQLResult result)
	{
		uint cooldown = 0;
		var maxCooldown = WorldConfig.GetUIntValue(WorldCfg.FeatureSystemCharacterUndeleteCooldown);

		if (!result.IsEmpty())
		{
			var lastUndelete = result.Read<uint>(0);
			var now = (uint)GameTime.GetGameTime();

			if (lastUndelete + maxCooldown > now)
				cooldown = Math.Max(0, lastUndelete + maxCooldown - now);
		}

		SendUndeleteCooldownStatusResponse(cooldown, maxCooldown);
	}

	[WorldPacketHandler(ClientOpcodes.UndeleteCharacter, Status = SessionStatus.Authed)]
	void HandleCharUndelete(UndeleteCharacter undeleteCharacter)
	{
		if (!WorldConfig.GetBoolValue(WorldCfg.FeatureSystemCharacterUndeleteEnabled))
		{
			SendUndeleteCharacterResponse(CharacterUndeleteResult.Disabled, undeleteCharacter.UndeleteInfo);

			return;
		}

		var stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_LAST_CHAR_UNDELETE);
		stmt.AddValue(0, BattlenetAccountId);

		var undeleteInfo = undeleteCharacter.UndeleteInfo;

		_queryProcessor.AddCallback(DB.Login.AsyncQuery(stmt)
									.WithChainingCallback((queryCallback, result) =>
									{
										if (!result.IsEmpty())
										{
											var lastUndelete = result.Read<uint>(0);
											var maxCooldown = WorldConfig.GetUIntValue(WorldCfg.FeatureSystemCharacterUndeleteCooldown);

											if (lastUndelete != 0 && (lastUndelete + maxCooldown > GameTime.GetGameTime()))
											{
												SendUndeleteCharacterResponse(CharacterUndeleteResult.Cooldown, undeleteInfo);

												return;
											}
										}

										stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_DEL_INFO_BY_GUID);
										stmt.AddValue(0, undeleteInfo.CharacterGuid.Counter);
										queryCallback.SetNextQuery(DB.Characters.AsyncQuery(stmt));
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

										if (account != AccountId)
										{
											SendUndeleteCharacterResponse(CharacterUndeleteResult.Unknown, undeleteInfo);

											return;
										}

										stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHECK_NAME);
										stmt.AddValue(0, undeleteInfo.Name);
										queryCallback.SetNextQuery(DB.Characters.AsyncQuery(stmt));
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

										stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_SUM_CHARS);
										stmt.AddValue(0, AccountId);
										queryCallback.SetNextQuery(DB.Characters.AsyncQuery(stmt));
									})
									.WithCallback(result =>
									{
										if (!result.IsEmpty())
											if (result.Read<ulong>(0) >= WorldConfig.GetUIntValue(WorldCfg.CharactersPerRealm)) // SQL's COUNT() returns uint64 but it will always be less than uint8.Max
											{
												SendUndeleteCharacterResponse(CharacterUndeleteResult.CharCreate, undeleteInfo);

												return;
											}

										stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_RESTORE_DELETE_INFO);
										stmt.AddValue(0, undeleteInfo.Name);
										stmt.AddValue(1, AccountId);
										stmt.AddValue(2, undeleteInfo.CharacterGuid.Counter);
										DB.Characters.Execute(stmt);

										stmt = DB.Login.GetPreparedStatement(LoginStatements.UPD_LAST_CHAR_UNDELETE);
										stmt.AddValue(0, BattlenetAccountId);
										DB.Login.Execute(stmt);

										Global.CharacterCacheStorage.UpdateCharacterInfoDeleted(undeleteInfo.CharacterGuid, false, undeleteInfo.Name);

										SendUndeleteCharacterResponse(CharacterUndeleteResult.Ok, undeleteInfo);
									}));
	}

	[WorldPacketHandler(ClientOpcodes.RepopRequest)]
	void HandleRepopRequest(RepopRequest packet)
	{
		if (Player.IsAlive || Player.HasPlayerFlag(PlayerFlags.Ghost))
			return;

		if (Player.HasAuraType(AuraType.PreventResurrection))
			return; // silently return, client should display the error by itself

		// the world update order is sessions, players, creatures
		// the netcode runs in parallel with all of these
		// creatures can kill players
		// so if the server is lagging enough the player can
		// release spirit after he's killed but before he is updated
		if (Player.DeathState == DeathState.JustDied)
		{
			Log.outDebug(LogFilter.Network,
						"HandleRepopRequestOpcode: got request after player {0} ({1}) was killed and before he was updated",
						Player.GetName(),
						Player.GUID.ToString());

			Player.KillPlayer();
		}

		//this is spirit release confirm?
		Player.RemovePet(null, PetSaveMode.NotInSlot, true);
		Player.BuildPlayerRepop();
		Player.RepopAtGraveyard();
	}

	[WorldPacketHandler(ClientOpcodes.ClientPortGraveyard)]
	void HandlePortGraveyard(PortGraveyard packet)
	{
		if (Player.IsAlive || !Player.HasPlayerFlag(PlayerFlags.Ghost))
			return;

		Player.RepopAtGraveyard();
	}

	[WorldPacketHandler(ClientOpcodes.RequestCemeteryList, Processing = PacketProcessing.Inplace)]
	void HandleRequestCemeteryList(RequestCemeteryList requestCemeteryList)
	{
		var zoneId = Player.Zone;
		var team = (uint)Player.Team;

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
			Log.outDebug(LogFilter.Network,
						"No graveyards found for zone {0} for player {1} (team {2}) in CMSG_REQUEST_CEMETERY_LIST",
						zoneId,
						_guidLow,
						team);

			return;
		}

		RequestCemeteryListResponse packet = new();
		packet.IsGossipTriggered = false;

		foreach (var id in graveyardIds)
			packet.CemeteryID.Add(id);

		SendPacket(packet);
	}

	[WorldPacketHandler(ClientOpcodes.ReclaimCorpse)]
	void HandleReclaimCorpse(ReclaimCorpse packet)
	{
		if (Player.IsAlive)
			return;

		// do not allow corpse reclaim in arena
		if (Player.InArena)
			return;

		// body not released yet
		if (!Player.HasPlayerFlag(PlayerFlags.Ghost))
			return;

		var corpse = Player.GetCorpse();

		if (!corpse)
			return;

		// prevent resurrect before 30-sec delay after body release not finished
		if ((corpse.GetGhostTime() + Player.GetCorpseReclaimDelay(corpse.GetCorpseType() == CorpseType.ResurrectablePVP)) > GameTime.GetGameTime())
			return;

		if (!corpse.IsWithinDistInMap(Player, 39, true))
			return;

		// resurrect
		Player.ResurrectPlayer(Player.InBattleground ? 1.0f : 0.5f);

		// spawn bones
		Player.SpawnCorpseBones();
	}


	[WorldPacketHandler(ClientOpcodes.StandStateChange)]
	void HandleStandStateChange(StandStateChange packet)
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

		Player.SetStandState(packet.StandState);
	}

	[WorldPacketHandler(ClientOpcodes.QuickJoinAutoAcceptRequests)]
	void HandleQuickJoinAutoAcceptRequests(QuickJoinAutoAcceptRequest packet)
	{
		Player.AutoAcceptQuickJoin = packet.AutoAccept;
	}

	[WorldPacketHandler(ClientOpcodes.OverrideScreenFlash)]
	void HandleOverrideScreenFlash(OverrideScreenFlash packet)
	{
		Player.OverrideScreenFlash = packet.ScreenFlashEnabled;
	}
}

public class LoginQueryHolder : SQLQueryHolder<PlayerLoginQueryLoad>
{
	readonly uint m_accountId;
	ObjectGuid m_guid;

	public LoginQueryHolder(uint accountId, ObjectGuid guid)
	{
		m_accountId = accountId;
		m_guid = guid;
	}

	public void Initialize()
	{
		var lowGuid = m_guid.Counter;

		var stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.From, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_CUSTOMIZATIONS);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.Customizations, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_GROUP_MEMBER);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.Group, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_AURAS);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.Auras, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_AURA_EFFECTS);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.AuraEffects, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_AURA_STORED_LOCATIONS);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.AuraStoredLocations, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_SPELL);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.Spells, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_SPELL_FAVORITES);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.SpellFavorites, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_QUESTSTATUS);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.QuestStatus, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_QUESTSTATUS_OBJECTIVES);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.QuestStatusObjectives, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_QUESTSTATUS_OBJECTIVES_CRITERIA);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.QuestStatusObjectivesCriteria, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_QUESTSTATUS_OBJECTIVES_CRITERIA_PROGRESS);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.QuestStatusObjectivesCriteriaProgress, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_QUESTSTATUS_DAILY);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.DailyQuestStatus, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_QUESTSTATUS_WEEKLY);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.WeeklyQuestStatus, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_QUESTSTATUS_MONTHLY);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.MonthlyQuestStatus, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_QUESTSTATUS_SEASONAL);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.SeasonalQuestStatus, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_REPUTATION);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.Reputation, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_INVENTORY);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.Inventory, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_ITEM_INSTANCE_ARTIFACT);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.Artifacts, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_ITEM_INSTANCE_AZERITE);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.Azerite, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_ITEM_INSTANCE_AZERITE_MILESTONE_POWER);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.AzeriteMilestonePowers, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_ITEM_INSTANCE_AZERITE_UNLOCKED_ESSENCE);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.AzeriteUnlockedEssences, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_ITEM_INSTANCE_AZERITE_EMPOWERED);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.AzeriteEmpowered, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_VOID_STORAGE);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.VoidStorage, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAIL);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.Mails, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAILITEMS);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.MailItems, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAILITEMS_ARTIFACT);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.MailItemsArtifact, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAILITEMS_AZERITE);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.MailItemsAzerite, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAILITEMS_AZERITE_MILESTONE_POWER);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.MailItemsAzeriteMilestonePower, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAILITEMS_AZERITE_UNLOCKED_ESSENCE);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.MailItemsAzeriteUnlockedEssence, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAILITEMS_AZERITE_EMPOWERED);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.MailItemsAzeriteEmpowered, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_SOCIALLIST);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.SocialList, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_HOMEBIND);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.HomeBind, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_SPELLCOOLDOWNS);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.SpellCooldowns, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_SPELL_CHARGES);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.SpellCharges, stmt);

		if (WorldConfig.GetBoolValue(WorldCfg.DeclinedNamesUsed))
		{
			stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_DECLINEDNAMES);
			stmt.AddValue(0, lowGuid);
			SetQuery(PlayerLoginQueryLoad.DeclinedNames, stmt);
		}

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_GUILD_MEMBER);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.Guild, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_ARENAINFO);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.ArenaInfo, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_ACHIEVEMENTS);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.Achievements, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_CRITERIAPROGRESS);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.CriteriaProgress, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_EQUIPMENTSETS);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.EquipmentSets, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_TRANSMOG_OUTFITS);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.TransmogOutfits, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_CUF_PROFILES);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.CufProfiles, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_BGDATA);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.BgData, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_GLYPHS);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.Glyphs, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_TALENTS);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.Talents, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_PVP_TALENTS);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.PvpTalents, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_PLAYER_ACCOUNT_DATA);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.AccountData, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_SKILLS);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.Skills, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_RANDOMBG);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.RandomBg, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_BANNED);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.Banned, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_QUESTSTATUSREW);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.QuestStatusRew, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_ACCOUNT_INSTANCELOCKTIMES);
		stmt.AddValue(0, m_accountId);
		SetQuery(PlayerLoginQueryLoad.InstanceLockTimes, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_PLAYER_CURRENCY);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.Currency, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CORPSE_LOCATION);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.CorpseLocation, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_PETS);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.PetSlots, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_GARRISON);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.Garrison, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_GARRISON_BLUEPRINTS);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.GarrisonBlueprints, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_GARRISON_BUILDINGS);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.GarrisonBuildings, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_GARRISON_FOLLOWERS);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.GarrisonFollowers, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_GARRISON_FOLLOWER_ABILITIES);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.GarrisonFollowerAbilities, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_TRAIT_ENTRIES);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.TraitEntries, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_TRAIT_CONFIGS);
		stmt.AddValue(0, lowGuid);
		SetQuery(PlayerLoginQueryLoad.TraitConfigs, stmt);
	}

	public ObjectGuid GetGuid()
	{
		return m_guid;
	}

	uint GetAccountId()
	{
		return m_accountId;
	}
}

class EnumCharactersQueryHolder : SQLQueryHolder<EnumCharacterQueryLoad>
{
	bool _isDeletedCharacters = false;

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

		var result = true;
		var stmt = DB.Characters.GetPreparedStatement(statements[isDeletedCharacters ? 1 : 0][withDeclinedNames ? 1 : 0]);
		stmt.AddValue(0, accountId);
		SetQuery(EnumCharacterQueryLoad.Characters, stmt);

		stmt = DB.Characters.GetPreparedStatement(statements[isDeletedCharacters ? 1 : 0][2]);
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