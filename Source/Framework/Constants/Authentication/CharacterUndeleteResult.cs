// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum CharacterUndeleteResult
{
    Ok = 0,
    Cooldown = 1,
    CharCreate = 2,
    Disabled = 3,
    NameTakenByThisAccount = 4,
    Unknown = 5
}