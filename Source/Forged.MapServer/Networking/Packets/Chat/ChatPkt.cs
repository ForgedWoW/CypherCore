// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.World;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Chat;

public class ChatPkt : ServerPacket
{
    public ChatFlags ChatFlags;
    public Language Language = Language.Universal;
    public uint AchievementID;
    public string Channel = "";
    public ObjectGuid? ChannelGUID;
    public string ChatText = "";
    public float DisplayTime;
    public bool FakeSenderName;
    public bool HideChatLog;
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
    public uint SpellID;
    public uint? Unused801;
    public ChatPkt() : base(ServerOpcodes.Chat) { }

    public void Initialize(ChatMsg chatType, Language language, WorldObject sender, WorldObject receiver, string message, uint achievementId = 0, string channelName = "", Locale locale = Locale.enUS, string addonPrefix = "")
    {
        // Clear everything because same packet can be used multiple times
        Clear();

        SenderGUID.Clear();
        SenderAccountGUID.Clear();
        SenderGuildGUID.Clear();
        TargetGUID.Clear();
        SenderName = "";
        TargetName = "";
        ChatFlags = ChatFlags.None;

        SlashCmd = chatType;
        Language = language;

        if (sender != null)
            SetSender(sender, locale);

        if (receiver != null)
            SetReceiver(receiver, locale);

        SenderVirtualAddress = WorldManager.Realm.Id.VirtualRealmAddress;
        TargetVirtualAddress = WorldManager.Realm.Id.VirtualRealmAddress;
        AchievementID = achievementId;
        Channel = channelName;
        Prefix = addonPrefix;
        ChatText = message;
    }

    public void SetReceiver(WorldObject receiver, Locale locale)
    {
        TargetGUID = receiver.GUID;

        var creatureReceiver = receiver.AsCreature;

        if (creatureReceiver != null)
            TargetName = creatureReceiver.GetName(locale);
    }

    public override void Write()
    {
        WorldPacket.WriteUInt8((byte)SlashCmd);
        WorldPacket.WriteUInt32((uint)Language);
        WorldPacket.WritePackedGuid(SenderGUID);
        WorldPacket.WritePackedGuid(SenderGuildGUID);
        WorldPacket.WritePackedGuid(SenderAccountGUID);
        WorldPacket.WritePackedGuid(TargetGUID);
        WorldPacket.WriteUInt32(TargetVirtualAddress);
        WorldPacket.WriteUInt32(SenderVirtualAddress);
        WorldPacket.WriteUInt32(AchievementID);
        WorldPacket.WriteFloat(DisplayTime);
        WorldPacket.WriteUInt32(SpellID);
        WorldPacket.WriteBits(SenderName.GetByteCount(), 11);
        WorldPacket.WriteBits(TargetName.GetByteCount(), 11);
        WorldPacket.WriteBits(Prefix.GetByteCount(), 5);
        WorldPacket.WriteBits(Channel.GetByteCount(), 7);
        WorldPacket.WriteBits(ChatText.GetByteCount(), 12);
        WorldPacket.WriteBits((ushort)ChatFlags, 14);
        WorldPacket.WriteBit(HideChatLog);
        WorldPacket.WriteBit(FakeSenderName);
        WorldPacket.WriteBit(Unused801.HasValue);
        WorldPacket.WriteBit(ChannelGUID.HasValue);
        WorldPacket.FlushBits();

        WorldPacket.WriteString(SenderName);
        WorldPacket.WriteString(TargetName);
        WorldPacket.WriteString(Prefix);
        WorldPacket.WriteString(Channel);
        WorldPacket.WriteString(ChatText);

        if (Unused801.HasValue)
            WorldPacket.WriteUInt32(Unused801.Value);

        if (ChannelGUID.HasValue)
            WorldPacket.WritePackedGuid(ChannelGUID.Value);
    }

    private void SetSender(WorldObject sender, Locale locale)
    {
        SenderGUID = sender.GUID;

        var creatureSender = sender.AsCreature;

        if (creatureSender != null)
            SenderName = creatureSender.GetName(locale);

        var playerSender = sender.AsPlayer;

        if (playerSender == null)
            return;

        SenderAccountGUID = playerSender.Session.AccountGUID;
        ChatFlags = playerSender.ChatFlags;

        SenderGuildGUID = ObjectGuid.Create(HighGuid.Guild, playerSender.GuildId);
    }
}