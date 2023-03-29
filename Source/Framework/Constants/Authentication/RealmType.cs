// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum RealmType
{
    Normal = 0,
    PVP = 1,
    Normal2 = 4,
    RP = 6,
    RPPVP = 8,

    MaxType = 14,

    FFAPVP = 16 // custom, free for all pvp mode like arena PvP in all zones except rest activated places and sanctuaries
    // replaced by REALM_PVP in realm list
}