// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Globals;

public class PlayerChoiceResponseRewardItem
{
    public PlayerChoiceResponseRewardItem() { }

    public PlayerChoiceResponseRewardItem(uint id, List<uint> bonusListIDs, int quantity)
    {
        Id = id;
        BonusListIDs = bonusListIDs;
        Quantity = quantity;
    }

    public List<uint> BonusListIDs { get; set; } = new();
    public uint Id { get; set; }
    public int Quantity { get; set; }
}