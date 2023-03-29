// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum CurrencyTypesFlagsB
{
    UseTotalEarnedForEarned = 0x01,
    ShowQuestXPGainInTooltip = 0x02,            // NYI
    NoNotificationMailOnOfflineProgress = 0x04, // NYI
    BattlenetVirtualCurrency = 0x08             // NYI
}