// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum SpellAreaFlag
{
    AutoCast = 0x1,                          // if has autocast, spell is applied on enter
    AutoRemove = 0x2,                        // if has autoremove, spell is remove automatically inside zone/area (always removed on leaving area or zone)
    IgnoreAutocastOnQuestStatusChange = 0x4, // if this flag is set then spell will not be applied automatically on quest status change
}