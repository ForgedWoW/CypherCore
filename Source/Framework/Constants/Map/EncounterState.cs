// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum EncounterState
{
    NotStarted = 0,
    InProgress = 1,
    Fail = 2,
    Done = 3,
    Special = 4,
    ToBeDecided = 5
}