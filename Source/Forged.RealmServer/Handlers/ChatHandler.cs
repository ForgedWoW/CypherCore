// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Forged.RealmServer.Chat;
using Forged.RealmServer.DataStorage;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Groups;
using Forged.RealmServer.Scripting.Interfaces.IPlayer;
using Forged.RealmServer.Networking;
using Game.Common.Handlers;
using Forged.RealmServer.Networking.Packets;
using Serilog;
using Forged.RealmServer.Scripting;
using Forged.RealmServer.Globals;

namespace Forged.RealmServer;

public class ChatHandler : IWorldSessionHandler
{
    private readonly WorldSession _session;
    private readonly WorldConfig _worldConfig;
    private readonly GameTime _gameTime;
    private readonly LanguageManager _languageManager;
    private readonly GameObjectManager _objectManager;
    private readonly ObjectAccessor _objectAccessor;
    private readonly GuildManager _guildManager;
    private readonly ScriptManager _scriptManager;

    public ChatHandler(WorldSession session, WorldConfig worldConfig, GameTime gameTime, LanguageManager languageManager,
		GameObjectManager objectManager, ObjectAccessor objectAccessor, GuildManager guildManager, ScriptManager scriptManager)
    {
        _session = session;
        _worldConfig = worldConfig;
        _gameTime = gameTime;
        _languageManager = languageManager;
        _objectManager = objectManager;
        _objectAccessor = objectAccessor;
        _guildManager = guildManager;
        _scriptManager = scriptManager;
    }

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
				Log.Logger.Error("HandleMessagechatOpcode : Unknown chat opcode ({0})", packet.GetOpcode());

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
		var sender = _session.Player;

		if (lang == Language.Universal && type != ChatMsg.Emote)
		{
			Log.Logger.Error("CMSG_MESSAGECHAT: Possible hacking-attempt: {0} tried to send a message in universal language", _session.GetPlayerInfo());
            _session.SendNotification(CypherStrings.UnknownLanguage);

			return;
		}

		// prevent talking at unknown language (cheating)
		var languageData = _languageManager.GetLanguageDescById(lang);

		if (languageData.Empty())
		{
            _session.SendNotification(CypherStrings.UnknownLanguage);

			return;
		}

		if (!languageData.Any(langDesc => langDesc.SkillId == 0 || sender.HasSkill((SkillType)langDesc.SkillId)))
			// also check SPELL_AURA_COMPREHEND_LANGUAGE (client offers option to speak in that language)
			if (!sender.HasAuraTypeWithMiscvalue(AuraType.ComprehendLanguage, (int)lang))
			{
                _session.SendNotification(CypherStrings.NotLearnedLanguage);

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
			if (_session.HasPermission(RBACPermissions.TwoSideInteractionChat))
				lang = Language.Universal;
			else
				switch (type)
				{
					case ChatMsg.Party:
					case ChatMsg.Raid:
					case ChatMsg.RaidWarning:
						// allow two side chat at group channel if two side group allowed
						if (_worldConfig.GetBoolValue(WorldCfg.AllowTwoSideInteractionGroup))
							lang = Language.Universal;

						break;
					case ChatMsg.Guild:
					case ChatMsg.Officer:
						// allow two side chat at guild channel if two side guild allowed
						if (_worldConfig.GetBoolValue(WorldCfg.AllowTwoSideInteractionGuild))
							lang = Language.Universal;

						break;
				}

			// but overwrite it by SPELL_AURA_MOD_LANGUAGE auras (only single case used)
			var ModLangAuras = sender.GetAuraEffectsByType(AuraType.ModLanguage);

			if (!ModLangAuras.Empty())
				lang = (Language)ModLangAuras.FirstOrDefault().MiscValue;
		}

		if (!_session.CanSpeak)
		{
			var timeStr = Time.secsToTimeString((ulong)(_session.MuteTime - _gameTime.CurrentGameTime));
            _session.SendNotification(CypherStrings.WaitBeforeSpeaking, timeStr);

			return;
		}

		if (sender.HasAura(1852) && type != ChatMsg.Whisper)
		{
            _session.SendNotification(_objectManager.GetCypherString(CypherStrings.GmSilence), sender.GetName());

			return;
		}

