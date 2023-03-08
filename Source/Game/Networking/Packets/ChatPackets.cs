// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

public class ChatMessage : ClientPacket
{
	public string Text;
	public Language Language = Language.Universal;
	public ChatMessage(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Language = (Language)_worldPacket.ReadInt32();
		var len = _worldPacket.ReadBits<uint>(11);
		Text = _worldPacket.ReadString(len);
	}
}

public class ChatMessageWhisper : ClientPacket
{
	public Language Language = Language.Universal;
	public string Text;
	public string Target;
	public ChatMessageWhisper(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Language = (Language)_worldPacket.ReadInt32();
		var targetLen = _worldPacket.ReadBits<uint>(9);
		var textLen = _worldPacket.ReadBits<uint>(11);
		Target = _worldPacket.ReadString(targetLen);
		Text = _worldPacket.ReadString(textLen);
	}
}

public class ChatMessageChannel : ClientPacket
{
	public Language Language = Language.Universal;
	public ObjectGuid ChannelGUID;
	public string Text;
	public string Target;
	public ChatMessageChannel(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Language = (Language)_worldPacket.ReadInt32();
		ChannelGUID = _worldPacket.ReadPackedGuid();
		var targetLen = _worldPacket.ReadBits<uint>(9);
		var textLen = _worldPacket.ReadBits<uint>(11);
		Target = _worldPacket.ReadString(targetLen);
		Text = _worldPacket.ReadString(textLen);
	}
}

public class ChatAddonMessage : ClientPacket
{
	public ChatAddonMessageParams Params = new();
	public ChatAddonMessage(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Params.Read(_worldPacket);
	}
}

class ChatAddonMessageTargeted : ClientPacket
{
	public string Target;
	public ChatAddonMessageParams Params = new();
	public ObjectGuid? ChannelGUID; // not optional in the packet. Optional for api reasons
	public ChatAddonMessageTargeted(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		var targetLen = _worldPacket.ReadBits<uint>(9);
		Params.Read(_worldPacket);
		ChannelGUID = _worldPacket.ReadPackedGuid();
		Target = _worldPacket.ReadString(targetLen);
	}
}

public class ChatMessageDND : ClientPacket
{
	public string Text;
	public ChatMessageDND(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		var len = _worldPacket.ReadBits<uint>(11);
		Text = _worldPacket.ReadString(len);
	}
}

public class ChatMessageAFK : ClientPacket
{
	public string Text;
	public ChatMessageAFK(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		var len = _worldPacket.ReadBits<uint>(11);
		Text = _worldPacket.ReadString(len);
	}
}

public class ChatMessageEmote : ClientPacket
{
	public string Text;
	public ChatMessageEmote(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		var len = _worldPacket.ReadBits<uint>(11);
		Text = _worldPacket.ReadString(len);
	}
}

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

public class EmoteMessage : ServerPacket
{
	public ObjectGuid Guid;
	public uint EmoteID;
	public List<uint> SpellVisualKitIDs = new();
	public int SequenceVariation;
	public EmoteMessage() : base(ServerOpcodes.Emote, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Guid);
		_worldPacket.WriteUInt32(EmoteID);
		_worldPacket.WriteInt32(SpellVisualKitIDs.Count);
		_worldPacket.WriteInt32(SequenceVariation);

		foreach (var id in SpellVisualKitIDs)
			_worldPacket.WriteUInt32(id);
	}
}

public class CTextEmote : ClientPacket
{
	public ObjectGuid Target;
	public int EmoteID;
	public int SoundIndex;
	public uint[] SpellVisualKitIDs;
	public int SequenceVariation;
	public CTextEmote(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Target = _worldPacket.ReadPackedGuid();
		EmoteID = _worldPacket.ReadInt32();
		SoundIndex = _worldPacket.ReadInt32();

		SpellVisualKitIDs = new uint[_worldPacket.ReadUInt32()];
		SequenceVariation = _worldPacket.ReadInt32();

		for (var i = 0; i < SpellVisualKitIDs.Length; ++i)
			SpellVisualKitIDs[i] = _worldPacket.ReadUInt32();
	}
}

