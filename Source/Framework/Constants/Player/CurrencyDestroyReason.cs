// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum CurrencyDestroyReason
{
    Cheat = 0,
    Spell = 1,
    VersionUpdate = 2,
    QuestTurnin = 3,
    Vendor = 4,
    Trade = 5,
    Capped = 6,
    Garrison = 7,
    DroppedToCorpse = 8,
    BonusRoll = 9,
    FactionConversion = 10,
    FulfillCraftingOrder = 11,
    Last = 12
}