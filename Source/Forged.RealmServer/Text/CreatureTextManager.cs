// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Chat;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Globals;
using Forged.RealmServer.Networking;
using Forged.RealmServer.Networking.Packets;
using Forged.RealmServer.World;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using Forged.RealmServer.DataStorage;

namespace Forged.RealmServer;

public sealed class CreatureTextManager
{
    private readonly IConfiguration _configuration;
    private readonly WorldConfig _worldConfig;
    private readonly CliDB _cliDb;
    private readonly WorldDatabase _worldDatabase;
    private readonly WorldManager _worldManager;
    private readonly LanguageManager _languageManager;
    private readonly DB2Manager _db2Manager;
    readonly Dictionary<uint, MultiMap<byte, CreatureTextEntry>> _textMap = new();
	readonly Dictionary<CreatureTextId, CreatureTextLocale> _localeTextMap = new();

	public CreatureTextManager(IConfiguration configuration, WorldConfig worldConfig, CliDB cliDB,
        WorldDatabase worldDatabase, WorldManager worldManager, LanguageManager languageManager, DB2Manager db2Manager)
    {
        _configuration = configuration;
        _worldConfig = worldConfig;
        _cliDb = cliDB;
        _worldDatabase = worldDatabase;
        _worldManager = worldManager;
        _languageManager = languageManager;
        _db2Manager = db2Manager;
    }

