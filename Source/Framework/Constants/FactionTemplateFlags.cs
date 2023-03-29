// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum FactionTemplateFlags
{
    PVP = 0x800,             // flagged for PvP
    ContestedGuard = 0x1000, // faction will attack players that were involved in PvP combats
    HostileByDefault = 0x2000
}