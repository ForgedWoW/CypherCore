// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum GuildBankRights
{
    ViewTab = 0x01,
    PutItem = 0x02,
    UpdateText = 0x04,

    DepositItem = ViewTab | PutItem,
    Full = -1
}