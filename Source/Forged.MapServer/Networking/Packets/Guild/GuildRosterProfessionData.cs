// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Networking.Packets;

public struct GuildRosterProfessionData
{
	public void Write(WorldPacket data)
	{
		data.WriteInt32(DbID);
		data.WriteInt32(Rank);
		data.WriteInt32(Step);
	}

	public int DbID;
	public int Rank;
	public int Step;
}