// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.LFG;

public struct LfgPlayerQuestRewardCurrency
{
    public uint CurrencyID;

    public uint Quantity;

    public LfgPlayerQuestRewardCurrency(uint currencyId, uint quantity)
    {
        CurrencyID = currencyId;
        Quantity = quantity;
    }
}