// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Forged.RealmServer.Chat;
using Forged.RealmServer.DataStorage;
using Game.Entities;
using Forged.RealmServer.Groups;
using Game.Networking;
using Game.Networking.Packets;
using Forged.RealmServer.Scripting.Interfaces.IPlayer;

namespace Forged.RealmServer;

public partial class WorldSession
{
	[WorldPacketHandler(ClientOpcodes.ChatMessageGuild)]
	[WorldPacketHandler(ClientOpcodes.ChatMessageOfficer)]
	[WorldPacketHandler(ClientOpcodes.ChatMessageParty)]
	[WorldPacketHandler(ClientOpcodes.ChatMessageRaid)]
	[WorldPacketHandler(ClientOpcodes.ChatMessageRaidWarning)]
	[WorldPacketHandler(ClientOpcodes.ChatMessageSay)]
	[WorldPacketHandler(ClientOpcodes.ChatMessageYell)]
	[WorldPacketHandler(ClientOpcodes.ChatMessageInstanceChat)]
	void HandleChatMessage(ChatMessage packet)
	{
		ChatMsg type;

		switch (packet.GetOpcode())
		{
			case ClientOpcodes.ChatMessageSay:
				type = ChatMsg.Say;

				break;
			case ClientOpcodes.ChatMessageYell:
				type = ChatMsg.Yell;

				break;
			case ClientOpcodes.ChatMessageGuild:
				type = ChatMsg.Guild;

				break;
			case ClientOpcodes.ChatMessageOfficer:
				type = ChatMsg.Officer;

				break;
			case ClientOpcodes.ChatMessageParty:
				type = ChatMsg.Party;

				break;
			case ClientOpcodes.ChatMessageRaid:
				type = ChatMsg.Raid;

				break;
			case ClientOpcodes.ChatMessageRaidWarning:
				type = ChatMsg.RaidWarning;

				break;
			case ClientOpcodes.ChatMessageInstanceChat:
				type = ChatMsg.InstanceChat;

				break;
			default:
				Log.outError(LogFilter.Network, "HandleMessagechatOpcode : Unknown chat opcode ({0})", packet.GetOpcode());

				return;
		}

		HandleChat(type, packet.Language, packet.Text);
	}

	[WorldPacketHandler(ClientOpcodes.ChatMessageWhisper)]
	void HandleChatMessageWhisper(ChatMessageWhisper packet)
	{
		HandleChat(ChatMsg.Whisper, packet.Language, packet.Text, packet.Target);
	}

	[WorldPacketHandler(ClientOpcodes.ChatMessageChannel)]
	void HandleChatMessageChannel(ChatMessageChannel packet)
	{
		HandleChat(ChatMsg.Channel, packet.Language, packet.Text, packet.Target, packet.ChannelGUID);
	}

	[WorldPacketHandler(ClientOpcodes.ChatMessageEmote)]
	void HandleChatMessageEmote(ChatMessageEmote packet)
	{
		HandleChat(ChatMsg.Emote, Language.Universal, packet.Text);
	}

