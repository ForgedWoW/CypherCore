// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Networking.Packets.Item;

namespace Forged.MapServer.Networking.Packets.LFG;

public struct LFGPlayerRewards
{
    public int BonusQuantity;

    public uint Quantity;

    public uint? RewardCurrency;

    public ItemInstance RewardItem;

    public LFGPlayerRewards(uint id, uint quantity, int bonusQuantity, bool isCurrency)
    {
        Quantity = quantity;
        BonusQuantity = bonusQuantity;
        RewardItem = null;
        RewardCurrency = null;

        if (!isCurrency)
            RewardItem = new ItemInstance
            {
                ItemID = id
            };
        else
            RewardCurrency = id;
    }

    public void Write(WorldPacket data)
    {
        data.WriteBit(RewardItem != null);
        data.WriteBit(RewardCurrency.HasValue);

        if (RewardItem != null)
            RewardItem.Write(data);

        data.WriteUInt32(Quantity);
        data.WriteInt32(BonusQuantity);

        if (RewardCurrency.HasValue)
            data.WriteUInt32(RewardCurrency.Value);
    }
}