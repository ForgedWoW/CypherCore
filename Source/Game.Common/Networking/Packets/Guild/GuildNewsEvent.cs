// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.Common.Entities.Objects;
using Game.Common.Networking.Packets.Item;

namespace Game.Common.Networking.Packets.Guild;

public class GuildNewsEvent
{
	public int Id;
	public uint CompletedDate;
	public int Type;
	public int Flags;
	public int[] Data = new int[2];
	public ObjectGuid MemberGuid;
	public List<ObjectGuid> MemberList = new();
	public ItemInstance Item;

	public void Write(WorldPacket data)
	{
		data.WriteInt32(Id);
		data.WritePackedTime(CompletedDate);
		data.WriteInt32(Type);
		data.WriteInt32(Flags);

		for (byte i = 0; i < 2; i++)
			data.WriteInt32(Data[i]);

		data.WritePackedGuid(MemberGuid);
		data.WriteInt32(MemberList.Count);

		foreach (var memberGuid in MemberList)
			data.WritePackedGuid(memberGuid);

		data.WriteBit(Item != null);
		data.FlushBits();

		if (Item != null)
			Item.Write(data);
	}
}
