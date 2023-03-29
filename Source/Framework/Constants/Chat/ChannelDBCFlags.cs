// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum ChannelDBCFlags
{
    None = 0x00000,
    Initial = 0x00001,   // General, Trade, Localdefense, Lfg
    ZoneDep = 0x00002,   // General, Trade, Localdefense, Guildrecruitment
    Global = 0x00004,    // Worlddefense
    Trade = 0x00008,     // Trade, Lfg
    CityOnly = 0x00010,  // Trade, Guildrecruitment, Lfg
    CityOnly2 = 0x00020, // Trade, Guildrecruitment, Lfg
    Defense = 0x10000,   // Localdefense, Worlddefense
    GuildReq = 0x20000,  // Guildrecruitment
    Lfg = 0x40000,       // Lfg
    Unk1 = 0x80000,      // General
    NoClientJoin = 0x200000
}