		if (string.IsNullOrEmpty(msg))
			return;

		if (new CommandHandler(_session).ParseCommands(msg))
			return;

		switch (type)
		{
			case ChatMsg.Say:
				// Prevent cheating
				if (!sender.IsAlive)
					return;

				if (sender.Level < _worldConfig.GetIntValue(WorldCfg.ChatSayLevelReq))
				{
                    _session.SendNotification(_objectManager.GetCypherString(CypherStrings.SayReq), _worldConfig.GetIntValue(WorldCfg.ChatSayLevelReq));

					return;
				}

				sender.Say(msg, lang);

				break;
			case ChatMsg.Emote:
				// Prevent cheating
				if (!sender.IsAlive)
					return;

				if (sender.Level < _worldConfig.GetIntValue(WorldCfg.ChatEmoteLevelReq))
				{
                    _session.SendNotification(_objectManager.GetCypherString(CypherStrings.SayReq), _worldConfig.GetIntValue(WorldCfg.ChatEmoteLevelReq));

					return;
				}

				sender.TextEmote(msg);

				break;
			case ChatMsg.Yell:
				// Prevent cheating
				if (!sender.IsAlive)
					return;

				if (sender.Level < _worldConfig.GetIntValue(WorldCfg.ChatYellLevelReq))
				{
                    _session.SendNotification(_objectManager.GetCypherString(CypherStrings.SayReq), _worldConfig.GetIntValue(WorldCfg.ChatYellLevelReq));

					return;
				}

				sender.Yell(msg, lang);

				break;
			case ChatMsg.Whisper:
				// @todo implement cross realm whispers (someday)
				var extName = GameObjectManager.ExtractExtendedPlayerName(target);

				if (!GameObjectManager.NormalizePlayerName(ref extName.Name))
				{
					SendChatPlayerNotfoundNotice(target);

					break;
				}

				var receiver = _objectAccessor.FindPlayerByName(extName.Name);

				if (!receiver || (lang != Language.Addon && !receiver.IsAcceptWhispers && receiver.Session.HasPermission(RBACPermissions.CanFilterWhispers) && !receiver.IsInWhisperWhiteList(sender.GUID)))
				{
					SendChatPlayerNotfoundNotice(target);

					return;
				}

				// Apply checks only if receiver is not already in whitelist and if receiver is not a GM with ".whisper on"
				if (!receiver.IsInWhisperWhiteList(sender.GUID) && !receiver.IsGameMasterAcceptingWhispers)
				{
					if (!sender.IsGameMaster && sender.Level < _worldConfig.GetIntValue(WorldCfg.ChatWhisperLevelReq))
					{
                        _session.SendNotification(_objectManager.GetCypherString(CypherStrings.WhisperReq), _worldConfig.GetIntValue(WorldCfg.ChatWhisperLevelReq));

						return;
					}

					if (_session.Player.EffectiveTeam != receiver.EffectiveTeam && !_session.HasPermission(RBACPermissions.TwoSideInteractionChat) && !receiver.IsInWhisperWhiteList(sender.GUID))
					{
						SendChatPlayerNotfoundNotice(target);

						return;
					}
				}

				if (_session.Player.HasAura(1852) && !receiver.IsGameMaster)
				{
                    _session.SendNotification(_objectManager.GetCypherString(CypherStrings.GmSilence), _session.Player.GetName());

					return;
				}

				if (receiver.Level < _worldConfig.GetIntValue(WorldCfg.ChatWhisperLevelReq) ||
					(_session.HasPermission(RBACPermissions.CanFilterWhispers) && !sender.IsAcceptWhispers && !sender.IsInWhisperWhiteList(receiver.GUID)))
					sender.AddWhisperWhiteList(receiver.GUID);

				_session.Player.Whisper(msg, lang, receiver);

				break;
			case ChatMsg.Party:
			{
				// if player is in Battleground, he cannot say to Battlegroundmembers by /p
				var group = _session.Player.OriginalGroup;

				if (!group)
				{
					group = _session.Player.Group;

					if (!group || group.IsBGGroup)
						return;
				}

				if (group.IsLeader(_session.Player.GUID))
					type = ChatMsg.PartyLeader;

				_scriptManager.OnPlayerChat(_session.Player, type, lang, msg, group);

				ChatPkt data = new();
				data.Initialize(type, lang, sender, null, msg);
				group.BroadcastPacket(data, false, group.GetMemberGroup(_session.Player.GUID));
			}

				break;
			case ChatMsg.Guild:
				if (_session.Player.GuildId != 0)
				{
					var guild = _guildManager.GetGuildById(_session.Player.GuildId);

					if (guild)
					{
						_scriptManager.OnPlayerChat(_session.Player, type, lang, msg, guild);

						guild.BroadcastToGuild(_session, false, msg, lang == Language.Addon ? Language.Addon : Language.Universal);
					}
				}

				break;
			case ChatMsg.Officer:
				if (_session.Player.GuildId != 0)
				{
					var guild = _guildManager.GetGuildById(_session.Player.GuildId);

					if (guild)
					{
						_scriptManager.OnPlayerChat(_session.Player, type, lang, msg, guild);

						guild.BroadcastToGuild(_session, true, msg, lang == Language.Addon ? Language.Addon : Language.Universal);
					}
				}

				break;
			case ChatMsg.Raid:
			{
				var group = _session.Player.Group;

				if (!group || !group.IsRaidGroup || group.IsBGGroup)
					return;

				if (group.IsLeader(_session.Player.GUID))
					type = ChatMsg.RaidLeader;

				_scriptManager.OnPlayerChat(_session.Player, type, lang, msg, group);

				ChatPkt data = new();
				data.Initialize(type, lang, sender, null, msg);
				group.BroadcastPacket(data, false);
			}
				
				break;
			case ChatMsg.RaidWarning:
			{
				var group = _session.Player.Group;

				if (!group || !(group.IsRaidGroup || _worldConfig.GetBoolValue(WorldCfg.ChatPartyRaidWarnings)) || !(group.IsLeader(_session.Player.GUID) || group.IsAssistant(_session.Player.GUID)) || group.IsBGGroup)
					return;

				_scriptManager.OnPlayerChat(_session.Player, type, lang, msg, group);

				ChatPkt data = new();
				//in Battleground, raid warning is sent only to players in Battleground - code is ok
				data.Initialize(ChatMsg.RaidWarning, lang, sender, null, msg);
				group.BroadcastPacket(data, false);
			}

				break;
			case ChatMsg.Channel:
				if (!_session.HasPermission(RBACPermissions.SkipCheckChatChannelReq))
					if (_session.Player.Level < _worldConfig.GetIntValue(WorldCfg.ChatChannelLevelReq))
					{
                        _session.SendNotification(_objectManager.GetCypherString(CypherStrings.ChannelReq), _worldConfig.GetIntValue(WorldCfg.ChatChannelLevelReq));

						return;
					}

				var chn = !channelGuid.IsEmpty ? ChannelManager.GetChannelForPlayerByGuid(channelGuid, sender) : ChannelManager.GetChannelForPlayerByNamePart(target, sender);

				if (chn != null)
				{
					_scriptManager.OnPlayerChat(_session.Player, type, lang, msg, chn);
					chn.Say(_session.Player.GUID, msg, lang);
				}

				break;
			case ChatMsg.InstanceChat:
			{
				var group = _session.Player.Group;

				if (!group)
					return;

				if (group.IsLeader(_session.Player.GUID))
					type = ChatMsg.InstanceChatLeader;

				_scriptManager.OnPlayerChat(_session.Player, type, lang, msg, group);

				ChatPkt packet = new();
				packet.Initialize(type, lang, sender, null, msg);
				group.BroadcastPacket(packet, false);

				break;
			}
			default:
				Log.Logger.Error("CHAT: unknown message type {0}, lang: {1}", type, lang);

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
		var sender = _session.Player;

		if (string.IsNullOrEmpty(prefix) || prefix.Length > 16)
			return;

		// Disabled addon channel?
		if (!_worldConfig.GetBoolValue(WorldCfg.AddonChannel))
			return;

		if (prefix == AddonChannelCommandHandler.PREFIX && new AddonChannelCommandHandler(_session).ParseCommands(text))
			return;

		switch (type)
		{
			case ChatMsg.Guild:
			case ChatMsg.Officer:
				if (sender.GuildId != 0)
				{
					var guild = _guildManager.GetGuildById(sender.GuildId);

					if (guild)
						guild.BroadcastAddonToGuild(_session, type == ChatMsg.Officer, text, prefix, isLogged);
				}

				break;
			case ChatMsg.Whisper:
				// @todo implement cross realm whispers (someday)
				var extName = GameObjectManager.ExtractExtendedPlayerName(target);

				if (!GameObjectManager.NormalizePlayerName(ref extName.Name))
					break;

				var receiver = _objectAccessor.FindPlayerByName(extName.Name);

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
				Log.Logger.Error("HandleAddonMessagechat: unknown addon message type {0}", type);

				break;
		}
	}

