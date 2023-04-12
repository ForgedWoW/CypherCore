﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Globals;

public class PlayerChoiceResponseRewardEntry
{
    public PlayerChoiceResponseRewardEntry(uint id, int quantity)
    {
        Id = id;
        Quantity = quantity;
    }

    public uint Id { get; set; }
    public int Quantity { get; set; }
}