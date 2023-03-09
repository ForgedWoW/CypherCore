// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Networking.Packets;

public struct SpellHealPrediction
{
	public ObjectGuid BeaconGUID;
	public uint Points;
	public byte Type;

	public void Write(WorldPacket data)
	{
		data.WriteUInt32(Points);
		data.WriteUInt8(Type);
		data.WritePackedGuid(BeaconGUID);
	}
}