	void HandleChat(ChatMsg type, Language lang, string msg, string target = "", ObjectGuid channelGuid = default)
	{
		var sender = Player;

		if (lang == Language.Universal && type != ChatMsg.Emote)
		{
			Log.outError(LogFilter.Network, "CMSG_MESSAGECHAT: Possible hacking-attempt: {0} tried to send a message in universal language", GetPlayerInfo());
			SendNotification(CypherStrings.UnknownLanguage);

			return;
		}

		// prevent talking at unknown language (cheating)
		var languageData = Global.LanguageMgr.GetLanguageDescById(lang);

		if (languageData.Empty())
		{
			SendNotification(CypherStrings.UnknownLanguage);

			return;
		}

		if (!languageData.Any(langDesc => langDesc.SkillId == 0 || sender.HasSkill((SkillType)langDesc.SkillId)))
			// also check SPELL_AURA_COMPREHEND_LANGUAGE (client offers option to speak in that language)
			if (!sender.HasAuraTypeWithMiscvalue(AuraType.ComprehendLanguage, (int)lang))
			{
				SendNotification(CypherStrings.NotLearnedLanguage);

				return;
			}

		// send in universal language if player in .gm on mode (ignore spell effects)
		if (sender.IsGameMaster)
		{
			lang = Language.Universal;
		}
		else
		{
			// send in universal language in two side iteration allowed mode
			if (HasPermission(RBACPermissions.TwoSideInteractionChat))
				lang = Language.Universal;
			else
				switch (type)
				{
					case ChatMsg.Party:
					case ChatMsg.Raid:
					case ChatMsg.RaidWarning:
						// allow two side chat at group channel if two side group allowed
						if (WorldConfig.GetBoolValue(WorldCfg.AllowTwoSideInteractionGroup))
							lang = Language.Universal;

						break;
					case ChatMsg.Guild:
					case ChatMsg.Officer:
						// allow two side chat at guild channel if two side guild allowed
						if (WorldConfig.GetBoolValue(WorldCfg.AllowTwoSideInteractionGuild))
							lang = Language.Universal;

						break;
				}

			// but overwrite it by SPELL_AURA_MOD_LANGUAGE auras (only single case used)
			var ModLangAuras = sender.GetAuraEffectsByType(AuraType.ModLanguage);

			if (!ModLangAuras.Empty())
				lang = (Language)ModLangAuras.FirstOrDefault().MiscValue;
		}

		if (!CanSpeak)
		{
			var timeStr = Time.secsToTimeString((ulong)(MuteTime - GameTime.GetGameTime()));
			SendNotification(CypherStrings.WaitBeforeSpeaking, timeStr);

			return;
		}

		if (sender.HasAura(1852) && type != ChatMsg.Whisper)
		{
			SendNotification(Global.ObjectMgr.GetCypherString(CypherStrings.GmSilence), sender.GetName());

			return;
		}

		if (string.IsNullOrEmpty(msg))
			return;

		if (new CommandHandler(this).ParseCommands(msg))
			return;

		switch (type)
		{
			case ChatMsg.Say:
				// Prevent cheating
				if (!sender.IsAlive)
					return;

				if (sender.Level < WorldConfig.GetIntValue(WorldCfg.ChatSayLevelReq))
				{
					SendNotification(Global.ObjectMgr.GetCypherString(CypherStrings.SayReq), WorldConfig.GetIntValue(WorldCfg.ChatSayLevelReq));

					return;
				}

				sender.Say(msg, lang);

				break;
			case ChatMsg.Emote:
				// Prevent cheating
				if (!sender.IsAlive)
					return;

				if (sender.Level < WorldConfig.GetIntValue(WorldCfg.ChatEmoteLevelReq))
				{
					SendNotification(Global.ObjectMgr.GetCypherString(CypherStrings.SayReq), WorldConfig.GetIntValue(WorldCfg.ChatEmoteLevelReq));

					return;
				}

				sender.TextEmote(msg);

				break;
			case ChatMsg.Yell:
				// Prevent cheating
				if (!sender.IsAlive)
					return;

				if (sender.Level < WorldConfig.GetIntValue(WorldCfg.ChatYellLevelReq))
				{
					SendNotification(Global.ObjectMgr.GetCypherString(CypherStrings.SayReq), WorldConfig.GetIntValue(WorldCfg.ChatYellLevelReq));

					return;
				}

				sender.Yell(msg, lang);

				break;
			case ChatMsg.Whisper:
				// @todo implement cross realm whispers (someday)
				var extName = ObjectManager.ExtractExtendedPlayerName(target);

				if (!ObjectManager.NormalizePlayerName(ref extName.Name))
				{
					SendChatPlayerNotfoundNotice(target);

					break;
				}

				var receiver = Global.ObjAccessor.FindPlayerByName(extName.Name);

				if (!receiver || (lang != Language.Addon && !receiver.IsAcceptWhispers && receiver.Session.HasPermission(RBACPermissions.CanFilterWhispers) && !receiver.IsInWhisperWhiteList(sender.GUID)))
				{
					SendChatPlayerNotfoundNotice(target);

					return;
				}

				// Apply checks only if receiver is not already in whitelist and if receiver is not a GM with ".whisper on"
				if (!receiver.IsInWhisperWhiteList(sender.GUID) && !receiver.IsGameMasterAcceptingWhispers)
				{
					if (!sender.IsGameMaster && sender.Level < WorldConfig.GetIntValue(WorldCfg.ChatWhisperLevelReq))
					{
						SendNotification(Global.ObjectMgr.GetCypherString(CypherStrings.WhisperReq), WorldConfig.GetIntValue(WorldCfg.ChatWhisperLevelReq));

						return;
					}

					if (_player.EffectiveTeam != receiver.EffectiveTeam && !HasPermission(RBACPermissions.TwoSideInteractionChat) && !receiver.IsInWhisperWhiteList(sender.GUID))
					{
						SendChatPlayerNotfoundNotice(target);

						return;
					}
				}

				if (_player.HasAura(1852) && !receiver.IsGameMaster)
				{
					SendNotification(Global.ObjectMgr.GetCypherString(CypherStrings.GmSilence), _player.GetName());

					return;
				}

				if (receiver.Level < WorldConfig.GetIntValue(WorldCfg.ChatWhisperLevelReq) ||
					(HasPermission(RBACPermissions.CanFilterWhispers) && !sender.IsAcceptWhispers && !sender.IsInWhisperWhiteList(receiver.GUID)))
					sender.AddWhisperWhiteList(receiver.GUID);

				_player.Whisper(msg, lang, receiver);

				break;
			case ChatMsg.Party:
			{
				// if player is in Battleground, he cannot say to Battlegroundmembers by /p
				var group = _player.OriginalGroup;

				if (!group)
				{
					group = _player.Group;

					if (!group || group.IsBGGroup)
						return;
				}

				if (group.IsLeader(_player.GUID))
					type = ChatMsg.PartyLeader;

				Global.ScriptMgr.OnPlayerChat(_player, type, lang, msg, group);

				ChatPkt data = new();
				data.Initialize(type, lang, sender, null, msg);
				group.BroadcastPacket(data, false, group.GetMemberGroup(_player.GUID));
			}

				break;
			case ChatMsg.Guild:
				if (_player.GuildId != 0)
				{
					var guild = Global.GuildMgr.GetGuildById(_player.GuildId);

					if (guild)
					{
						Global.ScriptMgr.OnPlayerChat(_player, type, lang, msg, guild);

						guild.BroadcastToGuild(this, false, msg, lang == Language.Addon ? Language.Addon : Language.Universal);
					}
				}

				break;
			case ChatMsg.Officer:
				if (_player.GuildId != 0)
				{
					var guild = Global.GuildMgr.GetGuildById(_player.GuildId);

					if (guild)
					{
						Global.ScriptMgr.OnPlayerChat(_player, type, lang, msg, guild);

						guild.BroadcastToGuild(this, true, msg, lang == Language.Addon ? Language.Addon : Language.Universal);
					}
				}

				break;
			case ChatMsg.Raid:
			{
				var group = _player.Group;

				if (!group || !group.IsRaidGroup || group.IsBGGroup)
					return;

				if (group.IsLeader(_player.GUID))
					type = ChatMsg.RaidLeader;

				Global.ScriptMgr.OnPlayerChat(_player, type, lang, msg, group);

				ChatPkt data = new();
				data.Initialize(type, lang, sender, null, msg);
				group.BroadcastPacket(data, false);
			}

				break;
			case ChatMsg.RaidWarning:
			{
				var group = _player.Group;

				if (!group || !(group.IsRaidGroup || WorldConfig.GetBoolValue(WorldCfg.ChatPartyRaidWarnings)) || !(group.IsLeader(_player.GUID) || group.IsAssistant(_player.GUID)) || group.IsBGGroup)
					return;

				Global.ScriptMgr.OnPlayerChat(_player, type, lang, msg, group);

				ChatPkt data = new();
				//in Battleground, raid warning is sent only to players in Battleground - code is ok
				data.Initialize(ChatMsg.RaidWarning, lang, sender, null, msg);
				group.BroadcastPacket(data, false);
			}

				break;
			case ChatMsg.Channel:
				if (!HasPermission(RBACPermissions.SkipCheckChatChannelReq))
					if (_player.Level < WorldConfig.GetIntValue(WorldCfg.ChatChannelLevelReq))
					{
						SendNotification(Global.ObjectMgr.GetCypherString(CypherStrings.ChannelReq), WorldConfig.GetIntValue(WorldCfg.ChatChannelLevelReq));

						return;
					}

				var chn = !channelGuid.IsEmpty ? ChannelManager.GetChannelForPlayerByGuid(channelGuid, sender) : ChannelManager.GetChannelForPlayerByNamePart(target, sender);

				if (chn != null)
				{
					Global.ScriptMgr.OnPlayerChat(_player, type, lang, msg, chn);
					chn.Say(_player.GUID, msg, lang);
				}

				break;
			case ChatMsg.InstanceChat:
			{
				var group = _player.Group;

				if (!group)
					return;

				if (group.IsLeader(_player.GUID))
					type = ChatMsg.InstanceChatLeader;

				Global.ScriptMgr.OnPlayerChat(_player, type, lang, msg, group);

				ChatPkt packet = new();
				packet.Initialize(type, lang, sender, null, msg);
				group.BroadcastPacket(packet, false);

				break;
			}
			default:
				Log.outError(LogFilter.ChatSystem, "CHAT: unknown message type {0}, lang: {1}", type, lang);

				break;
		}
	}

