// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum ItemSearchLocation
{
    Equipment = 0x01,
    Inventory = 0x02,
    Bank = 0x04,
    ReagentBank = 0x08,

    Default = Equipment | Inventory,
    Everywhere = Equipment | Inventory | Bank | ReagentBank
}