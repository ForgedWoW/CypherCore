// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum QueryDataGroup
{
    Creatures = 0x01,
    Gameobjects = 0x02,
    Items = 0x04,
    Quests = 0x08,
    POIs = 0x10,

    All = 0xFF
}