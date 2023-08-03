// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Chat;
using Forged.MapServer.Chat.Channels;
using Forged.MapServer.Chrono;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Globals;
using Forged.MapServer.Groups;
using Forged.MapServer.Guilds;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Chat;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IPlayer;
using Forged.MapServer.Server;
using Framework.Constants;
using Game.Common;
using Game.Common.Handlers;
using Microsoft.Extensions.Configuration;
using Serilog;

// ReSharper disable UnusedMember.Local

namespace Forged.MapServer.OpCodeHandlers;

public class ChatHandler : IWorldSessionHandler
{
    private readonly ChannelManager _channelManager;
    private readonly ClassFactory _classFactory;
    private readonly CliDB _cliDb;
    private readonly IConfiguration _config;
    private readonly GameObjectManager _gameObjectManager;
    private readonly GuildManager _guildManager;
    private readonly LanguageManager _languageManager;
    private readonly ObjectAccessor _objectAccessor;
    private readonly ScriptManager _scriptManager;
    private readonly WorldSession _session;

    public ChatHandler(WorldSession session, IConfiguration config, LanguageManager languageManager,
        GameObjectManager gameObjectManager, ObjectAccessor objectAccessor, ScriptManager scriptManager,
        ClassFactory classFactory, GuildManager guildManager, ChannelManager channelManager,
        CliDB cliDb)
    {
        _session = session;
        _config = config;
        _languageManager = languageManager;
        _gameObjectManager = gameObjectManager;
        _objectAccessor = objectAccessor;
        _scriptManager = scriptManager;
        _classFactory = classFactory;
        _guildManager = guildManager;
        _channelManager = channelManager;
        _cliDb = cliDb;
    }

    private void HandleChat(ChatMsg type, Language lang, string msg, string target = "", ObjectGuid channelGuid = default)
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
                        if (_config.GetValue("AllowTwoSide.Interaction.Group", false))
                            lang = Language.Universal;

                        break;

                    case ChatMsg.Guild:
                    case ChatMsg.Officer:
                        // allow two side chat at guild channel if two side guild allowed
                        if (_config.GetValue("AllowTwoSide.Interaction.Guild", false))
                            lang = Language.Universal;

