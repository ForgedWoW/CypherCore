// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Networking.Packets.AuctionHouse;

public class BucketInfo
{
    public byte? BattlePetBreedID;
    public bool ContainsOnlyCollectedAppearances;
    public bool ContainsOwnerItem;
    public List<uint> ItemModifiedAppearanceIDs = new();
    public AuctionBucketKey Key;
    public byte? MaxBattlePetLevel;
    public byte? MaxBattlePetQuality;
    public ulong MinPrice;
    public int RequiredLevel;
    public int TotalQuantity;
    public uint? Unk901_1;
    public void Write(WorldPacket data)
    {
        Key.Write(data);
        data.WriteInt32(TotalQuantity);
        data.WriteInt32(RequiredLevel);
        data.WriteUInt64(MinPrice);
        data.WriteInt32(ItemModifiedAppearanceIDs.Count);

        if (!ItemModifiedAppearanceIDs.Empty())
            foreach (int id in ItemModifiedAppearanceIDs)
                data.WriteInt32(id);

        data.WriteBit(MaxBattlePetQuality.HasValue);
        data.WriteBit(MaxBattlePetLevel.HasValue);
        data.WriteBit(BattlePetBreedID.HasValue);
        data.WriteBit(Unk901_1.HasValue);
        data.WriteBit(ContainsOwnerItem);
        data.WriteBit(ContainsOnlyCollectedAppearances);
        data.FlushBits();

        if (MaxBattlePetQuality.HasValue)
            data.WriteUInt8(MaxBattlePetQuality.Value);

        if (MaxBattlePetLevel.HasValue)
            data.WriteUInt8(MaxBattlePetLevel.Value);

        if (BattlePetBreedID.HasValue)
            data.WriteUInt8(BattlePetBreedID.Value);

        if (Unk901_1.HasValue)
            data.WriteUInt32(Unk901_1.Value);
    }
}