public class STextEmote : ServerPacket
{
	public ObjectGuid SourceGUID;
	public ObjectGuid SourceAccountGUID;
	public ObjectGuid TargetGUID;
	public int SoundIndex = -1;
	public int EmoteID;
	public STextEmote() : base(ServerOpcodes.TextEmote, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(SourceGUID);
		_worldPacket.WritePackedGuid(SourceAccountGUID);
		_worldPacket.WriteInt32(EmoteID);
		_worldPacket.WriteInt32(SoundIndex);
		_worldPacket.WritePackedGuid(TargetGUID);
	}
}

public class PrintNotification : ServerPacket
{
	public string NotifyText;

	public PrintNotification(string notifyText) : base(ServerOpcodes.PrintNotification)
	{
		NotifyText = notifyText;
	}

	public override void Write()
	{
		_worldPacket.WriteBits(NotifyText.GetByteCount(), 12);
		_worldPacket.WriteString(NotifyText);
	}
}

public class EmoteClient : ClientPacket
{
	public EmoteClient(WorldPacket packet) : base(packet) { }

	public override void Read() { }
}

class ChatPlayerNotfound : ServerPacket
{
	readonly string Name;

	public ChatPlayerNotfound(string name) : base(ServerOpcodes.ChatPlayerNotfound)
	{
		Name = name;
	}

	public override void Write()
	{
		_worldPacket.WriteBits(Name.GetByteCount(), 9);
		_worldPacket.WriteString(Name);
	}
}

class ChatPlayerAmbiguous : ServerPacket
{
	readonly string Name;

	public ChatPlayerAmbiguous(string name) : base(ServerOpcodes.ChatPlayerAmbiguous)
	{
		Name = name;
	}

	public override void Write()
	{
		_worldPacket.WriteBits(Name.GetByteCount(), 9);
		_worldPacket.WriteString(Name);
	}
}

class ChatRestricted : ServerPacket
{
	readonly ChatRestrictionType Reason;

	public ChatRestricted(ChatRestrictionType reason) : base(ServerOpcodes.ChatRestricted)
	{
		Reason = reason;
	}

	public override void Write()
	{
		_worldPacket.WriteUInt8((byte)Reason);
	}
}

class ChatServerMessage : ServerPacket
{
	public int MessageID;
	public string StringParam = "";
	public ChatServerMessage() : base(ServerOpcodes.ChatServerMessage) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(MessageID);

		_worldPacket.WriteBits(StringParam.GetByteCount(), 11);
		_worldPacket.WriteString(StringParam);
	}
}

class ChatRegisterAddonPrefixes : ClientPacket
{
	public string[] Prefixes = new string[64];
	public ChatRegisterAddonPrefixes(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		var count = _worldPacket.ReadInt32();

		for (var i = 0; i < count && i < 64; ++i)
			Prefixes[i] = _worldPacket.ReadString(_worldPacket.ReadBits<uint>(5));
	}
}

class ChatUnregisterAllAddonPrefixes : ClientPacket
{
	public ChatUnregisterAllAddonPrefixes(WorldPacket packet) : base(packet) { }

	public override void Read() { }
}

class DefenseMessage : ServerPacket
{
	public uint ZoneID;
	public string MessageText = "";
	public DefenseMessage() : base(ServerOpcodes.DefenseMessage) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(ZoneID);
		_worldPacket.WriteBits(MessageText.GetByteCount(), 12);
		_worldPacket.FlushBits();
		_worldPacket.WriteString(MessageText);
	}
}

class ChatReportIgnored : ClientPacket
{
	public ObjectGuid IgnoredGUID;
	public byte Reason;
	public ChatReportIgnored(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		IgnoredGUID = _worldPacket.ReadPackedGuid();
		Reason = _worldPacket.ReadUInt8();
	}
}

public class ChatAddonMessageParams
{
	public string Prefix;
	public string Text;
	public ChatMsg Type = ChatMsg.Party;
	public bool IsLogged;

	public void Read(WorldPacket data)
	{
		var prefixLen = data.ReadBits<uint>(5);
		var textLen = data.ReadBits<uint>(8);
		IsLogged = data.HasBit();
		Type = (ChatMsg)data.ReadInt32();
		Prefix = data.ReadString(prefixLen);
		Text = data.ReadString(textLen);
	}
}