// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Networking;

namespace Forged.RealmServer.Entities;

public class ArtifactPower
{
	public ushort ArtifactPowerId;
	public byte PurchasedRank;
	public byte CurrentRankWithBonus;

	public void WriteCreate(WorldPacket data, Item owner, Player receiver)
	{
		data.WriteUInt16(ArtifactPowerId);
		data.WriteUInt8(PurchasedRank);
		data.WriteUInt8(CurrentRankWithBonus);
	}

	public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Item owner, Player receiver)
	{
		data.WriteUInt16(ArtifactPowerId);
		data.WriteUInt8(PurchasedRank);
		data.WriteUInt8(CurrentRankWithBonus);
	}
}