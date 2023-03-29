// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Database;

public enum DatabaseTypeFlags
{
    None = 0,

    Login = 1,
    Character = 2,
    World = 4,
    Hotfix = 8,

    All = Login | Character | World | Hotfix
}