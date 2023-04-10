// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Events;

public enum GameEventState
{
    Normal = 0,          // standard GameInfo events
    WorldInactive = 1,   // not yet started
    WorldConditions = 2, // condition matching phase
    WorldNextPhase = 3,  // conditions are met, now 'length' timer to start next event
    WorldFinished = 4,   // next events are started, unapply this one
    Internal = 5         // never handled in update
}