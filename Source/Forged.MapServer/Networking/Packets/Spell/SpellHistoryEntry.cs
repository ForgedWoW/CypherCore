// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Spell;

public class SpellHistoryEntry
{
	public uint SpellID;
	public uint ItemID;
	public uint Category;
	public int RecoveryTime;
	public int CategoryRecoveryTime;
	public float ModRate = 1.0f;
	public bool OnHold;
	readonly uint? unused622_1; // This field is not used for anything in the client in 6.2.2.20444
	readonly uint? unused622_2; // This field is not used for anything in the client in 6.2.2.20444

	public void Write(WorldPacket data)
	{
		data.WriteUInt32(SpellID);
		data.WriteUInt32(ItemID);
		data.WriteUInt32(Category);
		data.WriteInt32(RecoveryTime);
		data.WriteInt32(CategoryRecoveryTime);
		data.WriteFloat(ModRate);
		data.WriteBit(unused622_1.HasValue);
		data.WriteBit(unused622_2.HasValue);
		data.WriteBit(OnHold);
		data.FlushBits();

		if (unused622_1.HasValue)
			data.WriteUInt32(unused622_1.Value);

		if (unused622_2.HasValue)
			data.WriteUInt32(unused622_2.Value);
	}
}