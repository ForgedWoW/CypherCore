// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.BattleGround;

class RatedMatchDeserterPenalty
{
	public int PersonalRatingChange;
	public int QueuePenaltySpellID;
	public int QueuePenaltyDuration;

	public void Write(WorldPacket data)
	{
		data.WriteInt32(PersonalRatingChange);
		data.WriteInt32(QueuePenaltySpellID);
		data.WriteInt32(QueuePenaltyDuration);
	}
}