// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

public class ChatPkt : ServerPacket
{
	public ChatMsg SlashCmd;
	public Language _Language = Language.Universal;
	public ObjectGuid SenderGUID;
	public ObjectGuid SenderGuildGUID;
	public ObjectGuid SenderAccountGUID;
	public ObjectGuid TargetGUID;
	public ObjectGuid PartyGUID;
	public uint SenderVirtualAddress;
	public uint TargetVirtualAddress;
	public string SenderName = "";
	public string TargetName = "";
	public string Prefix = "";
	public string Channel = "";
	public string ChatText = "";
	public uint AchievementID;
	public ChatFlags _ChatFlags;
	public float DisplayTime;
	public uint? Unused_801;
	public bool HideChatLog;
	public bool FakeSenderName;
	public ObjectGuid? ChannelGUID;
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

		SenderVirtualAddress = _worldManager.VirtualRealmAddress;
		TargetVirtualAddress = _worldManager.VirtualRealmAddress;
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

	void SetSender(WorldObject sender, Locale locale)
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