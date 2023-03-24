// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Common.Entities.Objects;
using Game.Common.Entities.Players;

namespace Game.Common.Networking.Packets.Social;

public class ContactInfo
{
	readonly uint VirtualRealmAddr;
	readonly uint NativeRealmAddr;
	readonly SocialFlag TypeFlags;
	readonly string Notes;
	readonly FriendStatus Status;
	readonly uint AreaID;
	readonly uint Level;
	readonly PlayerClass ClassID;
	readonly bool Mobile;

	readonly ObjectGuid Guid;
	readonly ObjectGuid WowAccountGuid;

	public ContactInfo(ObjectGuid guid, FriendInfo friendInfo)
	{
		Guid = guid;
		WowAccountGuid = friendInfo.WowAccountGuid;
		VirtualRealmAddr = Global.WorldMgr.VirtualRealmAddress;
		NativeRealmAddr = Global.WorldMgr.VirtualRealmAddress;
		TypeFlags = friendInfo.Flags;
		Notes = friendInfo.Note;
		Status = friendInfo.Status;
		AreaID = friendInfo.Area;
		Level = friendInfo.Level;
		ClassID = friendInfo.Class;
	}

	public void Write(WorldPacket data)
	{
		data.WritePackedGuid(Guid);
		data.WritePackedGuid(WowAccountGuid);
		data.WriteUInt32(VirtualRealmAddr);
		data.WriteUInt32(NativeRealmAddr);
		data.WriteUInt32((uint)TypeFlags);
		data.WriteUInt8((byte)Status);
		data.WriteUInt32(AreaID);
		data.WriteUInt32(Level);
		data.WriteUInt32((uint)ClassID);
		data.WriteBits(Notes.GetByteCount(), 10);
		data.WriteBit(Mobile);
		data.FlushBits();
		data.WriteString(Notes);
	}
}
