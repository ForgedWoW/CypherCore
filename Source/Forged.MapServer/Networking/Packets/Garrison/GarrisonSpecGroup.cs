// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Networking.Packets;

struct GarrisonSpecGroup
{
	public int ChrSpecializationID;
	public int SoulbindID;

	public void Write(WorldPacket data)
	{
		data.WriteInt32(ChrSpecializationID);
		data.WriteInt32(SoulbindID);
	}
}