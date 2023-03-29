// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum GroupFlags
{
    None = 0x00,
    FakeRaid = 0x01,
    Raid = 0x02,
    LfgRestricted = 0x04, // Script_HasLFGRestrictions()
    Lfg = 0x08,
    Destroyed = 0x10,
    OnePersonParty = 0x020,    // Script_IsOnePersonParty()
    EveryoneAssistant = 0x040, // Script_IsEveryoneAssistant()
    GuildGroup = 0x100,
    CrossFaction = 0x200,

    MaskBgRaid = FakeRaid | Raid
}