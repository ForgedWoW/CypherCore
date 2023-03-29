// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum HeirloomPlayerFlags
{
    None = 0x00,
    UpgradeLevel1 = 0x01,
    UpgradeLevel2 = 0x02,
    UpgradeLevel3 = 0x04,
    UpgradeLevel4 = 0x08,
    UpgradeLevel5 = 0x10,
    UpgradeLevel6 = 0x20,
}