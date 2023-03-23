// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;
using Game.Common.Networking.Packets.Garrison;

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.Garrison;

struct GarrisonTalent
{
	public void Write(WorldPacket data)
	{
		data.WriteInt32(GarrTalentID);
		data.WriteInt32(Rank);
		data.WriteInt64(ResearchStartTime);
		data.WriteInt32(Flags);
		data.WriteBit(Socket.HasValue);
		data.FlushBits();

		if (Socket.HasValue)
			Socket.Value.Write(data);
	}

	public int GarrTalentID;
	public int Rank;
	public long ResearchStartTime;
	public int Flags;
	public GarrisonTalentSocketData? Socket;
}
