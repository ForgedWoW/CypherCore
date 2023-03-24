// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.Spell;

public class CancelChannelling : ClientPacket
{
	public int ChannelSpell;
	public int Reason; // 40 = /run SpellStopCasting(), 16 = movement/AURA_INTERRUPT_FLAG_MOVE, 41 = turning/AURA_INTERRUPT_FLAG_TURNING
	public CancelChannelling(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		ChannelSpell = _worldPacket.ReadInt32();
		Reason = _worldPacket.ReadInt32();
	}
	// does not match SpellCastResult enum
}
