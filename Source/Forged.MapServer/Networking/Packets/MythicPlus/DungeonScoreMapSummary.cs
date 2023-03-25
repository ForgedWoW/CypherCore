// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Networking.Packets;

public struct DungeonScoreMapSummary
{
	public int ChallengeModeID;
	public float MapScore;
	public int BestRunLevel;
	public int BestRunDurationMS;
	public bool FinishedSuccess;

	public void Write(WorldPacket data)
	{
		data.WriteInt32(ChallengeModeID);
		data.WriteFloat(MapScore);
		data.WriteInt32(BestRunLevel);
		data.WriteInt32(BestRunDurationMS);
		data.WriteBit(FinishedSuccess);
		data.FlushBits();
	}
}