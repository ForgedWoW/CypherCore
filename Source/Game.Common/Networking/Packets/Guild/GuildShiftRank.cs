// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.Guild;

public class GuildShiftRank : ClientPacket
{
	public bool ShiftUp;
	public int RankOrder;
	public GuildShiftRank(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		RankOrder = _worldPacket.ReadInt32();
		ShiftUp = _worldPacket.HasBit();
	}
}
