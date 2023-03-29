// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum PlayerDelayedOperations
{
    SavePlayer = 0x01,
    ResurrectPlayer = 0x02,
    SpellCastDeserter = 0x04,
    BGMountRestore = 0x08, // Flag to restore mount state after teleport from BG
    BGTaxiRestore = 0x10,  // Flag to restore taxi state after teleport from BG
    BGGroupRestore = 0x20, // Flag to restore group state after teleport from BG
    End
}