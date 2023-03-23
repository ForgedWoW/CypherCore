// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Configuration;
using Framework.Constants;
using Framework.Database;
using Game.Common.DataStorage.ClientReader;
using Game.Common.DataStorage.Structs.C;
using Game.Common.Entities.Objects;
using Game.Common.Entities.Objects.Update;
using Game.Common.Entities.Players;
using Game.Common.Networking;
using Game.Common.Networking.Packets.Character;
using Game.Common.Networking.Packets.Misc;
using Game.Common.Networking.Packets.System;
using Game.Common.Server;

namespace Game.Common.Handlers;

public class CharacterHandler : IWorldSessionHandler
{
    private readonly WorldSession _session;
    private readonly CollectionMgr _collectionMgr;
    private readonly DB6Storage<ChrCustomizationReqRecord> _charCustomizationReqRecords;
    readonly List<ObjectGuid> _legitCharacters = new();

    public CharacterHandler(WorldSession session, CollectionMgr collectionMgr, DB6Storage<ChrCustomizationReqRecord> charCustomizationReqRecords)
    {
        _session = session;
        _collectionMgr = collectionMgr;
        _charCustomizationReqRecords = charCustomizationReqRecords;
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
			if (_session.Player == null)
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

			var req = _charCustomizationReqRecords.LookupByKey(customizationOptionData.ChrCustomizationReqID);

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

			var reqEntry = _charCustomizationReqRecords.LookupByKey(customizationChoiceData.ChrCustomizationReqID);

			if (reqEntry != null)
				if (!MeetsChrCustomizationReq(reqEntry, playerClass, true, customizations))
					return false;
		}

		return true;
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
		features.IsMuted = !_session.CanSpeak;


		features.TextToSpeechFeatureEnabled = false;

        _session.SendPacket(features);
	}

	[WorldPacketHandler(ClientOpcodes.EnumCharacters, Status = SessionStatus.Authed)]
	void HandleCharEnum(EnumCharacters charEnum)
	{
		// remove expired bans
		var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_EXPIRED_BANS);
		DB.Characters.Execute(stmt);

		// get all the data necessary for loading all characters (along with their pets) on the account
		EnumCharactersQueryHolder holder = new();

		if (!holder.Initialize(_session.AccountId, WorldConfig.GetBoolValue(WorldCfg.DeclinedNamesUsed), false))
		{
			HandleCharEnum(holder);

			return;
		}

        _session.AddQueryHolderCallback(DB.Characters.DelayQueryHolder(holder)).AfterComplete(result => HandleCharEnum((EnumCharactersQueryHolder)result));
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

				Log.outDebug(LogFilter.Network, "Loading Character {0} from account {1}.", charInfo.Guid.ToString(), _session.AccountId);

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
					Global.CharacterCacheStorage.AddCharacterCacheEntry(charInfo.Guid, _session.AccountId, charInfo.Name, charInfo.SexId, charInfo.RaceId, (byte)charInfo.ClassId, charInfo.ExperienceLevel, false);

				charResult.MaxCharacterLevel = Math.Max(charResult.MaxCharacterLevel, charInfo.ExperienceLevel);

				charResult.Characters.Add(charInfo);
			} while (result.NextRow() && charResult.Characters.Count < 200);

		charResult.IsAlliedRacesCreationAllowed = _session.CanAccessAlliedRaces();

		foreach (var requirement in Global.ObjectMgr.GetRaceUnlockRequirements())
		{
			EnumCharactersResult.RaceUnlock raceUnlock = new();
			raceUnlock.RaceID = requirement.Key;
			raceUnlock.HasExpansion = ConfigMgr.GetDefaultValue("character.EnforceRaceAndClassExpansions", true) ? (byte)_session.AccountExpansion >= requirement.Value.Expansion : true;
			raceUnlock.HasAchievement = (WorldConfig.GetBoolValue(WorldCfg.CharacterCreatingDisableAlliedRaceAchievementRequirement) ? true : requirement.Value.AchievementId != 0 ? false : true); // TODO: fix false here for actual check of criteria.

			charResult.RaceUnlockData.Add(raceUnlock);
		}

        _session.SendPacket(charResult);
	}

	
	[WorldPacketHandler(ClientOpcodes.PlayerLogin, Status = SessionStatus.Authed)]
	void HandlePlayerLogin(PlayerLogin playerLogin)
	{
		if (_session.IsPlayerLoading || _session.Player != null)
		{
			Log.outError(LogFilter.Network, "Player tries to login again, AccountId = {0}", _session.AccountId);
            _session.KickPlayer("WorldSession::HandlePlayerLoginOpcode Another client logging in");

			return;
		}

        _session.PlayerLoading = playerLogin.Guid;
		Log.outDebug(LogFilter.Network, "Character {0} logging in", playerLogin.Guid.ToString());

		if (!_legitCharacters.Contains(playerLogin.Guid))
		{
			Log.outError(LogFilter.Network, "Account ({0}) can't login with that character ({1}).", _session.AccountId, playerLogin.Guid.ToString());
            _session.KickPlayer("WorldSession::HandlePlayerLoginOpcode Trying to login with a character of another account");

			return;
		}

        _session.SendConnectToInstance(ConnectToSerial.WorldAttempt1);
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
			Log.outDebug(LogFilter.Network,
						"No graveyards found for zone {0} for player {1} (team {2}) in CMSG_REQUEST_CEMETERY_LIST",
						zoneId,
						_session.GuidLow,
						team);

			return;
		}

		RequestCemeteryListResponse packet = new();
		packet.IsGossipTriggered = false;

		foreach (var id in graveyardIds)
			packet.CemeteryID.Add(id);

        _session.SendPacket(packet);
	}

    [WorldPacketHandler(ClientOpcodes.QuickJoinAutoAcceptRequests)]
	void HandleQuickJoinAutoAcceptRequests(QuickJoinAutoAcceptRequest packet)
	{
		_session.Player.AutoAcceptQuickJoin = packet.AutoAccept;
	}
}

enum EnumCharacterQueryLoad
{
    Characters,
    Customizations
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
