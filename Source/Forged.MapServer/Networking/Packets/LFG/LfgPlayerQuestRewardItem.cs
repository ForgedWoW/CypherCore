// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.LFG;

public struct LfgPlayerQuestRewardItem
{
    public uint ItemID;

    public uint Quantity;

    public LfgPlayerQuestRewardItem(uint itemId, uint quantity)
    {
        ItemID = itemId;
        Quantity = quantity;
    }
}