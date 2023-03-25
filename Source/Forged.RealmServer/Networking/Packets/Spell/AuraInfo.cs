// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.Networking.Packets;

public struct AuraInfo
{
	public void Write(WorldPacket data)
	{
		data.WriteUInt8(Slot);
		data.WriteBit(AuraData != null);
		data.FlushBits();

		if (AuraData != null)
			AuraData.Write(data);
	}

	public byte Slot;
	public AuraDataInfo AuraData;
}