	[WorldPacketHandler(ClientOpcodes.ChatAddonMessage)]
	void HandleChatAddonMessage(ChatAddonMessage chatAddonMessage)
	{
		HandleChatAddon(chatAddonMessage.Params.Type, chatAddonMessage.Params.Prefix, chatAddonMessage.Params.Text, chatAddonMessage.Params.IsLogged);
	}

	[WorldPacketHandler(ClientOpcodes.ChatAddonMessageTargeted)]
	void HandleChatAddonMessageTargeted(ChatAddonMessageTargeted chatAddonMessageTargeted)
	{
		HandleChatAddon(chatAddonMessageTargeted.Params.Type, chatAddonMessageTargeted.Params.Prefix, chatAddonMessageTargeted.Params.Text, chatAddonMessageTargeted.Params.IsLogged, chatAddonMessageTargeted.Target, chatAddonMessageTargeted.ChannelGUID);
	}

	void HandleChatAddon(ChatMsg type, string prefix, string text, bool isLogged, string target = "", ObjectGuid? channelGuid = null)
	{
		var sender = Player;

		if (string.IsNullOrEmpty(prefix) || prefix.Length > 16)
			return;

		// Disabled addon channel?
		if (!WorldConfig.GetBoolValue(WorldCfg.AddonChannel))
			return;

		if (prefix == AddonChannelCommandHandler.PREFIX && new AddonChannelCommandHandler(this).ParseCommands(text))
			return;

		switch (type)
		{
			case ChatMsg.Guild:
			case ChatMsg.Officer:
				if (sender.GuildId != 0)
				{
					var guild = Global.GuildMgr.GetGuildById(sender.GuildId);

					if (guild)
						guild.BroadcastAddonToGuild(this, type == ChatMsg.Officer, text, prefix, isLogged);
				}

				break;
			case ChatMsg.Whisper:
				// @todo implement cross realm whispers (someday)
				var extName = ObjectManager.ExtractExtendedPlayerName(target);

				if (!ObjectManager.NormalizePlayerName(ref extName.Name))
					break;

				var receiver = Global.ObjAccessor.FindPlayerByName(extName.Name);

				if (!receiver)
					break;

				sender.WhisperAddon(text, prefix, isLogged, receiver);

				break;
			// Messages sent to "RAID" while in a party will get delivered to "PARTY"
			case ChatMsg.Party:
			case ChatMsg.Raid:
			case ChatMsg.InstanceChat:
			{
				PlayerGroup group = null;
				var subGroup = -1;

				if (type != ChatMsg.InstanceChat)
					group = sender.OriginalGroup;

				if (!group)
				{
					group = sender.Group;

					if (!group)
						break;

					if (type == ChatMsg.Party)
						subGroup = sender.SubGroup;
				}

				ChatPkt data = new();
				data.Initialize(type, isLogged ? Language.AddonLogged : Language.Addon, sender, null, text, 0, "", Locale.enUS, prefix);
				group.BroadcastAddonMessagePacket(data, prefix, true, subGroup, sender.GUID);

				break;
			}
			case ChatMsg.Channel:
				var chn = channelGuid.HasValue ? ChannelManager.GetChannelForPlayerByGuid(channelGuid.Value, sender) : ChannelManager.GetChannelForPlayerByNamePart(target, sender);

				if (chn != null)
					chn.AddonSay(sender.GUID, prefix, text, isLogged);

				break;

			default:
				Log.outError(LogFilter.Server, "HandleAddonMessagechat: unknown addon message type {0}", type);

				break;
		}
	}

