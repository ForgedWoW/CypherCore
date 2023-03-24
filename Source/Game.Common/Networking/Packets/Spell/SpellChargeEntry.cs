// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.Spell;

public class SpellChargeEntry
{
	public uint Category;
	public uint NextRecoveryTime;
	public float ChargeModRate = 1.0f;
	public byte ConsumedCharges;

	public void Write(WorldPacket data)
	{
		data.WriteUInt32(Category);
		data.WriteUInt32(NextRecoveryTime);
		data.WriteFloat(ChargeModRate);
		data.WriteUInt8(ConsumedCharges);
	}
}