	[WorldPacketHandler(ClientOpcodes.ChatMessageAfk)]
	void HandleChatMessageAFK(ChatMessageAFK packet)
	{
		var sender = _session.Player;

		if (sender.IsInCombat)
			return;

		if (sender.HasAura(1852))
		{
            _session.SendNotification(CypherStrings.GmSilence, sender.GetName());

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
			sender.AutoReplyMsg = string.IsNullOrEmpty(packet.Text) ? _objectManager.GetCypherString(CypherStrings.PlayerAfkDefault) : packet.Text;

			if (sender.IsDND)
				sender.ToggleDND();

			sender.ToggleAFK();
		}

		var guild = sender.Guild;

		if (guild != null)
			guild.SendEventAwayChanged(sender.GUID, sender.IsAFK, sender.IsDND);

		_scriptManager.OnPlayerChat(sender, ChatMsg.Afk, Language.Universal, packet.Text);
	}

	[WorldPacketHandler(ClientOpcodes.ChatMessageDnd)]
	void HandleChatMessageDND(ChatMessageDND packet)
	{
		var sender = _session.Player;

		if (sender.IsInCombat)
			return;

		if (sender.HasAura(1852))
		{
            _session.SendNotification(CypherStrings.GmSilence, sender.GetName());

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
			sender.AutoReplyMsg = string.IsNullOrEmpty(packet.Text) ? _objectManager.GetCypherString(CypherStrings.PlayerDndDefault) : packet.Text;

			if (sender.IsAFK)
				sender.ToggleAFK();

			sender.ToggleDND();
		}

		var guild = sender.Guild;

		if (guild != null)
			guild.SendEventAwayChanged(sender.GUID, sender.IsAFK, sender.IsDND);

		_scriptManager.OnPlayerChat(sender, ChatMsg.Dnd, Language.Universal, packet.Text);
	}