                        break;
                }

            // but overwrite it by SPELL_AURA_MOD_LANGUAGE auras (only single case used)
            var modLangAuras = sender.GetAuraEffectsByType(AuraType.ModLanguage);

            if (!modLangAuras.Empty())
                lang = (Language)modLangAuras.FirstOrDefault()!.MiscValue;
        }

        if (!_session.CanSpeak)
        {
            var timeStr = Time.SecsToTimeString((ulong)(_session.MuteTime - GameTime.CurrentTime));
            _session.SendNotification(CypherStrings.WaitBeforeSpeaking, timeStr);

            return;
        }

        if (sender.HasAura(1852) && type != ChatMsg.Whisper)
        {
            _session.SendNotification(_gameObjectManager.CypherStringCache.GetCypherString(CypherStrings.GmSilence), sender.GetName());

            return;
        }

        if (string.IsNullOrEmpty(msg))
            return;

        if (new CommandHandler(_classFactory, _session).ParseCommands(msg))
            return;

        switch (type)
        {
            case ChatMsg.Say:
                // Prevent cheating
                if (!sender.IsAlive)
                    return;

                if (sender.Level < _config.GetValue("ChatLevelReq.Say", 1))
                {
                    _session.SendNotification(_gameObjectManager.CypherStringCache.GetCypherString(CypherStrings.SayReq), _config.GetValue("ChatLevelReq.Say", 1));

                    return;
                }

                sender.Say(msg, lang);

                break;

            case ChatMsg.Emote:
                // Prevent cheating
                if (!sender.IsAlive)
                    return;

                if (sender.Level < _config.GetValue("ChatLevelReq.Emote", 1))
                {
                    _session.SendNotification(_gameObjectManager.CypherStringCache.GetCypherString(CypherStrings.SayReq), _config.GetValue("ChatLevelReq.Emote", 1));

                    return;
                }

                sender.TextEmote(msg);

                break;

            case ChatMsg.Yell:
                // Prevent cheating
                if (!sender.IsAlive)
                    return;

                if (sender.Level < _config.GetValue("ChatLevelReq.Yell", 1))
                {
                    _session.SendNotification(_gameObjectManager.CypherStringCache.GetCypherString(CypherStrings.SayReq), _config.GetValue("ChatLevelReq.Yell", 1));

                    return;
                }

                sender.Yell(msg, lang);

                break;

            case ChatMsg.Whisper:
                // @todo implement cross realm whispers (someday)
                var extName = _gameObjectManager.ExtractExtendedPlayerName(target);

                if (!_gameObjectManager.NormalizePlayerName(ref extName.Name))
                {
                    SendChatPlayerNotfoundNotice(target);

                    break;
                }

                var receiver = _objectAccessor.FindPlayerByName(extName.Name);

                if (receiver == null || (lang != Language.Addon && !receiver.IsAcceptWhispers && receiver.Session.HasPermission(RBACPermissions.CanFilterWhispers) && !receiver.IsInWhisperWhiteList(sender.GUID)))
                {
                    SendChatPlayerNotfoundNotice(target);

                    return;
                }

                // Apply checks only if receiver is not already in whitelist and if receiver is not a GM with ".whisper on"
                if (!receiver.IsInWhisperWhiteList(sender.GUID) && !receiver.IsGameMasterAcceptingWhispers)
                {
                    if (!sender.IsGameMaster && sender.Level < _config.GetValue("ChatLevelReq.Whisper", 1))
                    {
                        _session.SendNotification(_gameObjectManager.CypherStringCache.GetCypherString(CypherStrings.WhisperReq), _config.GetValue("ChatLevelReq.Whisper", 1));

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
                    _session.SendNotification(_gameObjectManager.CypherStringCache.GetCypherString(CypherStrings.GmSilence), _session.Player.GetName());

                    return;
                }

                if (receiver.Level < _config.GetValue("ChatLevelReq.Whisper", 1) ||
                    (_session.HasPermission(RBACPermissions.CanFilterWhispers) && !sender.IsAcceptWhispers && !sender.IsInWhisperWhiteList(receiver.GUID)))
                    sender.AddWhisperWhiteList(receiver.GUID);

                _session.Player.Whisper(msg, lang, receiver);

                break;

            case ChatMsg.Party:
            {
                // if player is in Battleground, he cannot say to Battlegroundmembers by /p
                var group = _session.Player.OriginalGroup;

                if (group == null)
                {
                    group = _session.Player.Group;

                    if (group == null || group.IsBGGroup)
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

                    if (guild != null)
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

                    if (guild != null)
                    {
                        _scriptManager.OnPlayerChat(_session.Player, type, lang, msg, guild);

                        guild.BroadcastToGuild(_session, true, msg, lang == Language.Addon ? Language.Addon : Language.Universal);
                    }
                }

                break;

            case ChatMsg.Raid:
            {
                var group = _session.Player.Group;

                if (group == null || !group.IsRaidGroup || group.IsBGGroup)
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

                if (group == null || !(group.IsRaidGroup || _config.GetValue("PartyRaidWarnings", false)) || !(group.IsLeader(_session.Player.GUID) || group.IsAssistant(_session.Player.GUID)) || group.IsBGGroup)
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
                    if (_session.Player.Level < _config.GetValue("ChatLevelReq.Channel", 1))
                    {
                        _session.SendNotification(_gameObjectManager.CypherStringCache.GetCypherString(CypherStrings.ChannelReq), _config.GetValue("ChatLevelReq.Channel", 1));

                        return;
                    }

                var chn = !channelGuid.IsEmpty ? _channelManager.GetChannelForPlayerByGuid(channelGuid, sender) : _channelManager.GetChannelForPlayerByNamePart(target, sender);

                if (chn != null)
                {
                    _scriptManager.OnPlayerChat(_session.Player, type, lang, msg, chn);
                    chn.Say(_session.Player.GUID, msg, lang);
                }

                break;

            case ChatMsg.InstanceChat:
            {
                var group = _session.Player.Group;

                if (group == null)
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

    private void HandleChatAddon(ChatMsg type, string prefix, string text, bool isLogged, string target = "", ObjectGuid? channelGuid = null)
    {
        var sender = _session.Player;

        if (string.IsNullOrEmpty(prefix) || prefix.Length > 16)
            return;

        // Disabled addon channel?
        if (!_config.GetValue("AddonChannel", true))
            return;

        if (prefix == AddonChannelCommandHandler.PREFIX && new AddonChannelCommandHandler(_classFactory, _session).ParseCommands(text))
            return;

        switch (type)
        {
            case ChatMsg.Guild:
            case ChatMsg.Officer:
                if (sender.GuildId != 0)
                {
                    var guild = _guildManager.GetGuildById(sender.GuildId);

                    if (guild != null)
                        guild.BroadcastAddonToGuild(_session, type == ChatMsg.Officer, text, prefix, isLogged);
                }

                break;

            case ChatMsg.Whisper:
                // @todo implement cross realm whispers (someday)
                var extName = _gameObjectManager.ExtractExtendedPlayerName(target);

                if (!_gameObjectManager.NormalizePlayerName(ref extName.Name))
                    break;

                var receiver = _objectAccessor.FindPlayerByName(extName.Name);

                if (receiver == null)
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

                if (group == null)
                {
                    group = sender.Group;

                    if (group == null)
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
                var chn = channelGuid.HasValue ? _channelManager.GetChannelForPlayerByGuid(channelGuid.Value, sender) : _channelManager.GetChannelForPlayerByNamePart(target, sender);

                if (chn != null)
                    chn.AddonSay(sender.GUID, prefix, text, isLogged);

                break;

            default:
                Log.Logger.Error("HandleAddonMessagechat: unknown addon message type {0}", type);

                break;
        }
    }

    [WorldPacketHandler(ClientOpcodes.ChatAddonMessage)]
    private void HandleChatAddonMessage(ChatAddonMessage chatAddonMessage)
    {
        HandleChatAddon(chatAddonMessage.Params.Type, chatAddonMessage.Params.Prefix, chatAddonMessage.Params.Text, chatAddonMessage.Params.IsLogged);
    }

    [WorldPacketHandler(ClientOpcodes.ChatAddonMessageTargeted)]
    private void HandleChatAddonMessageTargeted(ChatAddonMessageTargeted chatAddonMessageTargeted)
    {
        HandleChatAddon(chatAddonMessageTargeted.Params.Type, chatAddonMessageTargeted.Params.Prefix, chatAddonMessageTargeted.Params.Text, chatAddonMessageTargeted.Params.IsLogged, chatAddonMessageTargeted.Target, chatAddonMessageTargeted.ChannelGUID);
    }

    [WorldPacketHandler(ClientOpcodes.ChatReportIgnored)]
    private void HandleChatIgnoredOpcode(ChatReportIgnored packet)
    {
        var player = _objectAccessor.FindPlayer(packet.IgnoredGUID);

        if (player == null || player.Session == null)
            return;

        ChatPkt data = new();
        data.Initialize(ChatMsg.Ignored, Language.Universal, _session.Player, _session.Player, _session.Player.GetName());
        player.SendPacket(data);
    }

    [WorldPacketHandler(ClientOpcodes.ChatMessageGuild)]
    [WorldPacketHandler(ClientOpcodes.ChatMessageOfficer)]
    [WorldPacketHandler(ClientOpcodes.ChatMessageParty)]
    [WorldPacketHandler(ClientOpcodes.ChatMessageRaid)]
    [WorldPacketHandler(ClientOpcodes.ChatMessageRaidWarning)]
    [WorldPacketHandler(ClientOpcodes.ChatMessageSay)]
    [WorldPacketHandler(ClientOpcodes.ChatMessageYell)]
    [WorldPacketHandler(ClientOpcodes.ChatMessageInstanceChat)]
    private void HandleChatMessage(ChatMessage packet)
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

    [WorldPacketHandler(ClientOpcodes.ChatMessageAfk)]
    private void HandleChatMessageAFK(ChatMessageAFK packet)
    {
        var sender = _session.Player;

        if (sender.IsInCombat)
            return;

        if (sender.HasAura(1852))
        {
            _session.SendNotification(CypherStrings.GmSilence, sender.GetName());

            return;
        }

        if (sender.IsAfk) // Already AFK
        {
            if (string.IsNullOrEmpty(packet.Text))
                sender.ToggleAfk(); // Remove AFK
            else
                sender.AutoReplyMsg = packet.Text; // Update message
        }
        else // New AFK mode
        {
            sender.AutoReplyMsg = string.IsNullOrEmpty(packet.Text) ? _gameObjectManager.CypherStringCache.GetCypherString(CypherStrings.PlayerAfkDefault) : packet.Text;

            if (sender.IsDnd)
                sender.ToggleDnd();

            sender.ToggleAfk();
        }

        var guild = sender.Guild;

        if (guild != null)
            guild.SendEventAwayChanged(sender.GUID, sender.IsAfk, sender.IsDnd);

        _scriptManager.OnPlayerChat(sender, ChatMsg.Afk, Language.Universal, packet.Text);
    }

    [WorldPacketHandler(ClientOpcodes.ChatMessageChannel)]
    private void HandleChatMessageChannel(ChatMessageChannel packet)
    {
        HandleChat(ChatMsg.Channel, packet.Language, packet.Text, packet.Target, packet.ChannelGUID);
    }

    [WorldPacketHandler(ClientOpcodes.ChatMessageDnd)]
    private void HandleChatMessageDND(ChatMessageDND packet)
    {
        var sender = _session.Player;

        if (sender.IsInCombat)
            return;

        if (sender.HasAura(1852))
        {
            _session.SendNotification(CypherStrings.GmSilence, sender.GetName());

            return;
        }

        if (sender.IsDnd) // Already DND
        {
            if (string.IsNullOrEmpty(packet.Text))
                sender.ToggleDnd(); // Remove DND
            else
                sender.AutoReplyMsg = packet.Text; // Update message
        }
        else // New DND mode
        {
            sender.AutoReplyMsg = string.IsNullOrEmpty(packet.Text) ? _gameObjectManager.CypherStringCache.GetCypherString(CypherStrings.PlayerDndDefault) : packet.Text;

            if (sender.IsAfk)
                sender.ToggleAfk();

            sender.ToggleDnd();
        }

        var guild = sender.Guild;

        if (guild != null)
            guild.SendEventAwayChanged(sender.GUID, sender.IsAfk, sender.IsDnd);

        _scriptManager.OnPlayerChat(sender, ChatMsg.Dnd, Language.Universal, packet.Text);
    }

    [WorldPacketHandler(ClientOpcodes.ChatMessageEmote)]
    private void HandleChatMessageEmote(ChatMessageEmote packet)
    {
        HandleChat(ChatMsg.Emote, Language.Universal, packet.Text);
    }

    [WorldPacketHandler(ClientOpcodes.ChatMessageWhisper)]
    private void HandleChatMessageWhisper(ChatMessageWhisper packet)
    {
        HandleChat(ChatMsg.Whisper, packet.Language, packet.Text, packet.Target);
    }

    [WorldPacketHandler(ClientOpcodes.Emote, Processing = PacketProcessing.Inplace)]
    private void HandleEmote(EmoteClient packet)
    {
        if (packet == null || !_session.Player.IsAlive || _session.Player.HasUnitState(UnitState.Died))
            return;

        _scriptManager.ForEach<IPlayerOnClearEmote>(p => p.OnClearEmote(_session.Player));
        _session.Player.EmoteState = Emote.OneshotNone;
    }

    [WorldPacketHandler(ClientOpcodes.SendTextEmote, Processing = PacketProcessing.Inplace)]
    private void HandleTextEmote(CTextEmote packet)
    {
        if (!_session.Player.IsAlive)
            return;

        if (!_session.CanSpeak)
        {
            var timeStr = Time.SecsToTimeString((ulong)(_session.MuteTime - GameTime.CurrentTime));
            _session.SendNotification(CypherStrings.WaitBeforeSpeaking, timeStr);

            return;
        }

        _scriptManager.ForEach<IPlayerOnTextEmote>(p => p.OnTextEmote(_session.Player, (uint)packet.SoundIndex, (uint)packet.EmoteID, packet.Target));
        var em = _cliDb.EmotesTextStorage.LookupByKey(packet.EmoteID);

        if (em == null)
            return;

        var emote = (Emote)em.EmoteId;

        switch (emote)
        {
            case Emote.StateSleep:
            case Emote.StateSit:
            case Emote.StateKneel:
            case Emote.OneshotNone:
                break;

            case Emote.StateDance:
            case Emote.StateRead:
                _session.Player.EmoteState = emote;

                break;

            default:
                // Only allow text-emotes for "dead" entities (feign death included)
                if (_session.Player.HasUnitState(UnitState.Died))
                    break;

                _session.Player.HandleEmoteCommand(emote, null, packet.SpellVisualKitIDs, packet.SequenceVariation);

                break;
        }

        STextEmote textEmote = new();
        textEmote.SourceGUID = _session.Player.GUID;
        textEmote.SourceAccountGUID = _session.AccountGUID;
        textEmote.TargetGUID = packet.Target;
        textEmote.EmoteID = packet.EmoteID;
        textEmote.SoundIndex = packet.SoundIndex;
        _session.Player.SendMessageToSetInRange(textEmote, _config.GetValue("ListenRange.TextEmote", 40f), true);

        var unit = _objectAccessor.GetUnit(_session.Player, packet.Target);

        _session.Player.UpdateCriteria(CriteriaType.DoEmote, (uint)packet.EmoteID, 0, 0, unit);

        // Send scripted event call
        if (unit != null)
        {
            var creature = unit.AsCreature;

            if (creature != null)
                creature.AI.ReceiveEmote(_session.Player, (TextEmotes)packet.EmoteID);
        }

        if (emote != Emote.OneshotNone)
            _session.Player.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Anim);
    }

    private void SendChatPlayerNotfoundNotice(string name)
    {
        _session.SendPacket(new ChatPlayerNotfound(name));
    }

    private void SendChatRestricted(ChatRestrictionType restriction)
    {
        _session.SendPacket(new ChatRestricted(restriction));
    }

    private void SendPlayerAmbiguousNotice(string name)
    {
        _session.SendPacket(new ChatPlayerAmbiguous(name));
    }
}