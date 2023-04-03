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