// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Entities.Items;

public class ArtifactPowerData
{
    public uint ArtifactPowerId { get; set; }
    public byte CurrentRankWithBonus { get; set; }
    public byte PurchasedRank { get; set; }
}