﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Chat;

public class ChatPkt : ServerPacket
{
    public ChatFlags _ChatFlags;
    public Language _Language = Language.Universal;
    public uint AchievementID;
    public string Channel = "";
    public ObjectGuid? ChannelGUID;
    public string ChatText = "";
    public float DisplayTime;
    public bool FakeSenderName;
    public bool HideChatLog;
    public ObjectGuid PartyGUID;
    public string Prefix = "";
    public ObjectGuid SenderAccountGUID;
    public ObjectGuid SenderGUID;
    public ObjectGuid SenderGuildGUID;
    public string SenderName = "";
    public uint SenderVirtualAddress;
    public ChatMsg SlashCmd;
    public ObjectGuid TargetGUID;
    public string TargetName = "";
    public uint TargetVirtualAddress;
    public uint? Unused_801;
    public ChatPkt() : base(ServerOpcodes.Chat) { }

    public void Initialize(ChatMsg chatType, Language language, WorldObject sender, WorldObject receiver, string message, uint achievementId = 0, string channelName = "", Locale locale = Locale.enUS, string addonPrefix = "")
    {
        // Clear everything because same packet can be used multiple times
        Clear();

        SenderGUID.Clear();
        SenderAccountGUID.Clear();
        SenderGuildGUID.Clear();
        PartyGUID.Clear();
        TargetGUID.Clear();
        SenderName = "";
        TargetName = "";
        _ChatFlags = ChatFlags.None;

        SlashCmd = chatType;
        _Language = language;

        if (sender)
            SetSender(sender, locale);

        if (receiver)
            SetReceiver(receiver, locale);

        SenderVirtualAddress = Global.WorldMgr.VirtualRealmAddress;
        TargetVirtualAddress = Global.WorldMgr.VirtualRealmAddress;
        AchievementID = achievementId;
        Channel = channelName;
        Prefix = addonPrefix;
        ChatText = message;
    }

    public void SetReceiver(WorldObject receiver, Locale locale)
    {
        TargetGUID = receiver.GUID;

        var creatureReceiver = receiver.AsCreature;

        if (creatureReceiver)
            TargetName = creatureReceiver.GetName(locale);
    }

    public override void Write()
    {
        _worldPacket.WriteUInt8((byte)SlashCmd);
        _worldPacket.WriteUInt32((uint)_Language);
        _worldPacket.WritePackedGuid(SenderGUID);
        _worldPacket.WritePackedGuid(SenderGuildGUID);
        _worldPacket.WritePackedGuid(SenderAccountGUID);
        _worldPacket.WritePackedGuid(TargetGUID);
        _worldPacket.WriteUInt32(TargetVirtualAddress);
        _worldPacket.WriteUInt32(SenderVirtualAddress);
        _worldPacket.WritePackedGuid(PartyGUID);
        _worldPacket.WriteUInt32(AchievementID);
        _worldPacket.WriteFloat(DisplayTime);
        _worldPacket.WriteBits(SenderName.GetByteCount(), 11);
        _worldPacket.WriteBits(TargetName.GetByteCount(), 11);
        _worldPacket.WriteBits(Prefix.GetByteCount(), 5);
        _worldPacket.WriteBits(Channel.GetByteCount(), 7);
        _worldPacket.WriteBits(ChatText.GetByteCount(), 12);
        _worldPacket.WriteBits((byte)_ChatFlags, 14);
        _worldPacket.WriteBit(HideChatLog);
        _worldPacket.WriteBit(FakeSenderName);
        _worldPacket.WriteBit(Unused_801.HasValue);
        _worldPacket.WriteBit(ChannelGUID.HasValue);
        _worldPacket.FlushBits();

        _worldPacket.WriteString(SenderName);
        _worldPacket.WriteString(TargetName);
        _worldPacket.WriteString(Prefix);
        _worldPacket.WriteString(Channel);
        _worldPacket.WriteString(ChatText);

        if (Unused_801.HasValue)
            _worldPacket.WriteUInt32(Unused_801.Value);

        if (ChannelGUID.HasValue)
            _worldPacket.WritePackedGuid(ChannelGUID.Value);
    }

    private void SetSender(WorldObject sender, Locale locale)
    {
        SenderGUID = sender.GUID;

        var creatureSender = sender.AsCreature;

        if (creatureSender)
            SenderName = creatureSender.GetName(locale);

        var playerSender = sender.AsPlayer;

        if (playerSender)
        {
            SenderAccountGUID = playerSender.Session.AccountGUID;
            _ChatFlags = playerSender.ChatFlags;

            SenderGuildGUID = ObjectGuid.Create(HighGuid.Guild, playerSender.GuildId);

            var group = playerSender.Group;

            if (group)
                PartyGUID = group.GUID;
        }
    }
}