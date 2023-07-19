// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Networking.Packets.AuctionHouse;
using Framework.Constants;

namespace Forged.MapServer.AuctionHouse;

public record AuctionsBucketKey(uint ItemId, ushort ItemLevel, ushort BattlePetSpeciesId, ushort SuffixItemNameDescriptionId)
{
    public AuctionsBucketKey(AuctionBucketKey key) : this(key.ItemID, key.ItemLevel, key.BattlePetSpeciesID ?? 0, key.SuffixItemNameDescriptionID ?? 0) { }
}