	[WorldPacketHandler(ClientOpcodes.Emote, Processing = PacketProcessing.Inplace)]
	void HandleEmote(EmoteClient packet)
	{
		if (!_session.Player.IsAlive || _session.Player.HasUnitState(UnitState.Died))
			return;

		_scriptManager.ForEach<IPlayerOnClearEmote>(p => p.OnClearEmote(_session.Player));
		_session.Player.EmoteState = Emote.OneshotNone;
	}

	[WorldPacketHandler(ClientOpcodes.ChatReportIgnored)]
	void HandleChatIgnoredOpcode(ChatReportIgnored packet)
	{
		var player = _objectAccessor.FindPlayer(packet.IgnoredGUID);

		if (!player || player.Session == null)
			return;

		ChatPkt data = new();
		data.Initialize(ChatMsg.Ignored, Language.Universal, _session.Player, _session.Player, _session.Player.GetName());
		player.SendPacket(data);
	}

	void SendChatPlayerNotfoundNotice(string name)
	{
        _session.SendPacket(new ChatPlayerNotfound(name));
	}

	void SendPlayerAmbiguousNotice(string name)
	{
        _session.SendPacket(new ChatPlayerAmbiguous(name));
	}

	void SendChatRestricted(ChatRestrictionType restriction)
	{
        _session.SendPacket(new ChatRestricted(restriction));
	}
}