	public void LoadCreatureTexts()
	{
		var oldMSTime = Time.MSTime;

		_textMap.Clear(); // for reload case
		//all currently used temp texts are NOT reset

		var stmt = _worldDatabase.GetPreparedStatement(WorldStatements.SEL_CREATURE_TEXT);
		var result = _worldDatabase.Query(stmt);

		if (result.IsEmpty())
		{
			Log.Logger.Information("Loaded 0 ceature texts. DB table `creature_texts` is empty.");

			return;
		}

		uint textCount = 0;
		uint creatureCount = 0;

		do
		{
			CreatureTextEntry temp = new();

			temp.creatureId = result.Read<uint>(0);
			temp.groupId = result.Read<byte>(1);
			temp.id = result.Read<byte>(2);
			temp.text = result.Read<string>(3);
			temp.type = (ChatMsg)result.Read<byte>(4);
			temp.lang = (Language)result.Read<byte>(5);
			temp.probability = result.Read<float>(6);
			temp.emote = (Emote)result.Read<uint>(7);
			temp.duration = result.Read<uint>(8);
			temp.sound = result.Read<uint>(9);
			temp.SoundPlayType = (SoundKitPlayType)result.Read<byte>(10);
			temp.BroadcastTextId = result.Read<uint>(11);
			temp.TextRange = (CreatureTextRange)result.Read<byte>(12);

			if (temp.sound != 0)
				if (!_cliDb.SoundKitStorage.ContainsKey(temp.sound))
				{
					if (_configuration.GetDefaultValue("load.autoclean", false))
						_worldDatabase.Execute($"UPDATE creature_text SET Sound = 0 WHERE CreatureID = {temp.creatureId} AND GroupID = {temp.groupId}");
					else
						Log.Logger.Error($"GossipManager: Entry {temp.creatureId}, Group {temp.groupId} in table `creature_texts` has Sound {temp.sound} but sound does not exist.");

					temp.sound = 0;
				}

			if (temp.SoundPlayType >= SoundKitPlayType.Max)
			{
				if (_configuration.GetDefaultValue("load.autoclean", false))
					_worldDatabase.Execute($"UPDATE creature_text SET SoundPlayType = 0 WHERE CreatureID = {temp.creatureId} AND GroupID = {temp.groupId}");
				else
					Log.Logger.Error($"CreatureTextMgr: Entry {temp.creatureId}, Group {temp.groupId} in table `creature_text` has PlayType {temp.SoundPlayType} but does not exist.");

				temp.SoundPlayType = SoundKitPlayType.Normal;
			}

			if (temp.lang != Language.Universal && !_languageManager.IsLanguageExist(temp.lang))
			{
				if (_configuration.GetDefaultValue("load.autoclean", false))
					_worldDatabase.Execute($"UPDATE creature_text SET Language = 0 WHERE CreatureID = {temp.creatureId} AND GroupID = {temp.groupId}");
				else
					Log.Logger.Error($"CreatureTextMgr: Entry {temp.creatureId}, Group {temp.groupId} in table `creature_texts` using Language {temp.lang} but Language does not exist.");

				temp.lang = Language.Universal;
			}

			if (temp.type >= ChatMsg.Max)
			{
				if (_configuration.GetDefaultValue("load.autoclean", false))
					_worldDatabase.Execute($"UPDATE creature_text SET Type = {ChatMsg.Say} WHERE CreatureID = {temp.creatureId} AND GroupID = {temp.groupId}");
				else
					Log.Logger.Error($"CreatureTextMgr: Entry {temp.creatureId}, Group {temp.groupId} in table `creature_texts` has Type {temp.type} but this Chat Type does not exist.");

				temp.type = ChatMsg.Say;
			}

			if (temp.emote != 0)
				if (!_cliDb.EmotesStorage.ContainsKey((uint)temp.emote))
				{
					if (_configuration.GetDefaultValue("load.autoclean", false))
						_worldDatabase.Execute($"UPDATE creature_text SET Emote = 0 WHERE CreatureID = {temp.creatureId} AND GroupID = {temp.groupId}");
					else
						Log.Logger.Error($"CreatureTextMgr: Entry {temp.creatureId}, Group {temp.groupId} in table `creature_texts` has Emote {temp.emote} but emote does not exist.");

					temp.emote = Emote.OneshotNone;
				}

			if (temp.BroadcastTextId != 0)
				if (!_cliDb.BroadcastTextStorage.ContainsKey(temp.BroadcastTextId))
				{
					if (_configuration.GetDefaultValue("load.autoclean", false))
						_worldDatabase.Execute($"UPDATE creature_text SET BroadcastTextId = 0 WHERE CreatureID = {temp.creatureId} AND GroupID = {temp.groupId}");
					else
						Log.Logger.Error($"CreatureTextMgr: Entry {temp.creatureId}, Group {temp.groupId}, Id {temp.id} in table `creature_texts` has non-existing or incompatible BroadcastTextId {temp.BroadcastTextId}.");

					temp.BroadcastTextId = 0;
				}

			if (temp.TextRange > CreatureTextRange.Personal)
			{
				if (_configuration.GetDefaultValue("load.autoclean", false))
					_worldDatabase.Execute($"UPDATE creature_text SET TextRange = 0 WHERE CreatureID = {temp.creatureId} AND GroupID = {temp.groupId}");
				else
					Log.Logger.Error($"CreatureTextMgr: Entry {temp.creatureId}, Group {temp.groupId}, Id {temp.id} in table `creature_text` has incorrect TextRange {temp.TextRange}.");

				temp.TextRange = CreatureTextRange.Normal;
			}

			if (!_textMap.ContainsKey(temp.creatureId))
			{
				_textMap[temp.creatureId] = new MultiMap<byte, CreatureTextEntry>();
				++creatureCount;
			}

			_textMap[temp.creatureId].Add(temp.groupId, temp);
			++textCount;
		} while (result.NextRow());

		Log.Logger.Information($"Loaded {textCount} creature texts for {creatureCount} creatures in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
	}

	public void LoadCreatureTextLocales()
	{
		var oldMSTime = Time.MSTime;

		_localeTextMap.Clear(); // for reload case

		var result = _worldDatabase.Query("SELECT CreatureId, GroupId, ID, Locale, Text FROM creature_text_locale");

		if (result.IsEmpty())
			return;

		do
		{
			var creatureId = result.Read<uint>(0);
			uint groupId = result.Read<byte>(1);
			uint id = result.Read<byte>(2);
			var localeName = result.Read<string>(3);
			var locale = localeName.ToEnum<Locale>();

			if (!SharedConst.IsValidLocale(locale) || locale == Locale.enUS)
				continue;

			var key = new CreatureTextId(creatureId, groupId, id);

			if (!_localeTextMap.ContainsKey(key))
				_localeTextMap[key] = new CreatureTextLocale();

			var data = _localeTextMap[key];
			GameObjectManager.AddLocaleString(result.Read<string>(4), locale, data.Text);
		} while (result.NextRow());

		Log.Logger.Information("Loaded {0} creature localized texts in {1} ms", _localeTextMap.Count, Time.GetMSTimeDiffToNow(oldMSTime));
	}

	public uint SendChat(Creature source, byte textGroup, WorldObject whisperTarget = null, ChatMsg msgType = ChatMsg.Addon, Language language = Language.Addon,
						CreatureTextRange range = CreatureTextRange.Normal, uint sound = 0, SoundKitPlayType playType = SoundKitPlayType.Normal, TeamFaction team = TeamFaction.Other, bool gmOnly = false, Player srcPlr = null)
	{
		if (source == null)
			return 0;

		var sList = _textMap.LookupByKey(source.Entry);

		if (sList == null)
		{
			Log.Logger.Error("GossipManager: Could not find Text for Creature({0}) Entry {1} in 'creature_text' table. Ignoring.", source.GetName(), source.Entry);

			return 0;
		}

		var textGroupContainer = sList.LookupByKey(textGroup);

		if (textGroupContainer.Empty())
		{
			Log.Logger.Error("GossipManager: Could not find TextGroup {0} for Creature({1}) GuidLow {2} Entry {3}. Ignoring.", textGroup, source.GetName(), source.GUID.ToString(), source.Entry);

			return 0;
		}

		List<CreatureTextEntry> tempGroup = new();
		var repeatGroup = source.GetTextRepeatGroup(textGroup);

		foreach (var entry in textGroupContainer)
			if (!repeatGroup.Contains(entry.id))
				tempGroup.Add(entry);

		if (tempGroup.Empty())
		{
			source.ClearTextRepeatGroup(textGroup);
			tempGroup = textGroupContainer;
		}

		var textEntry = tempGroup.SelectRandomElementByWeight(t => t.probability);

		var finalType = (msgType == ChatMsg.Addon) ? textEntry.type : msgType;
		var finalLang = (language == Language.Addon) ? textEntry.lang : language;
		var finalSound = textEntry.sound;
		var finalPlayType = textEntry.SoundPlayType;

		if (sound != 0)
		{
			finalSound = sound;
			finalPlayType = playType;
		}
		else
		{
			var bct = _cliDb.BroadcastTextStorage.LookupByKey(textEntry.BroadcastTextId);

			if (bct != null)
			{
				var broadcastTextSoundId = bct.SoundKitID[source.Gender == Gender.Female ? 1 : 0];

				if (broadcastTextSoundId != 0)
					finalSound = broadcastTextSoundId;
			}
		}

		if (range == CreatureTextRange.Normal)
			range = textEntry.TextRange;

		if (finalSound != 0)
			SendSound(source, finalSound, finalType, whisperTarget, range, team, gmOnly, textEntry.BroadcastTextId, finalPlayType);

		Unit finalSource = source;

		if (srcPlr)
			finalSource = srcPlr;

		if (textEntry.emote != 0)
			SendEmote(finalSource, textEntry.emote);

		if (srcPlr)
		{
			PlayerTextBuilder builder = new(source, finalSource, finalSource.Gender, finalType, textEntry.groupId, textEntry.id, finalLang, whisperTarget);
			SendChatPacket(finalSource, builder, finalType, whisperTarget, range, team, gmOnly);
		}
		else
		{
			CreatureTextBuilder builder = new(finalSource, finalSource.Gender, finalType, textEntry.groupId, textEntry.id, finalLang, whisperTarget);
			SendChatPacket(finalSource, builder, finalType, whisperTarget, range, team, gmOnly);
		}

		source.SetTextRepeatId(textGroup, textEntry.id);

		return textEntry.duration;
	}

	public float GetRangeForChatType(ChatMsg msgType)
	{
		var dist = _worldConfig.GetFloatValue(WorldCfg.ListenRangeSay);

		switch (msgType)
		{
			case ChatMsg.MonsterYell:
				dist = _worldConfig.GetFloatValue(WorldCfg.ListenRangeYell);

				break;
			case ChatMsg.MonsterEmote:
			case ChatMsg.RaidBossEmote:
				dist = _worldConfig.GetFloatValue(WorldCfg.ListenRangeTextemote);

				break;
			default:
				break;
		}

		return dist;
	}

	public void SendSound(Creature source, uint sound, ChatMsg msgType, WorldObject whisperTarget = null, CreatureTextRange range = CreatureTextRange.Normal, TeamFaction team = TeamFaction.Other, bool gmOnly = false, uint keyBroadcastTextId = 0, SoundKitPlayType playType = SoundKitPlayType.Normal)
	{
		if (sound == 0 || !source)
			return;

		if (playType == SoundKitPlayType.ObjectSound)
		{
			PlayObjectSound pkt = new();
			pkt.TargetObjectGUID = whisperTarget.GUID;
			pkt.SourceObjectGUID = source.GUID;
			pkt.SoundKitID = sound;
			pkt.Position = whisperTarget.Location;
			pkt.BroadcastTextID = (int)keyBroadcastTextId;
			SendNonChatPacket(source, pkt, msgType, whisperTarget, range, team, gmOnly);
		}
		else if (playType == SoundKitPlayType.Normal)
		{
			SendNonChatPacket(source, new PlaySound(source.GUID, sound, keyBroadcastTextId), msgType, whisperTarget, range, team, gmOnly);
		}
	}

	public bool TextExist(uint sourceEntry, byte textGroup)
	{
		if (sourceEntry == 0)
			return false;

		var textHolder = _textMap.LookupByKey(sourceEntry);

		if (textHolder == null)
		{
			Log.Logger.Debug("CreatureTextMgr.TextExist: Could not find Text for Creature (entry {0}) in 'creature_text' table.", sourceEntry);

			return false;
		}

		var textEntryList = textHolder.LookupByKey(textGroup);

		if (textEntryList.Empty())
		{
			Log.Logger.Debug("CreatureTextMgr.TextExist: Could not find TextGroup {0} for Creature (entry {1}).", textGroup, sourceEntry);

			return false;
		}

		return true;
	}

	public string GetLocalizedChatString(uint entry, Gender gender, byte textGroup, uint id, Locale locale = Locale.enUS)
	{
		var multiMap = _textMap.LookupByKey(entry);

		if (multiMap == null)
			return "";

		var creatureTextEntryList = multiMap.LookupByKey(textGroup);

		if (creatureTextEntryList.Empty())
			return "";

		CreatureTextEntry creatureTextEntry = null;

		for (var i = 0; i != creatureTextEntryList.Count; ++i)
		{
			creatureTextEntry = creatureTextEntryList[i];

			if (creatureTextEntry.id == id)
				break;
		}

		if (creatureTextEntry == null)
			return "";

		if (locale >= Locale.Total)
			locale = Locale.enUS;

		string baseText;
		var bct = _cliDb.BroadcastTextStorage.LookupByKey(creatureTextEntry.BroadcastTextId);

		if (bct != null)
			baseText = _db2Manager.GetBroadcastTextValue(bct, locale, gender);
		else
			baseText = creatureTextEntry.text;

		if (locale != Locale.enUS && bct == null)
		{
			var creatureTextLocale = _localeTextMap.LookupByKey(new CreatureTextId(entry, textGroup, id));

			if (creatureTextLocale != null)
				GameObjectManager.GetLocaleString(creatureTextLocale.Text, locale, ref baseText);
		}

		return baseText;
	}

	public void SendChatPacket(WorldObject source, MessageBuilder builder, ChatMsg msgType, WorldObject whisperTarget = null, CreatureTextRange range = CreatureTextRange.Normal, TeamFaction team = TeamFaction.Other, bool gmOnly = false)
	{
		if (source == null)
			return;

		var localizer = new CreatureTextLocalizer(builder, msgType);

		switch (msgType)
		{
			case ChatMsg.MonsterWhisper:
			case ChatMsg.RaidBossWhisper:
			{
				if (range == CreatureTextRange.Normal) //ignores team and gmOnly
				{
					if (!whisperTarget || !whisperTarget.IsTypeId(TypeId.Player))
						return;

					localizer.Invoke(whisperTarget.AsPlayer);

					return;
				}

				break;
			}
			default:
				break;
		}

        // Send to map server
		// proximity chat messages
    }

    void SendNonChatPacket(WorldObject source, ServerPacket data, ChatMsg msgType, WorldObject whisperTarget, CreatureTextRange range, TeamFaction team, bool gmOnly)
	{
		var dist = GetRangeForChatType(msgType);

		switch (msgType)
		{
			case ChatMsg.MonsterParty:
				if (!whisperTarget)
					return;

				var whisperPlayer = whisperTarget.AsPlayer;

				if (whisperPlayer)
				{
					var group = whisperPlayer.Group;

					if (group)
						group.BroadcastWorker(player => player.SendPacket(data));
				}

				return;
			case ChatMsg.MonsterWhisper:
			case ChatMsg.RaidBossWhisper:
			{
				if (range == CreatureTextRange.Normal) //ignores team and gmOnly
				{
					if (!whisperTarget || !whisperTarget.IsTypeId(TypeId.Player))
						return;

					whisperTarget.AsPlayer.SendPacket(data);

					return;
				}

				break;
			}
			default:
				break;
		}

		switch (range)
		{
			case CreatureTextRange.Area:
			{
				var areaId = source.Area;
				var players = source.Map.Players;

				foreach (var pl in players)
					if (pl.Area == areaId && (team == 0 || pl.EffectiveTeam == team) && (!gmOnly || pl.IsGameMaster))
						pl.SendPacket(data);

				return;
			}
			case CreatureTextRange.Zone:
			{
				var zoneId = source.Zone;
				var players = source.Map.Players;

				foreach (var pl in players)
					if (pl.Zone == zoneId && (team == 0 || pl.EffectiveTeam == team) && (!gmOnly || pl.IsGameMaster))
						pl.SendPacket(data);

				return;
			}
			case CreatureTextRange.Map:
			{
				var players = source.Map.Players;

				foreach (var pl in players)
					if ((team == 0 || pl.EffectiveTeam == team) && (!gmOnly || pl.IsGameMaster))
						pl.SendPacket(data);

				return;
			}
			case CreatureTextRange.World:
			{
				var smap = _worldManager.AllSessions;

				foreach (var session in smap)
				{
					var player = session.Player;

					if (player != null)
						if ((team == 0 || player.Team == team) && (!gmOnly || player.IsGameMaster))
							player.SendPacket(data);
				}

				return;
			}
			case CreatureTextRange.Personal:
				if (whisperTarget == null || !whisperTarget.IsPlayer)
					return;

				whisperTarget.AsPlayer.SendPacket(data);

				return;
			case CreatureTextRange.Normal:
			default:
				break;
		}

		source.SendMessageToSetInRange(data, dist, true);
	}

	void SendEmote(Unit source, Emote emote)
	{
		if (!source)
			return;

		source.HandleEmoteCommand(emote);
	}
}