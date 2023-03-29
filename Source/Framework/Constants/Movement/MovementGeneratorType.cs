// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum MovementGeneratorType
{
    Idle = 0,     // IdleMovement
    Random = 1,   // RandomMovement
    Waypoint = 2, // WaypointMovement

    MaxDB = 3,               // *** this and below motion types can't be set in DB.
    Confused = 4,            // ConfusedMovementGenerator
    Chase = 5,               // TargetedMovementGenerator
    Home = 6,                // HomeMovementGenerator
    Flight = 7,              // WaypointMovementGenerator
    Point = 8,               // PointMovementGenerator
    Fleeing = 9,             // FleeingMovementGenerator
    Distract = 10,           // IdleMovementGenerator
    Assistance = 11,         // PointMovementGenerator
    AssistanceDistract = 12, // IdleMovementGenerator
    TimedFleeing = 13,       // FleeingMovementGenerator
    Follow = 14,
    Rotate = 15,
    Effect = 16,
    SplineChain = 17, // SplineChainMovementGenerator
    Formation = 18,   // FormationMovementGenerator
    Max
}