// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.RealmServer.Networking.Packets;

public struct TreasureItem
{
	public GossipOptionRewardType Type;
	public int ID;
	public int Quantity;

	public void Write(WorldPacket data)
	{
		data.WriteBits((byte)Type, 1);
		data.WriteInt32(ID);
		data.WriteInt32(Quantity);
	}
}