// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Networking;

namespace Forged.MapServer.Entities.Objects.Update;

public class UnlockedAzeriteEssence
{
	public uint AzeriteEssenceID;
	public uint Rank;

	public void WriteCreate(WorldPacket data, AzeriteItem owner, Player receiver)
	{
		data.WriteUInt32(AzeriteEssenceID);
		data.WriteUInt32(Rank);
	}

	public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, AzeriteItem owner, Player receiver)
	{
		data.WriteUInt32(AzeriteEssenceID);
		data.WriteUInt32(Rank);
	}
}