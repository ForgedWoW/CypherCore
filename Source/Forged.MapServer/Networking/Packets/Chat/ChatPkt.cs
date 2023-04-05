// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
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

        SenderVirtualAddress = WorldManager.Realm.Id.GetAddress();
        TargetVirtualAddress = WorldManager.Realm.Id.GetAddress();
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
        WorldPacket.WriteUInt8((byte)SlashCmd);
        WorldPacket.WriteUInt32((uint)_Language);
        WorldPacket.WritePackedGuid(SenderGUID);
        WorldPacket.WritePackedGuid(SenderGuildGUID);
        WorldPacket.WritePackedGuid(SenderAccountGUID);
        WorldPacket.WritePackedGuid(TargetGUID);
        WorldPacket.WriteUInt32(TargetVirtualAddress);
        WorldPacket.WriteUInt32(SenderVirtualAddress);
        WorldPacket.WritePackedGuid(PartyGUID);
        WorldPacket.WriteUInt32(AchievementID);
        WorldPacket.WriteFloat(DisplayTime);
        WorldPacket.WriteBits(SenderName.GetByteCount(), 11);
        WorldPacket.WriteBits(TargetName.GetByteCount(), 11);
        WorldPacket.WriteBits(Prefix.GetByteCount(), 5);
        WorldPacket.WriteBits(Channel.GetByteCount(), 7);
        WorldPacket.WriteBits(ChatText.GetByteCount(), 12);
        WorldPacket.WriteBits((byte)_ChatFlags, 14);
        WorldPacket.WriteBit(HideChatLog);
        WorldPacket.WriteBit(FakeSenderName);
        WorldPacket.WriteBit(Unused_801.HasValue);
        WorldPacket.WriteBit(ChannelGUID.HasValue);
        WorldPacket.FlushBits();

        WorldPacket.WriteString(SenderName);
        WorldPacket.WriteString(TargetName);
        WorldPacket.WriteString(Prefix);
        WorldPacket.WriteString(Channel);
        WorldPacket.WriteString(ChatText);

        if (Unused_801.HasValue)
            WorldPacket.WriteUInt32(Unused_801.Value);

        if (ChannelGUID.HasValue)
            WorldPacket.WritePackedGuid(ChannelGUID.Value);
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