	[WorldPacketHandler(ClientOpcodes.ChatMessageAfk)]
	void HandleChatMessageAFK(ChatMessageAFK packet)
	{
		var sender = Player;

		if (sender.IsInCombat)
			return;

		if (sender.HasAura(1852))
		{
			SendNotification(CypherStrings.GmSilence, sender.GetName());

			return;
		}

		if (sender.IsAFK) // Already AFK
		{
			if (string.IsNullOrEmpty(packet.Text))
				sender.ToggleAFK(); // Remove AFK
			else
				sender.AutoReplyMsg = packet.Text; // Update message
		}
		else // New AFK mode
		{
			sender.AutoReplyMsg = string.IsNullOrEmpty(packet.Text) ? Global.ObjectMgr.GetCypherString(CypherStrings.PlayerAfkDefault) : packet.Text;

			if (sender.IsDND)
				sender.ToggleDND();

			sender.ToggleAFK();
		}

		var guild = sender.Guild;

		if (guild != null)
			guild.SendEventAwayChanged(sender.GUID, sender.IsAFK, sender.IsDND);

		Global.ScriptMgr.OnPlayerChat(sender, ChatMsg.Afk, Language.Universal, packet.Text);
	}

	[WorldPacketHandler(ClientOpcodes.ChatMessageDnd)]
	void HandleChatMessageDND(ChatMessageDND packet)
	{
		var sender = Player;

		if (sender.IsInCombat)
			return;

		if (sender.HasAura(1852))
		{
			SendNotification(CypherStrings.GmSilence, sender.GetName());

			return;
		}

		if (sender.IsDND) // Already DND
		{
			if (string.IsNullOrEmpty(packet.Text))
				sender.ToggleDND(); // Remove DND
			else
				sender.AutoReplyMsg = packet.Text; // Update message
		}
		else // New DND mode
		{
			sender.AutoReplyMsg = string.IsNullOrEmpty(packet.Text) ? Global.ObjectMgr.GetCypherString(CypherStrings.PlayerDndDefault) : packet.Text;

			if (sender.IsAFK)
				sender.ToggleAFK();

			sender.ToggleDND();
		}

		var guild = sender.Guild;

		if (guild != null)
			guild.SendEventAwayChanged(sender.GUID, sender.IsAFK, sender.IsDND);

		Global.ScriptMgr.OnPlayerChat(sender, ChatMsg.Dnd, Language.Universal, packet.Text);
	}

