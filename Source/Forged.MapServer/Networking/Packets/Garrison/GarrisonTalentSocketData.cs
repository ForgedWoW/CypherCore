﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Networking.Packets;

struct GarrisonTalentSocketData
{
	public int SoulbindConduitID;
	public int SoulbindConduitRank;

	public void Write(WorldPacket data)
	{
		data.WriteInt32(SoulbindConduitID);
		data.WriteInt32(SoulbindConduitRank);
	}
}