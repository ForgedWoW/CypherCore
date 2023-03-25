// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Scripting.Interfaces.IWorld;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.Server;

public class WorldConfig
{
    private static readonly Dictionary<WorldCfg, object> Values = new();

	public static void Load(bool reload = false)
	{
		// Read other configuration items from the config file
		Values[WorldCfg.DurabilityLossInPvp] = GetDefaultValue("DurabilityLoss.InPvP", false);

		Values[WorldCfg.Compression] = GetDefaultValue("Compression", 1);

		if ((int)Values[WorldCfg.Compression] < 1 || (int)Values[WorldCfg.Compression] > 9)
		{
			Log.Logger.Error("Compression Level ({0}) must be in range 1..9. Using default compression Level (1).", Values[WorldCfg.Compression]);
			Values[WorldCfg.Compression] = 1;
		}

		Values[WorldCfg.AddonChannel] = GetDefaultValue("AddonChannel", true);
		Values[WorldCfg.CleanCharacterDb] = GetDefaultValue("CleanCharacterDB", false);
		Values[WorldCfg.PersistentCharacterCleanFlags] = GetDefaultValue("PersistentCharacterCleanFlags", 0);
		Values[WorldCfg.AuctionReplicateDelay] = GetDefaultValue("Auction.ReplicateItemsCooldown", 900);
		Values[WorldCfg.AuctionSearchDelay] = GetDefaultValue("Auction.SearchDelay", 300);

		if ((int)Values[WorldCfg.AuctionSearchDelay] < 100 || (int)Values[WorldCfg.AuctionSearchDelay] > 10000)
		{
			Log.Logger.Error("Auction.SearchDelay ({0}) must be between 100 and 10000. Using default of 300ms", Values[WorldCfg.AuctionSearchDelay]);
			Values[WorldCfg.AuctionSearchDelay] = 300;
		}

		Values[WorldCfg.AuctionTaintedSearchDelay] = GetDefaultValue("Auction.TaintedSearchDelay", 3000);

		if ((int)Values[WorldCfg.AuctionTaintedSearchDelay] < 100 || (int)Values[WorldCfg.AuctionTaintedSearchDelay] > 10000)
		{
			Log.Logger.Error($"Auction.TaintedSearchDelay ({Values[WorldCfg.AuctionTaintedSearchDelay]}) must be between 100 and 10000. Using default of 3s");
			Values[WorldCfg.AuctionTaintedSearchDelay] = 3000;
		}

		Values[WorldCfg.ChatChannelLevelReq] = GetDefaultValue("ChatLevelReq.Channel", 1);
		Values[WorldCfg.ChatWhisperLevelReq] = GetDefaultValue("ChatLevelReq.Whisper", 1);
		Values[WorldCfg.ChatEmoteLevelReq] = GetDefaultValue("ChatLevelReq.Emote", 1);
		Values[WorldCfg.ChatSayLevelReq] = GetDefaultValue("ChatLevelReq.Say", 1);
		Values[WorldCfg.ChatYellLevelReq] = GetDefaultValue("ChatLevelReq.Yell", 1);
		Values[WorldCfg.PartyLevelReq] = GetDefaultValue("PartyLevelReq", 1);
		Values[WorldCfg.TradeLevelReq] = GetDefaultValue("LevelReq.Trade", 1);
		Values[WorldCfg.AuctionLevelReq] = GetDefaultValue("LevelReq.Auction", 1);
		Values[WorldCfg.MailLevelReq] = GetDefaultValue("LevelReq.Mail", 1);
		Values[WorldCfg.PreserveCustomChannels] = GetDefaultValue("PreserveCustomChannels", false);
		Values[WorldCfg.PreserveCustomChannelDuration] = GetDefaultValue("PreserveCustomChannelDuration", 14);
		Values[WorldCfg.PreserveCustomChannelInterval] = GetDefaultValue("PreserveCustomChannelInterval", 5);
		Values[WorldCfg.GridUnload] = GetDefaultValue("GridUnload", true);
		Values[WorldCfg.BasemapLoadGrids] = GetDefaultValue("BaseMapLoadAllGrids", false);

		if ((bool)Values[WorldCfg.BasemapLoadGrids] && (bool)Values[WorldCfg.GridUnload])
		{
			Log.Logger.Error("BaseMapLoadAllGrids enabled, but GridUnload also enabled. GridUnload must be disabled to enable base map pre-loading. Base map pre-loading disabled");
			Values[WorldCfg.BasemapLoadGrids] = false;
		}

		Values[WorldCfg.InstancemapLoadGrids] = GetDefaultValue("InstanceMapLoadAllGrids", false);

		if ((bool)Values[WorldCfg.InstancemapLoadGrids] && (bool)Values[WorldCfg.GridUnload])
		{
			Log.Logger.Error("InstanceMapLoadAllGrids enabled, but GridUnload also enabled. GridUnload must be disabled to enable instance map pre-loading. Instance map pre-loading disabled");
			Values[WorldCfg.InstancemapLoadGrids] = false;
		}

		Values[WorldCfg.IntervalSave] = GetDefaultValue("PlayerSaveInterval", 15 * Time.Minute * Time.InMilliseconds);
		Values[WorldCfg.IntervalDisconnectTolerance] = GetDefaultValue("DisconnectToleranceInterval", 0);
		Values[WorldCfg.StatsSaveOnlyOnLogout] = GetDefaultValue("PlayerSave.Stats.SaveOnlyOnLogout", true);

		Values[WorldCfg.MinLevelStatSave] = GetDefaultValue("PlayerSave.Stats.MinLevel", 0);

		if ((int)Values[WorldCfg.MinLevelStatSave] > SharedConst.MaxLevel)
		{
			Log.Logger.Error("PlayerSave.Stats.MinLevel ({0}) must be in range 0..80. Using default, do not save character stats (0).", Values[WorldCfg.MinLevelStatSave]);
			Values[WorldCfg.MinLevelStatSave] = 0;
		}

		Values[WorldCfg.IntervalGridclean] = GetDefaultValue("GridCleanUpDelay", 5 * Time.Minute * Time.InMilliseconds);

		if ((int)Values[WorldCfg.IntervalGridclean] < MapConst.MinGridDelay)
		{
			Log.Logger.Error("GridCleanUpDelay ({0}) must be greater {1} Use this minimal value.", Values[WorldCfg.IntervalGridclean], MapConst.MinGridDelay);
			Values[WorldCfg.IntervalGridclean] = MapConst.MinGridDelay;
		}

		Values[WorldCfg.IntervalMapupdate] = GetDefaultValue("MapUpdateInterval", 10);

		if ((int)Values[WorldCfg.IntervalMapupdate] < MapConst.MinMapUpdateDelay)
		{
			Log.Logger.Error("MapUpdateInterval ({0}) must be greater {1}. Use this minimal value.", Values[WorldCfg.IntervalMapupdate], MapConst.MinMapUpdateDelay);
			Values[WorldCfg.IntervalMapupdate] = MapConst.MinMapUpdateDelay;
		}

		Values[WorldCfg.IntervalChangeweather] = GetDefaultValue("ChangeWeatherInterval", 10 * Time.Minute * Time.InMilliseconds);

		if (reload)
		{
			var val = GetDefaultValue("WorldServerPort", 8085);

			if (val != (int)Values[WorldCfg.PortWorld])
				Log.Logger.Error("WorldServerPort option can't be changed at worldserver.conf reload, using current value ({0}).", Values[WorldCfg.PortWorld]);

			val = GetDefaultValue("InstanceServerPort", 8086);

			if (val != (int)Values[WorldCfg.PortInstance])
				Log.Logger.Error("InstanceServerPort option can't be changed at worldserver.conf reload, using current value ({0}).", Values[WorldCfg.PortInstance]);
		}
		else
		{
			Values[WorldCfg.PortWorld] = GetDefaultValue("WorldServerPort", 8085);
			Values[WorldCfg.PortInstance] = GetDefaultValue("InstanceServerPort", 8086);
		}

		// Config values are in "milliseconds" but we handle SocketTimeOut only as "seconds" so divide by 1000
		Values[WorldCfg.SocketTimeoutTime] = GetDefaultValue("SocketTimeOutTime", 900000) / 1000;
		Values[WorldCfg.SocketTimeoutTimeActive] = GetDefaultValue("SocketTimeOutTimeActive", 60000) / 1000;
		Values[WorldCfg.SessionAddDelay] = GetDefaultValue("SessionAddDelay", 10000);

		Values[WorldCfg.GroupXpDistance] = GetDefaultValue("MaxGroupXPDistance", 74.0f);
		Values[WorldCfg.MaxRecruitAFriendDistance] = GetDefaultValue("MaxRecruitAFriendBonusDistance", 100.0f);
		Values[WorldCfg.MinQuestScaledXpRatio] = GetDefaultValue("MinQuestScaledXPRatio", 0);

		if ((int)Values[WorldCfg.MinQuestScaledXpRatio] > 100)
		{
			Log.Logger.Error($"MinQuestScaledXPRatio ({Values[WorldCfg.MinQuestScaledXpRatio]}) must be in range 0..100. Set to 0.");
			Values[WorldCfg.MinQuestScaledXpRatio] = 0;
		}

		Values[WorldCfg.MinCreatureScaledXpRatio] = GetDefaultValue("MinCreatureScaledXPRatio", 0);

		if ((int)Values[WorldCfg.MinCreatureScaledXpRatio] > 100)
		{
			Log.Logger.Error($"MinCreatureScaledXPRatio ({Values[WorldCfg.MinCreatureScaledXpRatio]}) must be in range 0..100. Set to 0.");
			Values[WorldCfg.MinCreatureScaledXpRatio] = 0;
		}

		Values[WorldCfg.MinDiscoveredScaledXpRatio] = GetDefaultValue("MinDiscoveredScaledXPRatio", 0);

		if ((int)Values[WorldCfg.MinDiscoveredScaledXpRatio] > 100)
		{
			Log.Logger.Error($"MinDiscoveredScaledXPRatio ({Values[WorldCfg.MinDiscoveredScaledXpRatio]}) must be in range 0..100. Set to 0.");
			Values[WorldCfg.MinDiscoveredScaledXpRatio] = 0;
		}

		/// @todo Add MonsterSight (with meaning) in worldserver.conf or put them as define
		Values[WorldCfg.SightMonster] = GetDefaultValue("MonsterSight", 50.0f);

		Values[WorldCfg.RegenHpCannotReachTargetInRaid] = GetDefaultValue("Creature.RegenHPCannotReachTargetInRaid", true);

		if (reload)
		{
			var val = GetDefaultValue("GameType", 0);

			if (val != (int)Values[WorldCfg.GameType])
				Log.Logger.Error("GameType option can't be changed at worldserver.conf reload, using current value ({0}).", Values[WorldCfg.GameType]);
		}
		else
		{
			Values[WorldCfg.GameType] = GetDefaultValue("GameType", 0);
		}

		if (reload)
		{
			var val = (int)GetDefaultValue("RealmZone", RealmZones.Development);

			if (val != (int)Values[WorldCfg.RealmZone])
				Log.Logger.Error("RealmZone option can't be changed at worldserver.conf reload, using current value ({0}).", Values[WorldCfg.RealmZone]);
		}
		else
		{
			Values[WorldCfg.RealmZone] = GetDefaultValue("RealmZone", (int)RealmZones.Development);
		}

		Values[WorldCfg.AllowTwoSideInteractionCalendar] = GetDefaultValue("AllowTwoSide.Interaction.Calendar", false);
		Values[WorldCfg.AllowTwoSideInteractionChannel] = GetDefaultValue("AllowTwoSide.Interaction.Channel", false);
		Values[WorldCfg.AllowTwoSideInteractionGroup] = GetDefaultValue("AllowTwoSide.Interaction.Group", false);
		Values[WorldCfg.AllowTwoSideInteractionGuild] = GetDefaultValue("AllowTwoSide.Interaction.Guild", false);
		Values[WorldCfg.AllowTwoSideInteractionAuction] = GetDefaultValue("AllowTwoSide.Interaction.Auction", true);
		Values[WorldCfg.AllowTwoSideTrade] = GetDefaultValue("AllowTwoSide.Trade", false);
		Values[WorldCfg.StrictPlayerNames] = GetDefaultValue("StrictPlayerNames", 0);
		Values[WorldCfg.StrictCharterNames] = GetDefaultValue("StrictCharterNames", 0);
		Values[WorldCfg.StrictPetNames] = GetDefaultValue("StrictPetNames", 0);

		Values[WorldCfg.MinPlayerName] = GetDefaultValue("MinPlayerName", 2);

		if ((int)Values[WorldCfg.MinPlayerName] < 1 || (int)Values[WorldCfg.MinPlayerName] > 12)
		{
			Log.Logger.Error("MinPlayerName ({0}) must be in range 1..{1}. Set to 2.", Values[WorldCfg.MinPlayerName], 12);
			Values[WorldCfg.MinPlayerName] = 2;
		}

		Values[WorldCfg.MinCharterName] = GetDefaultValue("MinCharterName", 2);

		if ((int)Values[WorldCfg.MinCharterName] < 1 || (int)Values[WorldCfg.MinCharterName] > 24)
		{
			Log.Logger.Error("MinCharterName ({0}) must be in range 1..{1}. Set to 2.", Values[WorldCfg.MinCharterName], 24);
			Values[WorldCfg.MinCharterName] = 2;
		}

		Values[WorldCfg.MinPetName] = GetDefaultValue("MinPetName", 2);

		if ((int)Values[WorldCfg.MinPetName] < 1 || (int)Values[WorldCfg.MinPetName] > 12)
		{
			Log.Logger.Error("MinPetName ({0}) must be in range 1..{1}. Set to 2.", Values[WorldCfg.MinPetName], 12);
			Values[WorldCfg.MinPetName] = 2;
		}

		Values[WorldCfg.CharterCostGuild] = GetDefaultValue("Guild.CharterCost", 1000);
		Values[WorldCfg.CharterCostArena2v2] = GetDefaultValue("ArenaTeam.CharterCost.2v2", 800000);
		Values[WorldCfg.CharterCostArena3v3] = GetDefaultValue("ArenaTeam.CharterCost.3v3", 1200000);
		Values[WorldCfg.CharterCostArena5v5] = GetDefaultValue("ArenaTeam.CharterCost.5v5", 2000000);

		Values[WorldCfg.CharacterCreatingDisabled] = GetDefaultValue("CharacterCreating.Disabled", 0);
		Values[WorldCfg.CharacterCreatingDisabledRacemask] = GetDefaultValue("CharacterCreating.Disabled.RaceMask", 0);
		Values[WorldCfg.CharacterCreatingDisabledClassmask] = GetDefaultValue("CharacterCreating.Disabled.ClassMask", 0);

		Values[WorldCfg.CharactersPerRealm] = GetDefaultValue("CharactersPerRealm", 60);

		if ((int)Values[WorldCfg.CharactersPerRealm] < 1 || (int)Values[WorldCfg.CharactersPerRealm] > 200)
		{
			Log.Logger.Error("CharactersPerRealm ({0}) must be in range 1..200. Set to 200.", Values[WorldCfg.CharactersPerRealm]);
			Values[WorldCfg.CharactersPerRealm] = 200;
		}

		// must be after CharactersPerRealm
		Values[WorldCfg.CharactersPerAccount] = GetDefaultValue("CharactersPerAccount", 60);

		if ((int)Values[WorldCfg.CharactersPerAccount] < (int)Values[WorldCfg.CharactersPerRealm])
		{
			Log.Logger.Error("CharactersPerAccount ({0}) can't be less than CharactersPerRealm ({1}).", Values[WorldCfg.CharactersPerAccount], Values[WorldCfg.CharactersPerRealm]);
			Values[WorldCfg.CharactersPerAccount] = Values[WorldCfg.CharactersPerRealm];
		}

		Values[WorldCfg.CharacterCreatingEvokersPerRealm] = GetDefaultValue("CharacterCreating.EvokersPerRealm", 1);

		if ((int)Values[WorldCfg.CharacterCreatingEvokersPerRealm] < 0 || (int)Values[WorldCfg.CharacterCreatingEvokersPerRealm] > 10)
		{
			Log.Logger.Error($"CharacterCreating.EvokersPerRealm ({Values[WorldCfg.CharacterCreatingEvokersPerRealm]}) must be in range 0..10. Set to 1.");
			Values[WorldCfg.CharacterCreatingEvokersPerRealm] = 1;
		}

		Values[WorldCfg.CharacterCreatingMinLevelForDemonHunter] = GetDefaultValue("CharacterCreating.MinLevelForDemonHunter", 0);
		Values[WorldCfg.CharacterCreatingMinLevelForEvoker] = GetDefaultValue("CharacterCreating.MinLevelForEvoker", 50);
		Values[WorldCfg.CharacterCreatingDisableAlliedRaceAchievementRequirement] = GetDefaultValue("CharacterCreating.DisableAlliedRaceAchievementRequirement", false);

		Values[WorldCfg.SkipCinematics] = GetDefaultValue("SkipCinematics", 0);

		if ((int)Values[WorldCfg.SkipCinematics] < 0 || (int)Values[WorldCfg.SkipCinematics] > 2)
		{
			Log.Logger.Error("SkipCinematics ({0}) must be in range 0..2. Set to 0.", Values[WorldCfg.SkipCinematics]);
			Values[WorldCfg.SkipCinematics] = 0;
		}

		if (reload)
		{
			var val = GetDefaultValue("MaxPlayerLevel", SharedConst.DefaultMaxLevel);

			if (val != (int)Values[WorldCfg.MaxPlayerLevel])
				Log.Logger.Error("MaxPlayerLevel option can't be changed at config reload, using current value ({0}).", Values[WorldCfg.MaxPlayerLevel]);
		}
		else
		{
			Values[WorldCfg.MaxPlayerLevel] = GetDefaultValue("MaxPlayerLevel", SharedConst.DefaultMaxLevel);
		}

		if ((int)Values[WorldCfg.MaxPlayerLevel] > SharedConst.MaxLevel)
		{
			Log.Logger.Error("MaxPlayerLevel ({0}) must be in range 1..{1}. Set to {1}.", Values[WorldCfg.MaxPlayerLevel], SharedConst.MaxLevel);
			Values[WorldCfg.MaxPlayerLevel] = SharedConst.MaxLevel;
		}

		Values[WorldCfg.MinDualspecLevel] = GetDefaultValue("MinDualSpecLevel", 40);

		Values[WorldCfg.StartPlayerLevel] = GetDefaultValue("StartPlayerLevel", 1);

		if ((int)Values[WorldCfg.StartPlayerLevel] < 1)
		{
			Log.Logger.Error("StartPlayerLevel ({0}) must be in range 1..MaxPlayerLevel({1}). Set to 1.", Values[WorldCfg.StartPlayerLevel], Values[WorldCfg.MaxPlayerLevel]);
			Values[WorldCfg.StartPlayerLevel] = 1;
		}
		else if ((int)Values[WorldCfg.StartPlayerLevel] > (int)Values[WorldCfg.MaxPlayerLevel])
		{
			Log.Logger.Error("StartPlayerLevel ({0}) must be in range 1..MaxPlayerLevel({1}). Set to {2}.", Values[WorldCfg.StartPlayerLevel], Values[WorldCfg.MaxPlayerLevel], Values[WorldCfg.MaxPlayerLevel]);
			Values[WorldCfg.StartPlayerLevel] = Values[WorldCfg.MaxPlayerLevel];
		}

		Values[WorldCfg.StartDeathKnightPlayerLevel] = GetDefaultValue("StartDeathKnightPlayerLevel", 8);

		if ((int)Values[WorldCfg.StartDeathKnightPlayerLevel] < 1)
		{
			Log.Logger.Error("StartDeathKnightPlayerLevel ({0}) must be in range 1..MaxPlayerLevel({1}). Set to 1.",
							Values[WorldCfg.StartDeathKnightPlayerLevel],
							Values[WorldCfg.MaxPlayerLevel]);

			Values[WorldCfg.StartDeathKnightPlayerLevel] = 1;
		}
		else if ((int)Values[WorldCfg.StartDeathKnightPlayerLevel] > (int)Values[WorldCfg.MaxPlayerLevel])
		{
			Log.Logger.Error("StartDeathKnightPlayerLevel ({0}) must be in range 1..MaxPlayerLevel({1}). Set to {2}.",
							Values[WorldCfg.StartDeathKnightPlayerLevel],
							Values[WorldCfg.MaxPlayerLevel],
							Values[WorldCfg.MaxPlayerLevel]);

			Values[WorldCfg.StartDeathKnightPlayerLevel] = Values[WorldCfg.MaxPlayerLevel];
		}

		Values[WorldCfg.StartDemonHunterPlayerLevel] = GetDefaultValue("StartDemonHunterPlayerLevel", 8);

		if ((int)Values[WorldCfg.StartDemonHunterPlayerLevel] < 1)
		{
			Log.Logger.Error("StartDemonHunterPlayerLevel ({0}) must be in range 1..MaxPlayerLevel({1}). Set to 1.",
							Values[WorldCfg.StartDemonHunterPlayerLevel],
							Values[WorldCfg.MaxPlayerLevel]);

			Values[WorldCfg.StartDemonHunterPlayerLevel] = 1;
		}
		else if ((int)Values[WorldCfg.StartDemonHunterPlayerLevel] > (int)Values[WorldCfg.MaxPlayerLevel])
		{
			Log.Logger.Error("StartDemonHunterPlayerLevel ({0}) must be in range 1..MaxPlayerLevel({1}). Set to {2}.",
							Values[WorldCfg.StartDemonHunterPlayerLevel],
							Values[WorldCfg.MaxPlayerLevel],
							Values[WorldCfg.MaxPlayerLevel]);

			Values[WorldCfg.StartDemonHunterPlayerLevel] = Values[WorldCfg.MaxPlayerLevel];
		}

		Values[WorldCfg.StartEvokerPlayerLevel] = GetDefaultValue("StartEvokerPlayerLevel", 58);

		if ((int)Values[WorldCfg.StartEvokerPlayerLevel] < 1)
		{
			Log.Logger.Error($"StartEvokerPlayerLevel ({Values[WorldCfg.StartEvokerPlayerLevel]}) must be in range 1..MaxPlayerLevel({Values[WorldCfg.MaxPlayerLevel]}). Set to 1.");
			Values[WorldCfg.StartEvokerPlayerLevel] = 1;
		}
		else if ((int)Values[WorldCfg.StartEvokerPlayerLevel] > (int)Values[WorldCfg.MaxPlayerLevel])
		{
			Log.Logger.Error($"StartEvokerPlayerLevel ({Values[WorldCfg.StartEvokerPlayerLevel]}) must be in range 1..MaxPlayerLevel({Values[WorldCfg.MaxPlayerLevel]}). Set to {Values[WorldCfg.MaxPlayerLevel]}.");
			Values[WorldCfg.StartEvokerPlayerLevel] = Values[WorldCfg.MaxPlayerLevel];
		}

		Values[WorldCfg.StartAlliedRaceLevel] = GetDefaultValue("StartAlliedRacePlayerLevel", 10);

		if ((int)Values[WorldCfg.StartAlliedRaceLevel] < 1)
		{
			Log.Logger.Error($"StartAlliedRaceLevel ({Values[WorldCfg.StartAlliedRaceLevel]}) must be in range 1..MaxPlayerLevel({Values[WorldCfg.MaxPlayerLevel]}). Set to 1.");
			Values[WorldCfg.StartAlliedRaceLevel] = 1;
		}
		else if ((int)Values[WorldCfg.StartAlliedRaceLevel] > (int)Values[WorldCfg.MaxPlayerLevel])
		{
			Log.Logger.Error($"StartAlliedRaceLevel ({Values[WorldCfg.StartAlliedRaceLevel]}) must be in range 1..MaxPlayerLevel({Values[WorldCfg.MaxPlayerLevel]}). Set to {Values[WorldCfg.MaxPlayerLevel]}.");
			Values[WorldCfg.StartAlliedRaceLevel] = Values[WorldCfg.MaxPlayerLevel];
		}

		Values[WorldCfg.StartPlayerMoney] = GetDefaultValue("StartPlayerMoney", 0);

		if ((int)Values[WorldCfg.StartPlayerMoney] < 0)
		{
			Log.Logger.Error("StartPlayerMoney ({0}) must be in range 0..{1}. Set to {2}.", Values[WorldCfg.StartPlayerMoney], PlayerConst.MaxMoneyAmount, 0);
			Values[WorldCfg.StartPlayerMoney] = 0;
		}
		else if ((int)Values[WorldCfg.StartPlayerMoney] > 0x7FFFFFFF - 1) // TODO: (See MaxMoneyAMOUNT)
		{
			Log.Logger.Error("StartPlayerMoney ({0}) must be in range 0..{1}. Set to {2}.",
							Values[WorldCfg.StartPlayerMoney],
							0x7FFFFFFF - 1,
							0x7FFFFFFF - 1);

			Values[WorldCfg.StartPlayerMoney] = 0x7FFFFFFF - 1;
		}

		Values[WorldCfg.CurrencyResetHour] = GetDefaultValue("Currency.ResetHour", 3);

		if ((int)Values[WorldCfg.CurrencyResetHour] > 23)
			Log.Logger.Error("StartPlayerMoney ({0}) must be in range 0..{1}. Set to {2}.", Values[WorldCfg.CurrencyResetHour] = 3);

		Values[WorldCfg.CurrencyResetDay] = GetDefaultValue("Currency.ResetDay", 3);

		if ((int)Values[WorldCfg.CurrencyResetDay] > 6)
		{
			Log.Logger.Error("Currency.ResetDay ({0}) can't be load. Set to 3.", Values[WorldCfg.CurrencyResetDay]);
			Values[WorldCfg.CurrencyResetDay] = 3;
		}

		Values[WorldCfg.CurrencyResetInterval] = GetDefaultValue("Currency.ResetInterval", 7);

		if ((int)Values[WorldCfg.CurrencyResetInterval] <= 0)
		{
			Log.Logger.Error("Currency.ResetInterval ({0}) must be > 0, set to default 7.", Values[WorldCfg.CurrencyResetInterval]);
			Values[WorldCfg.CurrencyResetInterval] = 7;
		}

		Values[WorldCfg.MaxRecruitAFriendBonusPlayerLevel] = GetDefaultValue("RecruitAFriend.MaxLevel", 60);

		if ((int)Values[WorldCfg.MaxRecruitAFriendBonusPlayerLevel] > (int)Values[WorldCfg.MaxPlayerLevel])
		{
			Log.Logger.Error("RecruitAFriend.MaxLevel ({0}) must be in the range 0..MaxLevel({1}). Set to {2}.",
							Values[WorldCfg.MaxRecruitAFriendBonusPlayerLevel],
							Values[WorldCfg.MaxPlayerLevel],
							60);

			Values[WorldCfg.MaxRecruitAFriendBonusPlayerLevel] = 60;
		}

		Values[WorldCfg.MaxRecruitAFriendBonusPlayerLevelDifference] = GetDefaultValue("RecruitAFriend.MaxDifference", 4);
		Values[WorldCfg.AllTaxiPaths] = GetDefaultValue("AllFlightPaths", false);
		Values[WorldCfg.InstantTaxi] = GetDefaultValue("InstantFlightPaths", false);

		Values[WorldCfg.InstanceIgnoreLevel] = GetDefaultValue("Instance.IgnoreLevel", false);
		Values[WorldCfg.InstanceIgnoreRaid] = GetDefaultValue("Instance.IgnoreRaid", false);

		Values[WorldCfg.CastUnstuck] = GetDefaultValue("CastUnstuck", true);
		Values[WorldCfg.ResetScheduleWeekDay] = GetDefaultValue("ResetSchedule.WeekDay", 2);
		Values[WorldCfg.ResetScheduleHour] = GetDefaultValue("ResetSchedule.Hour", 8);
		Values[WorldCfg.InstanceUnloadDelay] = GetDefaultValue("Instance.UnloadDelay", 30 * Time.Minute * Time.InMilliseconds);
		Values[WorldCfg.DailyQuestResetTimeHour] = GetDefaultValue("Quests.DailyResetTime", 3);

		if ((int)Values[WorldCfg.DailyQuestResetTimeHour] > 23)
		{
			Log.Logger.Error($"Quests.DailyResetTime ({Values[WorldCfg.DailyQuestResetTimeHour]}) must be in range 0..23. Set to 3.");
			Values[WorldCfg.DailyQuestResetTimeHour] = 3;
		}

		Values[WorldCfg.WeeklyQuestResetTimeWDay] = GetDefaultValue("Quests.WeeklyResetWDay", 3);

		if ((int)Values[WorldCfg.WeeklyQuestResetTimeWDay] > 6)
		{
			Log.Logger.Error($"Quests.WeeklyResetDay ({Values[WorldCfg.WeeklyQuestResetTimeWDay]}) must be in range 0..6. Set to 3 (Wednesday).");
			Values[WorldCfg.WeeklyQuestResetTimeWDay] = 3;
		}

		Values[WorldCfg.MaxPrimaryTradeSkill] = GetDefaultValue("MaxPrimaryTradeSkill", 2);
		Values[WorldCfg.MinPetitionSigns] = GetDefaultValue("MinPetitionSigns", 4);

		if ((int)Values[WorldCfg.MinPetitionSigns] > 4)
		{
			Log.Logger.Error("MinPetitionSigns ({0}) must be in range 0..4. Set to 4.", Values[WorldCfg.MinPetitionSigns]);
			Values[WorldCfg.MinPetitionSigns] = 4;
		}

		Values[WorldCfg.GmLoginState] = GetDefaultValue("GM.LoginState", 2);
		Values[WorldCfg.GmVisibleState] = GetDefaultValue("GM.Visible", 2);
		Values[WorldCfg.GmChat] = GetDefaultValue("GM.Chat", 2);
		Values[WorldCfg.GmWhisperingTo] = GetDefaultValue("GM.WhisperingTo", 2);
		Values[WorldCfg.GmFreezeDuration] = GetDefaultValue("GM.FreezeAuraDuration", 0);

		Values[WorldCfg.GmLevelInGmList] = GetDefaultValue("GM.InGMList.Level", (int)AccountTypes.Administrator);
		Values[WorldCfg.GmLevelInWhoList] = GetDefaultValue("GM.InWhoList.Level", (int)AccountTypes.Administrator);
		Values[WorldCfg.StartGmLevel] = GetDefaultValue("GM.StartLevel", 1);

		if ((int)Values[WorldCfg.StartGmLevel] < (int)Values[WorldCfg.StartPlayerLevel])
		{
			Log.Logger.Error("GM.StartLevel ({0}) must be in range StartPlayerLevel({1})..{2}. Set to {3}.",
							Values[WorldCfg.StartGmLevel],
							Values[WorldCfg.StartPlayerLevel],
							SharedConst.MaxLevel,
							Values[WorldCfg.StartPlayerLevel]);

			Values[WorldCfg.StartGmLevel] = Values[WorldCfg.StartPlayerLevel];
		}
		else if ((int)Values[WorldCfg.StartGmLevel] > SharedConst.MaxLevel)
		{
			Log.Logger.Error("GM.StartLevel ({0}) must be in range 1..{1}. Set to {1}.", Values[WorldCfg.StartGmLevel], SharedConst.MaxLevel);
			Values[WorldCfg.StartGmLevel] = SharedConst.MaxLevel;
		}

		Values[WorldCfg.AllowGmGroup] = GetDefaultValue("GM.AllowInvite", false);
		Values[WorldCfg.GmLowerSecurity] = GetDefaultValue("GM.LowerSecurity", false);
		Values[WorldCfg.ForceShutdownThreshold] = GetDefaultValue("GM.ForceShutdownThreshold", 30);

		Values[WorldCfg.GroupVisibility] = GetDefaultValue("Visibility.GroupMode", 1);

		Values[WorldCfg.MailDeliveryDelay] = GetDefaultValue("MailDeliveryDelay", Time.Hour);
		Values[WorldCfg.CleanOldMailTime] = GetDefaultValue("CleanOldMailTime", 4);

		if ((int)Values[WorldCfg.CleanOldMailTime] > 23)
		{
			Log.Logger.Error($"CleanOldMailTime ({Values[WorldCfg.CleanOldMailTime]}) must be an hour, between 0 and 23. Set to 4.");
			Values[WorldCfg.CleanOldMailTime] = 4;
		}

		Values[WorldCfg.UptimeUpdate] = GetDefaultValue("UpdateUptimeInterval", 10);

		if ((int)Values[WorldCfg.UptimeUpdate] <= 0)
		{
			Log.Logger.Error("UpdateUptimeInterval ({0}) must be > 0, set to default 10.", Values[WorldCfg.UptimeUpdate]);
			Values[WorldCfg.UptimeUpdate] = 10;
		}

		// log db cleanup interval
		Values[WorldCfg.LogdbClearinterval] = GetDefaultValue("LogDB.Opt.ClearInterval", 10);

		if ((int)Values[WorldCfg.LogdbClearinterval] <= 0)
		{
			Log.Logger.Error("LogDB.Opt.ClearInterval ({0}) must be > 0, set to default 10.", Values[WorldCfg.LogdbClearinterval]);
			Values[WorldCfg.LogdbClearinterval] = 10;
		}

		Values[WorldCfg.LogdbCleartime] = GetDefaultValue("LogDB.Opt.ClearTime", 1209600); // 14 days default
		Log.Logger.Information("Will clear `logs` table of entries older than {0} seconds every {1} minutes.", Values[WorldCfg.LogdbCleartime], Values[WorldCfg.LogdbClearinterval]);

		Values[WorldCfg.SkillChanceOrange] = GetDefaultValue("SkillChance.Orange", 100);
		Values[WorldCfg.SkillChanceYellow] = GetDefaultValue("SkillChance.Yellow", 75);
		Values[WorldCfg.SkillChanceGreen] = GetDefaultValue("SkillChance.Green", 25);
		Values[WorldCfg.SkillChanceGrey] = GetDefaultValue("SkillChance.Grey", 0);

		Values[WorldCfg.SkillChanceMiningSteps] = GetDefaultValue("SkillChance.MiningSteps", 75);
		Values[WorldCfg.SkillChanceSkinningSteps] = GetDefaultValue("SkillChance.SkinningSteps", 75);

		Values[WorldCfg.SkillProspecting] = GetDefaultValue("SkillChance.Prospecting", false);
		Values[WorldCfg.SkillMilling] = GetDefaultValue("SkillChance.Milling", false);

		Values[WorldCfg.SkillGainCrafting] = GetDefaultValue("SkillGain.Crafting", 1);

		Values[WorldCfg.SkillGainGathering] = GetDefaultValue("SkillGain.Gathering", 1);

		Values[WorldCfg.MaxOverspeedPings] = GetDefaultValue("MaxOverspeedPings", 2);

		if ((int)Values[WorldCfg.MaxOverspeedPings] != 0 && (int)Values[WorldCfg.MaxOverspeedPings] < 2)
		{
			Log.Logger.Error("MaxOverspeedPings ({0}) must be in range 2..infinity (or 0 to disable check). Set to 2.", Values[WorldCfg.MaxOverspeedPings]);
			Values[WorldCfg.MaxOverspeedPings] = 2;
		}

		Values[WorldCfg.Weather] = GetDefaultValue("ActivateWeather", true);

		Values[WorldCfg.DisableBreathing] = GetDefaultValue("DisableWaterBreath", (int)AccountTypes.Console);
    }

	public static uint GetUIntValue(WorldCfg confi)
	{
		return Convert.ToUInt32(Values.LookupByKey(confi));
	}

	public static int GetIntValue(WorldCfg confi)
	{
		return Convert.ToInt32(Values.LookupByKey(confi));
	}

	public static ulong GetUInt64Value(WorldCfg confi)
	{
		return Convert.ToUInt64(Values.LookupByKey(confi));
	}

	public static bool GetBoolValue(WorldCfg confi)
	{
		return Convert.ToBoolean(Values.LookupByKey(confi));
	}

	public static float GetFloatValue(WorldCfg confi)
	{
		return Convert.ToSingle(Values.LookupByKey(confi));
	}

	public static void SetValue(WorldCfg confi, object value)
	{
		Values[confi] = value;
	}
}