	[WorldPacketHandler(ClientOpcodes.Emote, Processing = PacketProcessing.Inplace)]
	void HandleEmote(EmoteClient packet)
	{
		if (!_player.IsAlive || _player.HasUnitState(UnitState.Died))
			return;

		Global.ScriptMgr.ForEach<IPlayerOnClearEmote>(p => p.OnClearEmote(_player));
		_player.EmoteState = Emote.OneshotNone;
	}

	[WorldPacketHandler(ClientOpcodes.ChatReportIgnored)]
	void HandleChatIgnoredOpcode(ChatReportIgnored packet)
	{
		var player = Global.ObjAccessor.FindPlayer(packet.IgnoredGUID);

		if (!player || player.Session == null)
			return;

		ChatPkt data = new();
		data.Initialize(ChatMsg.Ignored, Language.Universal, _player, _player, _player.GetName());
		player.SendPacket(data);
	}

	void SendChatPlayerNotfoundNotice(string name)
	{
		SendPacket(new ChatPlayerNotfound(name));
	}

	void SendPlayerAmbiguousNotice(string name)
	{
		SendPacket(new ChatPlayerAmbiguous(name));
	}

	void SendChatRestricted(ChatRestrictionType restriction)
	{
		SendPacket(new ChatRestricted(restriction));
	}
}