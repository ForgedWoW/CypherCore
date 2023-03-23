// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Entities.Items;
using Game.Entities;
using Game.Common.Entities.Players;
using Game.Common.Networking;

namespace Game.Common.Entities.Objects.Update;

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
