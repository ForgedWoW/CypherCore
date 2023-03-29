// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum NotifyFlags
{
    None = 0x00,
    AIRelocation = 0x01,
    VisibilityChanged = 0x02,
    All = 0xFF
}